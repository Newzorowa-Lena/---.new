using System;
using System.Windows;

namespace FlightSimulatorWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Здесь можно добавить обработку необработанных исключений
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Обработка необработанных исключений
            MessageBox.Show($"Произошла непредвиденная ошибка:\n{e.Exception.Message}",
                          "Ошибка",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}