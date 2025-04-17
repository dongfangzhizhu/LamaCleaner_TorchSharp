using System;
using System.Globalization; // Added for CultureInfo
using System.Threading; // Added for Thread.CurrentThread
using System.Windows;
using InpaintDesktop.Localization;

namespace InpaintDesktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Get current UI culture
            string cultureName = CultureInfo.CurrentUICulture.Name;
            // Or force a specific culture for testing:
            // string cultureName = "zh-CN";
            // string cultureName = "en-US";

            // Load the language file based on culture
            LocalizationHelper.LoadLanguage(cultureName);

            // Set the UI culture for the application threads (optional but good practice)
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);


            // 初始化本地化管理器 (This seems unused if LocalizationHelper is static)
            // var localizationManager = LocalizationManager.Instance;

            // 设置应用程序的异常处理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An error occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}