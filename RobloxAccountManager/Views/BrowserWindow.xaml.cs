using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using RobloxAccountManager.Models;
using RobloxAccountManager.Services;

namespace RobloxAccountManager.Views
{
    public partial class BrowserWindow : Window
    {
        private readonly RobloxAccount _account;
        private readonly SecurityService _securityService;

        public BrowserWindow(RobloxAccount account, SecurityService securityService)
        {
            InitializeComponent();
            _account = account;
            _securityService = securityService;

            TitleText.Text = $"Roblox Browser - {_account.Username}";
            Title = $"Roblox Browser - {_account.Username}";

            Loaded += BrowserWindow_Loaded;
        }

        private async void BrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
        }

        private async Task InitializeWebView()
        {
            try
            {

                string baseDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserData");
                string userDataFolder = Path.Combine(baseDataFolder, _account.UserId.ToString());
                

                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
                

                await Browser.EnsureCoreWebView2Async(env);


                string cookie = _securityService.Decrypt(_account.CookieCipher);
                if (!string.IsNullOrEmpty(cookie))
                {
                    var cookieManager = Browser.CoreWebView2.CookieManager;
                    var roblostecurity = cookieManager.CreateCookie(".ROBLOSECURITY", cookie, ".roblox.com", "/");
                    roblostecurity.IsHttpOnly = true;
                    roblostecurity.IsSecure = true; // Roblox requires secure cookies
                    cookieManager.AddOrUpdateCookie(roblostecurity);
                }


                Browser.CoreWebView2.Navigate("https://www.roblox.com/home");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize browser: {ex.Message}", "Browser Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Browser.Reload();
        }
    }
}
