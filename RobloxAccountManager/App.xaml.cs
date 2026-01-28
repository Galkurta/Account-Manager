using System.Configuration;
using System.Data;
using System.Windows;

namespace RobloxAccountManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                string errorMsg = $"[{DateTime.Now}] Unhandled Exception:\n{e.Exception}\n\nStack Trace:\n{e.Exception.StackTrace}\n--------------------------\n";
                System.IO.File.AppendAllText(logFile, errorMsg);
                
                MessageBox.Show($"Application crashed. See crash_log.txt for details.\nError: {e.Exception.Message}", "Crash Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }
    }

