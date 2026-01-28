using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace RobloxAccountManager.Views
{
    public partial class InternalBrowserWindow : Window
    {
        private readonly string _targetUrl;

        public InternalBrowserWindow(string url)
        {
            InitializeComponent();
            _targetUrl = url;
            Loaded += InternalBrowserWindow_Loaded;
        }

        private async void InternalBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Browser.EnsureCoreWebView2Async();
                Browser.CoreWebView2.Navigate(_targetUrl);
                
                // Allow downloads if this is used for version manager
                Browser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize browser: {ex.Message}", "Browser Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Default behavior is to show the default download dialog, which is fine.
            // We can ensure it's not handled/cancelled.
            e.Handled = false;
        }
    }
}
