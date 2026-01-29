using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace RobloxAccountManager.Views
{
    public partial class InternalBrowserWindow : Window
    {
        private readonly string _targetUrl;
        private readonly string? _destinationPath;

        public InternalBrowserWindow(string url, string? destinationPath = null)
        {
            InitializeComponent();
            _targetUrl = url;
            _destinationPath = destinationPath;
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
            // If we have a custom destination path, force save there and skip dialog
            if (!string.IsNullOrEmpty(_destinationPath) && System.IO.Directory.Exists(_destinationPath))
            {
                string fileName = System.IO.Path.GetFileName(e.ResultFilePath);
                e.ResultFilePath = System.IO.Path.Combine(_destinationPath, fileName);
                e.Handled = true; // Use our path, don't show dialog
            }
            else
            {
                // Default behavior is to show the default download dialog
                e.Handled = false;
            }

            var downloadOperation = e.DownloadOperation;
            downloadOperation.StateChanged += (s, args) =>
            {
                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    // If we controlled the path, we should handle extraction
                    if (!string.IsNullOrEmpty(_destinationPath) && System.IO.Directory.Exists(_destinationPath))
                    {
                         Dispatcher.Invoke(async () => 
                         {
                             Services.LogService.Log("Download completed. Extracting...", Services.LogLevel.Info, "Browser");
                             try
                             {
                                 string zipPath = downloadOperation.ResultFilePath;
                                 string fileName = System.IO.Path.GetFileNameWithoutExtension(zipPath);
                                 
                                 // Extract "version-xxxxxxxx" from filename (e.g., WEAO-LIVE-...-version-hash -> version-hash)
                                 string folderName = fileName;
                                 var match = System.Text.RegularExpressions.Regex.Match(fileName, @"version-[a-f0-9]+");
                                 if (match.Success)
                                 {
                                     folderName = match.Value;
                                 }

                                 string extractPath = System.IO.Path.Combine(_destinationPath, folderName);

                                 if (System.IO.Directory.Exists(extractPath))
                                 {
                                     System.IO.Directory.Delete(extractPath, true);
                                 }
                                 
                                 // Create the directory ensures it exists before extraction
                                 System.IO.Directory.CreateDirectory(extractPath);

                                 await System.Threading.Tasks.Task.Run(() => 
                                 {
                                     System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                                     System.IO.File.Delete(zipPath);
                                 });

                                 Services.LogService.Log($"Extracted version to {extractPath}", Services.LogLevel.Success, "Browser");
                                 Close();
                             }
                             catch (Exception ex)
                             {
                                 Services.LogService.Error($"Extraction failed: {ex.Message}", "Browser");
                                 MessageBox.Show($"Extraction failed: {ex.Message}", "Version Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                 // Close anyway? Maybe keep open to see error? logic says close if download done is the trigger, but error might need attention.
                                 // Let's close for now to stay consistent with auto-close logic request, user sees log.
                                 Close();
                             }
                         });
                    }
                    else
                    {
                        Dispatcher.Invoke(() => 
                        {
                            Services.LogService.Log("Download completed. Closing internal browser.", Services.LogLevel.Success, "Browser");
                            Close();
                        });
                    }
                }
            };
        }
    }
}
