using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RobloxAccountManager.Services
{
    public class RobloxVersionService
    {
        private readonly HttpClient _httpClient;

        public RobloxVersionService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WEAO-3PService");
        }

        public async Task<RobloxVersionInfo?> GetCurrentVersion()
        {
            return await FetchVersion("https://weao.xyz/api/versions/current");
        }

        public async Task<RobloxVersionInfo?> GetFutureVersion()
        {
            return await FetchVersion("https://weao.xyz/api/versions/future");
        }

        public async Task<RobloxVersionInfo?> GetPastVersion()
        {
            return await FetchVersion("https://weao.xyz/api/versions/past");
        }

        private async Task<RobloxVersionInfo?> FetchVersion(string url)
        {
            try
            {
                LogService.Log($"Fetching version info from: {url}", LogLevel.Info, "Version");
                var response = await _httpClient.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(response))
                {
                    var root = doc.RootElement;
                    LogService.Log($"Successfully fetched version data.", LogLevel.Info, "Version");
                    return new RobloxVersionInfo
                    {
                        WindowsVersion = GetStringSafe(root, "Windows"),
                        WindowsDate = GetStringSafe(root, "WindowsDate"),
                        MacVersion = GetStringSafe(root, "Mac"),
                        MacDate = GetStringSafe(root, "MacDate"),
                        AndroidVersion = GetStringSafe(root, "Android"),
                        AndroidDate = GetStringSafe(root, "AndroidDate"),
                        iOSVersion = GetStringSafe(root, "iOS"),
                        iOSDate = GetStringSafe(root, "iOSDate")
                    };
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error fetching version from {url}: {ex.Message}", "Version");
                return null;
            }
        }

        private string GetStringSafe(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() ?? "N/A";
            }
            return "N/A";
        }
    }

    public class RobloxVersionInfo
    {
        public string? WindowsVersion { get; set; }
        public string? WindowsDate { get; set; }
        public string? MacVersion { get; set; }
        public string? MacDate { get; set; }
        public string? AndroidVersion { get; set; }
        public string? AndroidDate { get; set; }
        public string? iOSVersion { get; set; }
        public string? iOSDate { get; set; }
    }
}
