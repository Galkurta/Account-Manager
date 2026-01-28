using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RobloxAccountManager.Services;
using System.IO;

namespace RobloxAccountManager.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private string _customRobloxPath;

        [ObservableProperty]
        private string _downloadPath;

        [ObservableProperty]
        private string _executorPath;

        [ObservableProperty]
        private bool _autoLaunchExecutor;

        [ObservableProperty]
        private int _autoRejoinDelaySeconds;

        public SettingsViewModel()
        {
            _settingsService = new SettingsService();
            _customRobloxPath = _settingsService.CurrentSettings.CustomRobloxPath ?? string.Empty;
            _downloadPath = _settingsService.CurrentSettings.DownloadPath ?? string.Empty;
            _executorPath = _settingsService.CurrentSettings.ExecutorPath ?? string.Empty;
            _autoLaunchExecutor = _settingsService.CurrentSettings.AutoLaunchExecutor;
            _autoRejoinDelaySeconds = _settingsService.CurrentSettings.AutoRejoinDelaySeconds;

            System.Diagnostics.Debug.WriteLine("[Settings] Opened Settings Menu");
        }

        partial void OnCustomRobloxPathChanged(string value)
        {
            _settingsService.CurrentSettings.CustomRobloxPath = value;
            _settingsService.SaveSettings();
            System.Diagnostics.Debug.WriteLine($"[Settings] Updated Custom Roblox Path: {value}");
        }

        partial void OnDownloadPathChanged(string value)
        {
            _settingsService.CurrentSettings.DownloadPath = value;
            _settingsService.SaveSettings();
            System.Diagnostics.Debug.WriteLine($"[Settings] Updated Download Path: {value}");
        }

        partial void OnExecutorPathChanged(string value)
        {
            _settingsService.CurrentSettings.ExecutorPath = value;
            _settingsService.SaveSettings();
            System.Diagnostics.Debug.WriteLine($"[Settings] Updated Executor Path: {value}");
        }

        partial void OnAutoLaunchExecutorChanged(bool value)
        {
            _settingsService.CurrentSettings.AutoLaunchExecutor = value;
            _settingsService.SaveSettings();
            System.Diagnostics.Debug.WriteLine($"[Settings] Updated Auto Launch Executor: {value}");
        }

        partial void OnAutoRejoinDelaySecondsChanged(int value)
        {
            _settingsService.CurrentSettings.AutoRejoinDelaySeconds = value;
            _settingsService.SaveSettings();
            System.Diagnostics.Debug.WriteLine($"[Settings] Updated Auto Rejoin Delay: {value}");
        }

        [RelayCommand]
        private void BrowseCustomPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Roblox Versions Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                CustomRobloxPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void BrowseDownloadPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Download Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                DownloadPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void BrowseExecutorPath()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Executor Executable",
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                ExecutorPath = dialog.FileName;
            }
        }
    }
}
