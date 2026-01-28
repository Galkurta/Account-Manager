using System;
using System.Linq;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobloxAccountManager.Services;
using RobloxAccountManager.Views;
using System.Windows;

namespace RobloxAccountManager.ViewModels
{
    public partial class VersionManagerViewModel : ObservableObject
    {
        private readonly RobloxVersionService _versionService;
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private string _installedVersion;

        [ObservableProperty]
        private string _currentVersion;

        [ObservableProperty]
        private string _futureVersion;

        [ObservableProperty]
        private string _pastVersion;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _isLoading;


        // CustomPath is now a computed property from SettingsService
        public string CustomPath => _settingsService.CurrentSettings.CustomRobloxPath;

        public VersionManagerViewModel()
        {
            _versionService = new RobloxVersionService();
            _settingsService = new SettingsService();
            
            // Initialize non-nullable fields to default values
            _installedVersion = "Searching...";
            _currentVersion = "Loading...";
            _futureVersion = "Loading...";
            _pastVersion = "Loading...";
            _statusMessage = "Initializing...";

            // LoadVersionsAsync will read CustomPath via getter
            LoadVersionsAsync();
        }
        
        // ... (rest of the file remains same, except CustomPath property is replaced above)

        public async void LoadVersionsAsync()
        {
            IsLoading = true;
            StatusMessage = "Fetching version data...";

            try
            {
                // 1. Detect Installed Version
                InstalledVersion = DetectInstalledVersion();

                // 2. Fetch WEAO Data
                var current = await _versionService.GetCurrentVersion();
                var future = await _versionService.GetFutureVersion();
                var past = await _versionService.GetPastVersion();

                CurrentVersion = current?.WindowsVersion ?? "Unknown";
                FutureVersion = future?.WindowsVersion ?? "Unknown";
                PastVersion = past?.WindowsVersion ?? "Unknown";

                StatusMessage = "Version data loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string DetectInstalledVersion()
        {
            try
            {
                // 1. Registry Check (Most reliable for active installation)
                using (var key = Registry.ClassesRoot.OpenSubKey(@"roblox-player\shell\open\command"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("")?.ToString();
                        if (!string.IsNullOrEmpty(val)) 
                        {
                            int firstQuote = val.IndexOf('"');
                            int lastQuote = val.LastIndexOf('"');
                            if (firstQuote != -1 && lastQuote > firstQuote)
                            {
                                string exePath = val.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                                if (File.Exists(exePath))
                                {
                                    string? dirPath = Path.GetDirectoryName(exePath);
                                    if (!string.IsNullOrEmpty(dirPath))
                                        return new DirectoryInfo(dirPath).Name;
                                }
                            }
                        }
                    }
                }

                // 2. Custom Path Check (User defined)
                if (!string.IsNullOrWhiteSpace(CustomPath) && Directory.Exists(CustomPath))
                {
                    var dir = new DirectoryInfo(CustomPath);

                    // Case A: The user selected a specific version folder (e.g. .../version-1234/)
                    if (File.Exists(Path.Combine(dir.FullName, "RobloxPlayerBeta.exe")))
                    {
                        return dir.Name;
                    }
                    
                    // Case B: The user selected the 'Versions' root folder (e.g. .../Fishstrap/Versions/)
                    // logic: Find all subfolders starting with "version-", check if they have the exe, take newest.
                    var latestVersionDir = dir.GetDirectories()
                                              .Where(d => d.Name.StartsWith("version-") && File.Exists(Path.Combine(d.FullName, "RobloxPlayerBeta.exe")))
                                              .OrderByDescending(d => d.LastWriteTime)
                                              .FirstOrDefault();

                    if (latestVersionDir != null)
                    {
                        return latestVersionDir.Name;
                    }
                }

                return "Not Found";
            }
            catch
            {
                return "Error Detecting";
            }
        }

        // BrowseCustomPath removed - moved to Settings

        [RelayCommand]
        private void Refresh()
        {
            LoadVersionsAsync();
        }

        [RelayCommand]
        private void OpenDownload(string type)
        {
            string version = type switch
            {
                "Installed" => InstalledVersion, // This might be "Searching..." or "Not Found", handle check
                "Current" => CurrentVersion,
                "Future" => FutureVersion,
                "Past" => PastVersion,
                _ => ""
            };

            if (!string.IsNullOrEmpty(version) && version.StartsWith("version-"))
            {
                // RDD URL Structure:
                // https://rdd.weao.gg/?channel=LIVE&binaryType=WindowsPlayer&version=<HASH>
                string url = $"https://rdd.weao.gg/?channel=LIVE&binaryType=WindowsPlayer&version={version}";
                
                try
                {
                    // Open in internal browser window (WebView2)
                    // Ensure UI thread access if needed, but RelayCommand usually runs on UI thread for button clicks.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var browser = new InternalBrowserWindow(url);
                        browser.Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
                        browser.Show();
                    });

                    StatusMessage = $"Opened internal RDD browser for {version}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to open internal browser: {ex.Message}";
                }
            }
            else
            {
                StatusMessage = "Invalid version selected for download.";
            }
        }
    }
}
