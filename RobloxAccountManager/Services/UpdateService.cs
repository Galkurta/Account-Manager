using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using RobloxAccountManager.Models;

namespace RobloxAccountManager.Services
{
    public class UpdateService
    {
        private const string REPO_OWNER = "Galkurta";
        private const string REPO_NAME = "Account-Manager"; 
        private const string LAST_CHECK_FILE = "last_update_check.json";
        
        // Use a persistent timestamp to respect the 24h interval
        private string _lastCheckFilePath;

        public UpdateService()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "RobloxAccountManager");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                _lastCheckFilePath = Path.Combine(folder, LAST_CHECK_FILE);
            }
            catch
            {
                _lastCheckFilePath = LAST_CHECK_FILE;
            }
        }

        public async Task<GithubRelease?> CheckForUpdatesAsync(bool force = false)
        {
            try
            {
                // Let's implement the check logic but allow forcing it if needed.
                if (!force && ShouldSkipCheck()) return null;

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RobloxAccountManager");

                var release = await client.GetFromJsonAsync<GithubRelease>(
                    $"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest");

                if (release == null) return null;

                // Save check time ONLY if not forced, or maybe always? 
                // Let's save it always so we reset the 24h timer on manual check too.
                File.WriteAllText(_lastCheckFilePath, DateTime.Now.ToString("o"));

                // specific version parsing logic
                string currentVersionStr = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
                // Assembly version usually is 1.0.0.0, GitHub tag might be v1.0.0
                
                string remoteTag = release.TagName.TrimStart('v');
                
                // Simple version comparison
                Version current = new Version(currentVersionStr);
                
                // Handle cases where tag is just "1.1" -> "1.1.0"
                if (remoteTag.Split('.').Length < 3)
                {
                   // Pad if necessary, or let Version constructor handle it if it follows x.y.z
                }
                
                // Clean tag of any suffixes like "-beta" for Version class (it throws on non-numeric)
                string safeRemoteTag = remoteTag.Split('-')[0];

                if (Version.TryParse(safeRemoteTag, out Version? remote))
                {
                    if (remote > current)
                    {
                        return release;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                return null;
            }
        }

        private bool ShouldSkipCheck()
        {
            try
            {
                if (!File.Exists(_lastCheckFilePath)) return false;
                string content = File.ReadAllText(_lastCheckFilePath);
                if (DateTime.TryParse(content, out DateTime lastCheck))
                {
                    return (DateTime.Now - lastCheck).TotalHours < 24;
                }
            }
            catch { }
            return false;
        }

        public async Task DownloadAndInstallAsync(GithubRelease release)
        {
            try
            {
                // Find .exe asset
                var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"));
                if (asset == null) return;

                string tempFile = Path.Combine(Path.GetTempPath(), asset.Name);

                using HttpClient client = new HttpClient();
                var data = await client.GetByteArrayAsync(asset.BrowserDownloadUrl);
                await File.WriteAllBytesAsync(tempFile, data);

                // Run installer
                Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });

                // Shutdown app
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LogService.Error($"Update install failed: {ex.Message}", "Update");
                // In a real app, we might want to notify the user of failure
            }
        }
    }
}
