using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Reflection;

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

        public AboutViewModel()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
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
    }
}
