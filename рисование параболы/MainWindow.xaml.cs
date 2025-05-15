using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace FlightSimulatorWPF
{
    public partial class MainWindow : Window
    {
        private List<TrajectoryPoint> trajectory = new List<TrajectoryPoint>();

        public MainWindow()
        {
            InitializeComponent();
            canvasTrajectory.SizeChanged += CanvasTrajectory_SizeChanged;
        }

        private void CanvasTrajectory_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawTrajectory();
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем параметры (заменили switch expression на обычный switch)
                double mass;
                switch (cbBirds.SelectedIndex)
                {
                    case 0: mass = 0.7; break;
                    case 1: mass = 1.0; break;
                    case 2: mass = 1.5; break;
                    default: mass = 1.0; break;
                }

                double force = slForce.Value;
                double angle = slAngle.Value;

                // Рассчитываем траекторию
                trajectory = CalculateTrajectory(mass, force, angle);

                // Отображаем результаты
                DisplayResults();
                DrawTrajectory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка симуляции");
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

            while (y >= 0)
            {
                double speed = Math.Sqrt(vx * vx + vy * vy);
                points.Add(new TrajectoryPoint(t, x, y, speed));

                // Физика полёта
                x += vx * dt;
                y += vy * dt - 0.5 * g * dt * dt;
                vy -= g * dt;
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
                            $"Время полёта: {last.Time:F1} с";
        }

        private void DrawTrajectory()
        {
            canvasTrajectory.Children.Clear();

            if (trajectory.Count < 2) return;

            // Масштабирование
            double maxX = trajectory.Max(p => p.X);
            double maxY = trajectory.Max(p => p.Y);
            double scaleX = (canvasTrajectory.ActualWidth - 40) / maxX;
            double scaleY = (canvasTrajectory.ActualHeight - 40) / maxY;
            double scale = Math.Min(scaleX, scaleY) * 0.9;

            // Рисуем оси
            DrawAxes(scale);

            // Рисуем траекторию (заменили LINQ на обычный цикл)
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
        }

        private void DrawAxes(double scale)
        {
            // Ось X
            canvasTrajectory.Children.Add(new Line
            {
                X1 = 20,
                Y1 = canvasTrajectory.ActualHeight - 20,
                X2 = canvasTrajectory.ActualWidth - 20,
                Y2 = canvasTrajectory.ActualHeight - 20,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            });

            // Ось Y
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
}