using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RobloxAccountManager.Services;
using RobloxAccountManager.ViewModels;

namespace RobloxAccountManager.Views
{
    public partial class LogsPage : Page
    {
        public ObservableCollection<LogEntry> Logs { get; private set; } = new ObservableCollection<LogEntry>();

        public LogsPage()
        {
            InitializeComponent();
            
            // Bind ListView
            LogListView.ItemsSource = Logs;

            if (Application.Current.MainWindow.DataContext is MainViewModel vm)
                DataContext = vm.LogsVM;
            
            LogService.OnLogEntry += LogService_OnLogEntry;
            Unloaded += LogsPage_Unloaded;

            LoadLogHistory();
        }

        private void LogsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            LogService.OnLogEntry -= LogService_OnLogEntry;
        }

        private void LogService_OnLogEntry(LogEntry entry)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AppendLog(entry);
            });
        }

        private void LoadLogHistory()
        {
            var history = LogService.GetHistory();
            foreach (var entry in history)
            {
                Logs.Add(entry);
            }
            if (Logs.Count > 0)
                LogListView.ScrollIntoView(Logs[Logs.Count - 1]);
        }

        private void AppendLog(LogEntry entry)
        {
            Logs.Add(entry);
            
            // Auto-scroll
            LogListView.ScrollIntoView(entry);
        }
    }
}
