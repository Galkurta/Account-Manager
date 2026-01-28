using System;
using System.Linq;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;

namespace RobloxAccountManager.Views
{
    public partial class LoginContentDialog : ContentDialog
    {
        public string ScrapedCookie { get; private set; } = string.Empty;
        public DateTime? ScrapedCookieExpiration { get; private set; }

        public LoginContentDialog(ContentDialogHost presenter) : base(presenter)
        {
            InitializeComponent();
            Loaded += LoginContentDialog_Loaded;
        }

        private async void LoginContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            try
            {
                await LoginWebView.EnsureCoreWebView2Async();
                LoginWebView.CoreWebView2.CookieManager.DeleteAllCookies();
                LoginWebView.NavigationCompleted += LoginWebView_NavigationCompleted;
            }
            catch (Exception ex)
            {
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    await mw.ShowAlertAsync("WebView Error", $"Failed to initialize WebView2: {ex.Message}");
                }
            }
        }

        private async void LoginWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            try
            {
                var cookieManager = LoginWebView.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.roblox.com");
                
                var authCookie = cookies.FirstOrDefault(c => c.Name == ".ROBLOSECURITY");
                if (authCookie != null)
                {
                    ScrapedCookie = authCookie.Value;
                    
                    // CoreWebView2Cookie.Expires is DateTime in this wrapper
                    ScrapedCookieExpiration = authCookie.Expires;
                    

                }
            }
            catch (Exception ex)
            {
                 if (Application.Current.MainWindow is MainWindow mw)
                 {
                    await mw.ShowAlertAsync("Cookie Error", $"Error reading cookies: {ex.Message}");
                 }
            }
        }
    }
}
