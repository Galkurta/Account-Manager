using System;
using System.Linq;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;

namespace RobloxAccountManager.Views
{
    public partial class LoginWindow : FluentWindow
    {
        public string ScrapedCookie { get; private set; } = string.Empty;

        public LoginWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoginWebView.EnsureCoreWebView2Async();
            
            // Clear existing cookies for a clean login session if desired
            LoginWebView.CoreWebView2.CookieManager.DeleteAllCookies();

            // Hook URI navigation to check for successful login state if needed, 
            // but primarily we monitor cookies.
            // A more robust way: Poll for cookie or wait for SourceChanged to "home" page.
            
            LoginWebView.NavigationCompleted += LoginWebView_NavigationCompleted;
        }

        private async void LoginWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            // Check if we have the .ROBLOSECURITY cookie for .roblox.com
            try
            {
                var cookieManager = LoginWebView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.roblox.com");
                
                var authCookie = cookies.FirstOrDefault(c => c.Name == ".ROBLOSECURITY");
                if (authCookie != null)
                {
                    ScrapedCookie = authCookie.Value;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                // Handle error
                System.Windows.MessageBox.Show($"Error reading cookies: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
