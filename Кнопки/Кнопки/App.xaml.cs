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

            // Инициализация глобальных ресурсов или сервисов
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            // Здесь можно добавить инициализацию:
            // - Глобальных стилей
            // - Сервисов приложения
            // - Настроек
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Очистка ресурсов при закрытии приложения
            base.OnExit(e);
        }
    }
}