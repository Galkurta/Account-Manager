using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Reflection;
using RobloxAccountManager.Services;
using System.Threading.Tasks;

namespace RobloxAccountManager.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _appVersion;

        [ObservableProperty]
        private string _appName = "Roblox Account Manager";

        [ObservableProperty]
        private string _developer = "Galkurta";

        [ObservableProperty]
        private string _updateStatus = "Up to date";

        [ObservableProperty]
        private bool _isCheckingForUpdate;
        
        [ObservableProperty]
        private bool _updateAvailable;

        private readonly MainViewModel _mainViewModel;
        private readonly UpdateService _updateService;

        public AboutViewModel(MainViewModel main, UpdateService updateService)
        {
            _mainViewModel = main;
            _updateService = updateService;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
            
            // Initial check status text
             _updateStatus = "Click to check for updates";
        }

        [RelayCommand]
        public async Task CheckForUpdates()
        {
            if (IsCheckingForUpdate) return;

            IsCheckingForUpdate = true;
            UpdateStatus = "Checking for updates...";
            UpdateAvailable = false;

            try
            {
                var release = await _updateService.CheckForUpdatesAsync(force: true);
                if (release != null)
                {
                    UpdateStatus = $"Update found: {release.TagName}";
                    UpdateAvailable = true;
                    
                     var result = System.Windows.MessageBox.Show($"New version {release.TagName} is available!\n\nRelease Notes:\n{release.Body}\n\nDo you want to update now?", "Update Found", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
                     
                     if (result == System.Windows.MessageBoxResult.Yes)
                     {
                         await _updateService.DownloadAndInstallAsync(release);
                     }
                }
                else
                {
                    UpdateStatus = "You are up to date";
                    UpdateAvailable = false;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus = "Check failed";
                LogService.Error($"Manual update check error: {ex}", "Update");
            }
            finally
            {
                IsCheckingForUpdate = false;
            }
        }

        [RelayCommand]
        public void OpenGitHub()
        {
            OpenUrl("https://github.com/Galkurta/Account-Manager");
        }

        [RelayCommand]
        public void OpenTelegram()
        {
            OpenUrl("https://t.me/nookiesol");
        }

        [RelayCommand]
        public void OpenDiscord()
        {
            // Weao's Discord
            OpenUrl("https://discord.gg/3ujEKMBmet"); 
        }

        [RelayCommand]
        public void OpenWpfUi()
        {
            OpenUrl("https://github.com/lepoco/wpfui");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }

        }

        [RelayCommand]
        public void NavigateBack()
        {
            _mainViewModel?.NavigateAccounts();
        }
    }
}
