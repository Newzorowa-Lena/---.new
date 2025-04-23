using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;

namespace FlightSimulatorWPF
{
    public partial class MainWindow : Window
    {
        private FlightSimulator simulator;
        private List<TrajectoryPoint> trajectory;
        private List<Obstacle> obstacles;
        private List<Pig> pigs;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSimulationObjects();
        }

        private void InitializeSimulationObjects()
        {
            obstacles = new List<Obstacle>();
            obstacles.Add(new Obstacle("Wood", 30));
            obstacles.Add(new Obstacle("Stone", 80));

            pigs = new List<Pig>();
            pigs.Add(new Pig("Green Pig", 50));
            pigs.Add(new Pig("Big Pig", 100));
        }

        private double GetSelectedMass()
        {
            switch (birdComboBox.SelectedIndex)
            {
                case 0: return 0.7;  // the Blues
                case 1: return 1.0;  // Stella
                case 2: return 1.5;  // Red
                default: return 1.0;  // Default
            }
        }

        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double mass = GetSelectedMass();
                double force = forceSlider.Value;
                double angle = angleSlider.Value;

                simulator = new FlightSimulator(mass, force, angle);
                simulator.OnFlightCompleted += (s, args) =>
                    Dispatcher.Invoke(() => resultsText.Text += "\nFlight completed!");

                trajectory = ConvertTrajectory(simulator.CalculateTrajectory());
                DisplayResults();

                trajectoryDataGrid.ItemsSource = trajectory;
                impactDataGrid.ItemsSource = CalculateImpactResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<TrajectoryPoint> ConvertTrajectory(List<(double time, double x, double y, double speed)> points)
        {
            var result = new List<TrajectoryPoint>();
            foreach (var point in points)
            {
                result.Add(new TrajectoryPoint(point.time, point.x, point.y, point.speed));
            }
            return result;
        }

        private List<ImpactResult> CalculateImpactResults()
        {
            double impactForce = simulator.CalculateImpactForce() * GetAttackMultiplier();
            var results = new List<ImpactResult>();

            foreach (var obstacle in obstacles)
            {
                bool destroyed = obstacle.TakeDamage(impactForce);
                results.Add(new ImpactResult(
                    $"Obstacle ({obstacle.Material})",
                    obstacle.Durability + (int)impactForce,
                    obstacle.Durability,
                    destroyed ? "DESTROYED" : "Damaged"));
            }

            foreach (var pig in pigs)
            {
                bool killed = pig.TakeDamage(impactForce);
                results.Add(new ImpactResult(
                    pig.Name,
                    pig.Health + (int)impactForce,
                    pig.Health,
                    killed ? "ELIMINATED" : "Injured"));
            }

            return results;
        }

        private double GetAttackMultiplier()
        {
            if (explosiveAttack.IsChecked == true) return 1.5;
            if (acceleratedAttack.IsChecked == true) return 1.3;
            return 1.0;
        }

        private void DisplayResults()
        {
            if (simulator == null || trajectory == null || trajectory.Count == 0) return;

            double maxHeight = 0;
            foreach (var point in trajectory)
            {
                if (point.Y > maxHeight) maxHeight = point.Y;
            }

            resultsText.Text = $"Results:\nMass: {simulator.Mass} kg\nForce: {simulator.Force} N\n" +
                             $"Angle: {simulator.Angle}°\nSpeed: {simulator.InitialSpeed:F2} m/s\n" +
                             $"Duration: {trajectory[trajectory.Count - 1].Time:F2} s\n" +
                             $"Max Height: {maxHeight:F2} m\nFinal Speed: {simulator.FinalSpeed:F2} m/s";
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (trajectory == null || trajectory.Count == 0)
            {
                MessageBox.Show("No data to export", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "flight_data.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Time,X,Y,Speed");
                        foreach (var point in trajectory)
                        {
                            writer.WriteLine($"{point.Time:F2},{point.X:F2},{point.Y:F2},{point.Speed:F2}");
                        }
                    }
                    MessageBox.Show("Data exported successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class FlightSimulator
    {
        public double Mass { get; }
        public double Force { get; }
        public double Angle { get; }
        public double DeltaTime { get; set; } = 0.05;
        private readonly double g = 9.8;
        private readonly double airDensity = 1.225;
        private readonly double dragCoefficient = 0.47;
        private readonly double crossSectionalArea = 0.01;

        public double InitialSpeed { get; }
        public double FinalSpeed { get; private set; }

        public delegate void FlightCompletedEventHandler(object sender, EventArgs e);
        public event FlightCompletedEventHandler OnFlightCompleted;

        public FlightSimulator(double mass, double force, double angle)
        {
            Mass = mass;
            Force = force;
            Angle = angle;
            InitialSpeed = force / mass;
        }

        public List<(double time, double x, double y, double speed)> CalculateTrajectory()
        {
            var trajectory = new List<(double time, double x, double y, double speed)>();

            double angleRad = Angle * Math.PI / 180.0;
            double vX = InitialSpeed * Math.Cos(angleRad);
            double vY = InitialSpeed * Math.Sin(angleRad);

            double time = 0;
            double x = 0, y = 0;

            while (y >= 0)
            {
                trajectory.Add((time, x, y, Math.Sqrt(vX * vX + vY * vY)));

                double speed = Math.Sqrt(vX * vX + vY * vY);
                double dragForce = 0.5 * airDensity * dragCoefficient * crossSectionalArea * speed * speed;
                double dragAcceleration = dragForce / Mass;

                double dragAx = dragAcceleration * (vX / speed);
                double dragAy = dragAcceleration * (vY / speed);

                vX -= dragAx * DeltaTime;
                vY -= (g + dragAy) * DeltaTime;

                x += vX * DeltaTime;
                y += vY * DeltaTime;
                time += DeltaTime;
            }

            FinalSpeed = Math.Sqrt(vX * vX + vY * vY);
            OnFlightCompleted?.Invoke(this, EventArgs.Empty);
            return trajectory;
        }

        public double CalculateImpactForce(double contactTime = 0.1)
        {
            return (Mass * FinalSpeed) / contactTime;
        }
    }

    public class Obstacle
    {
        public string Material { get; }
        public int Durability { get; private set; }

        public Obstacle(string material, int durability)
        {
            Material = material;
            Durability = durability;
        }

        public bool TakeDamage(double force)
        {
            Durability -= (int)force;
            return Durability <= 0;
        }
    }

    public class Pig
    {
        public string Name { get; }
        public int Health { get; private set; }

        public Pig(string name, int health)
        {
            Name = name;
            Health = health;
        }

        public bool TakeDamage(double force)
        {
            Health -= (int)force;
            return Health <= 0;
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

    public class ImpactResult
    {
        public string Target { get; set; }
        public int InitialHealth { get; set; }
        public int RemainingHealth { get; set; }
        public string Status { get; set; }

        public ImpactResult(string target, int initialHealth, int remainingHealth, string status)
        {
            Target = target;
            InitialHealth = initialHealth;
            RemainingHealth = remainingHealth;
            Status = status;
        }
    }
}