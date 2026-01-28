using CommunityToolkit.Mvvm.ComponentModel;
using RobloxAccountManager.Services;
using System.Windows;

namespace RobloxAccountManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _logOutput = "Ready.\n";

        public LogsViewModel()
        {
             // Subscribe to global logs
            LogService.OnLog += (msg) =>
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    LogOutput += msg + "\n";
                });
            };
        }
    }
}
