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

        [ObservableProperty]
        private string _discordWebhookUrl = "";

        private readonly MainViewModel _mainViewModel;

        public SettingsViewModel(MainViewModel main, SettingsService settingsService)
        {
            _mainViewModel = main;
            _settingsService = settingsService;
            _customRobloxPath = _settingsService.CurrentSettings.CustomRobloxPath ?? string.Empty;
            _downloadPath = _settingsService.CurrentSettings.DownloadPath ?? string.Empty;
            _executorPath = _settingsService.CurrentSettings.ExecutorPath ?? string.Empty;
            _autoLaunchExecutor = _settingsService.CurrentSettings.AutoLaunchExecutor;
            _autoRejoinDelaySeconds = _settingsService.CurrentSettings.AutoRejoinDelaySeconds;
            _discordWebhookUrl = _settingsService.CurrentSettings.DiscordWebhookUrl ?? string.Empty;

            System.Diagnostics.Debug.WriteLine("[Settings] Opened Settings Menu");
        }

        partial void OnDiscordWebhookUrlChanged(string value)
        {
            _settingsService.CurrentSettings.DiscordWebhookUrl = value;
            _settingsService.SaveSettings();
            // Don't log full URL for privacy
            LogService.Log("[Settings] Updated Discord Webhook URL", LogLevel.Info, "Settings");
        }

        partial void OnCustomRobloxPathChanged(string value)
        {
            _settingsService.CurrentSettings.CustomRobloxPath = value;
            _settingsService.SaveSettings();
            LogService.Log($"[Settings] Updated Custom Roblox Path: {value}", LogLevel.Info, "Settings");
        }

        partial void OnDownloadPathChanged(string value)
        {
            _settingsService.CurrentSettings.DownloadPath = value;
            _settingsService.SaveSettings();
            LogService.Log($"[Settings] Updated Download Path: {value}", LogLevel.Info, "Settings");
        }

        partial void OnExecutorPathChanged(string value)
        {
            _settingsService.CurrentSettings.ExecutorPath = value;
            _settingsService.SaveSettings();
            LogService.Log($"[Settings] Updated Executor Path: {value}", LogLevel.Info, "Settings");
        }

        partial void OnAutoLaunchExecutorChanged(bool value)
        {
            _settingsService.CurrentSettings.AutoLaunchExecutor = value;
            _settingsService.SaveSettings();
            LogService.Log($"[Settings] Updated Auto Launch Executor: {value}", LogLevel.Info, "Settings");
        }

        partial void OnAutoRejoinDelaySecondsChanged(int value)
        {
            _settingsService.CurrentSettings.AutoRejoinDelaySeconds = value;
            _settingsService.SaveSettings();
            LogService.Log($"[Settings] Updated Auto Rejoin Delay: {value}", LogLevel.Info, "Settings");
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


        [RelayCommand]
        public void NavigateBack()
        {
            _mainViewModel.NavigateAccounts();
        }
    }
}
