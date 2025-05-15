using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace FlightSimulatorWPF
{
    public partial class MainWindow : Window
    {
        private List<TrajectoryPoint> trajectory = new List<TrajectoryPoint>();
        private DispatcherTimer simulationTimer;
        private double simulationTime = 0;
        private List<Obstacle> obstacles = new List<Obstacle>();
        private bool isSimulating = false;
        private double currentBoost = 1.0;
        private double airDensity = 1.225; // Плотность воздуха (кг/м³)
        private double dragCoefficient = 0.47; // Коэффициент сопротивления для сферы

        public MainWindow()
        {
            InitializeComponent();
            canvasTrajectory.SizeChanged += CanvasTrajectory_SizeChanged;
            InitializeSimulationTimer();
            InitializeObstacles();
        }

        private void InitializeSimulationTimer()
        {
            simulationTimer = new DispatcherTimer();
            simulationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            simulationTimer.Tick += SimulationTimer_Tick;
        }

        private void InitializeObstacles()
        {
            obstacles = new List<Obstacle>
            {
                new Obstacle { X = 20, Width = 5, Height = 10, Type = ObstacleType.Wall },
                new Obstacle { X = 40, Width = 10, Height = 5, Type = ObstacleType.Wall },
                new Obstacle { X = 70, Width = 15, Height = 8, Type = ObstacleType.Wall }
            };
        }

        private void CanvasTrajectory_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawTrajectory();
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            if (isSimulating)
            {
                StopSimulation();
                return;
            }

            StartSimulation();
        }

        private void StartSimulation()
        {
            try
            {
                // Получаем параметры
                double mass = GetSelectedBirdMass();
                currentBoost = GetSelectedBoostValue();
                double force = slForce.Value * currentBoost;
                double angle = slAngle.Value;
                airDensity = slAirDensity.Value;
                dragCoefficient = slDragCoefficient.Value;

                // Рассчитываем траекторию
                trajectory = CalculateTrajectory(mass, force, angle);

                // Запускаем визуализацию в реальном времени
                simulationTime = 0;
                isSimulating = true;
                btnSimulate.Content = "Остановить";
                simulationTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка симуляции");
            }
        }

        private void StopSimulation()
        {
            simulationTimer.Stop();
            isSimulating = false;
            btnSimulate.Content = "Запустить";
            DrawTrajectory(); // Рисуем полную траекторию при остановке
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            simulationTime += 0.05;

            // Находим текущую позицию
            var currentPoint = trajectory.FirstOrDefault(p => p.Time >= simulationTime);
            if (currentPoint == null)
            {
                StopSimulation();
                return;
            }

            // Проверяем столкновения
            if (CheckCollision(currentPoint))
            {
                StopSimulation();
                MessageBox.Show("Птичка столкнулась с препятствием!", "Столкновение");
                return;
            }

            // Рисуем траекторию до текущей точки
            DrawRealTimeTrajectory(simulationTime);
        }

        private bool CheckCollision(TrajectoryPoint point)
        {
            foreach (var obstacle in obstacles)
            {
                if (point.X >= obstacle.X && point.X <= obstacle.X + obstacle.Width &&
                    point.Y <= obstacle.Height)
                {
                    return true;
                }
            }
            return false;
        }

        private double GetSelectedBirdMass()
        {
            switch (cbBirds.SelectedIndex)
            {
                case 0: return 0.7;
                case 1: return 1.0;
                case 2: return 1.5;
                default: return 1.0;
            }
        }

        private double GetSelectedBoostValue()
        {
            switch (cbBoost.SelectedIndex)
            {
                case 0: return 1.0;
                case 1: return 1.2;
                case 2: return 1.5;
                case 3: return 2.0;
                default: return 1.0;
            }
        }

        private ObstacleType GetSelectedObstacleType()
        {
            switch (cbObstacleType.SelectedIndex)
            {
                case 0: return ObstacleType.Wall;
                case 1: return ObstacleType.Pit;
                case 2: return ObstacleType.Cloud;
                default: return ObstacleType.Wall;
            }
        }

        private List<TrajectoryPoint> CalculateTrajectory(double mass, double force, double angle)
        {
            const double g = 9.8;
            const double dt = 0.05;
            var points = new List<TrajectoryPoint>();

            double angleRad = angle * Math.PI / 180;
            double vx = (force / mass) * Math.Cos(angleRad);
            double vy = (force / mass) * Math.Sin(angleRad);
            double x = 0, y = 0, t = 0;
            double crossSectionArea = 0.01; // Площадь поперечного сечения птицы (м²)

            while (y >= 0)
            {
                double speed = Math.Sqrt(vx * vx + vy * vy);
                points.Add(new TrajectoryPoint(t, x, y, speed));

                // Сила сопротивления воздуха
                double dragForce = 0.5 * airDensity * speed * speed * dragCoefficient * crossSectionArea;

                // Угол направления силы сопротивления
                double dragAngle = Math.Atan2(vy, vx);
                double dragForceX = dragForce * Math.Cos(dragAngle + Math.PI);
                double dragForceY = dragForce * Math.Sin(dragAngle + Math.PI);

                // Обновляем скорость с учетом сопротивления воздуха
                double ax = dragForceX / mass;
                double ay = -g + dragForceY / mass;

                vx += ax * dt;
                vy += ay * dt;

                // Обновляем позицию
                x += vx * dt;
                y += vy * dt;

                t += dt;
            }

            return points;
        }

        private void DisplayResults()
            {
                if (trajectory.Count == 0) return;

                var last = trajectory[trajectory.Count - 1];
                double maxY = trajectory.Max(p => p.Y);
                tbResults.Text = $"Дальность: {last.X:F1} м\n" +
                                $"Макс. высота: {maxY:F1} м\n" +
                                $"Время полёта: {last.Time:F1} с\n" +
                                $"Усиление: x{currentBoost:F1}";
            }

            private void DrawRealTimeTrajectory(double currentTime)
            {
                canvasTrajectory.Children.Clear();

                if (trajectory.Count < 2) return;

                // Scaling
                double maxX = trajectory.Max(p => p.X);
                double maxY = trajectory.Max(p => p.Y);
                double scaleX = (canvasTrajectory.ActualWidth - 40) / maxX;
                double scaleY = (canvasTrajectory.ActualHeight - 40) / maxY;
                double scale = Math.Min(scaleX, scaleY) * 0.9;

                // Draw axes
                DrawAxes(scale);

                // Draw obstacles
                DrawObstacles(scale);

                // Draw trajectory up to current time
                var visiblePoints = trajectory.Where(p => p.Time <= currentTime).ToList();

                if (visiblePoints.Count > 1)
                {
                    var polyline = new Polyline
                    {
                        Stroke = Brushes.Blue,
                        StrokeThickness = 2
                    };

                    foreach (var point in visiblePoints)
                    {
                        double canvasX = point.X * scale + 20;
                        double canvasY = canvasTrajectory.ActualHeight - point.Y * scale - 20;
                        polyline.Points.Add(new Point(canvasX, canvasY));
                    }

                    canvasTrajectory.Children.Add(polyline);
                }

                // Draw current position
                var currentPoint = visiblePoints.LastOrDefault();
                if (currentPoint != null)
                {
                    double canvasX = currentPoint.X * scale + 20;
                    double canvasY = canvasTrajectory.ActualHeight - currentPoint.Y * scale - 20;

                    var birdEllipse = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = GetBirdColor(),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(birdEllipse, canvasX - 5);
                    Canvas.SetTop(birdEllipse, canvasY - 5);
                    canvasTrajectory.Children.Add(birdEllipse);
                }

                DisplayResults();
            }

            private void DrawTrajectory()
            {
                canvasTrajectory.Children.Clear();

                if (trajectory.Count < 2) return;

                // Scaling
                double maxX = trajectory.Max(p => p.X);
                double maxY = trajectory.Max(p => p.Y);
                double scaleX = (canvasTrajectory.ActualWidth - 40) / maxX;
                double scaleY = (canvasTrajectory.ActualHeight - 40) / maxY;
                double scale = Math.Min(scaleX, scaleY) * 0.9;

                // Draw axes
                DrawAxes(scale);

                // Draw obstacles
                DrawObstacles(scale);

                // Draw full trajectory
                var polyline = new Polyline
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };

                foreach (var point in trajectory)
                {
                    double canvasX = point.X * scale + 20;
                    double canvasY = canvasTrajectory.ActualHeight - point.Y * scale - 20;
                    polyline.Points.Add(new Point(canvasX, canvasY));
                }

                canvasTrajectory.Children.Add(polyline);
                DisplayResults();
            }

            private void DrawObstacles(double scale)
            {
                foreach (var obstacle in obstacles)
                {
                    double canvasX = obstacle.X * scale + 20;
                    double canvasWidth = obstacle.Width * scale;
                    double canvasHeight = obstacle.Height * scale;
                    double canvasY = canvasTrajectory.ActualHeight - 20;

                    var rect = new Rectangle
                    {
                        Width = canvasWidth,
                        Height = canvasHeight,
                        Fill = GetObstacleBrush(obstacle.Type),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(rect, canvasX);
                    Canvas.SetTop(rect, canvasY - canvasHeight);
                    canvasTrajectory.Children.Add(rect);
                }
            }

            private Brush GetObstacleBrush(ObstacleType type)
            {
                switch (type)
                {
                    case ObstacleType.Wall: return Brushes.Brown;
                    case ObstacleType.Pit: return Brushes.Gray;
                    case ObstacleType.Cloud: return Brushes.LightGray;
                    default: return Brushes.Brown;
                }
            }

            private Brush GetBirdColor()
            {
                switch (cbBirds.SelectedIndex)
                {
                    case 0: return Brushes.Blue;
                    case 1: return Brushes.Yellow;
                    case 2: return Brushes.Red;
                    default: return Brushes.Blue;
                }
            }

            private void DrawAxes(double scale)
            {
                // X axis
                canvasTrajectory.Children.Add(new Line
                {
                    X1 = 20,
                    Y1 = canvasTrajectory.ActualHeight - 20,
                    X2 = canvasTrajectory.ActualWidth - 20,
                    Y2 = canvasTrajectory.ActualHeight - 20,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });

                // Y axis
                canvasTrajectory.Children.Add(new Line
                {
                    X1 = 20,
                    Y1 = 20,
                    X2 = 20,
                    Y2 = canvasTrajectory.ActualHeight - 20,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });
            }

            private void BtnExport_Click(object sender, RoutedEventArgs e)
            {
                if (trajectory.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Ошибка");
                    return;
                }

                var dialog = new SaveFileDialog { Filter = "CSV файлы|*.csv" };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        using (var writer = new System.IO.StreamWriter(dialog.FileName))
                        {
                            writer.WriteLine("Время;X;Y;Скорость");
                            foreach (var point in trajectory)
                            {
                                writer.WriteLine($"{point.Time:F2};{point.X:F2};{point.Y:F2};{point.Speed:F2}");
                            }
                        }
                        MessageBox.Show("Данные успешно экспортированы", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
                    }
                }
            }

            private void BtnAddObstacle_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    var obstacle = new Obstacle
                    {
                        X = double.Parse(txtObstacleX.Text),
                        Width = double.Parse(txtObstacleWidth.Text),
                        Height = double.Parse(txtObstacleHeight.Text),
                        Type = GetSelectedObstacleType()
                    };

                    obstacles.Add(obstacle);
                    DrawTrajectory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления препятствия: {ex.Message}", "Ошибка");
                }
            }

            private void BtnClearObstacles_Click(object sender, RoutedEventArgs e)
            {
                obstacles.Clear();
                DrawTrajectory();
            }
        }

        public class TrajectoryPoint
        {
            public double Time { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Speed { get; set; }

            public TrajectoryPoint(double time, double x, double y, double speed)
            {
                Time = time;
                X = x;
                Y = y;
                Speed = speed;
            }
        }

        public class Obstacle
        {
            public double X { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public ObstacleType Type { get; set; }
        }

        public enum ObstacleType
        {
            Wall,
            Pit,
            Cloud
        }
    }
