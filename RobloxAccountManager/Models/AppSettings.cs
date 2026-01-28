using System.Text.Json.Serialization;

namespace RobloxAccountManager.Models
{
    public class AppSettings
    {
        [JsonPropertyName("customRobloxPath")]
        public string CustomRobloxPath { get; set; } = string.Empty;

        [JsonPropertyName("downloadPath")]
        public string DownloadPath { get; set; } = string.Empty;

        [JsonPropertyName("executorPath")]
        public string ExecutorPath { get; set; } = string.Empty;

        [JsonPropertyName("autoLaunchExecutor")]
        public bool AutoLaunchExecutor { get; set; } = false;

        [JsonPropertyName("autoRejoinDelaySeconds")]
        public int AutoRejoinDelaySeconds { get; set; } = 15;
    }
}
