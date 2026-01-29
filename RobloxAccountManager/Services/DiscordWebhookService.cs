using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

namespace RobloxAccountManager.Services
{
    public class DiscordWebhookService
    {
        private readonly SettingsService _settingsService;
        private readonly HttpClient _client;

        public DiscordWebhookService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _client = new HttpClient();
        }

        public async Task SendNotificationAsync(string title, string message, int color, string? accountName = null, long? userId = null, string? placeId = null, string? jobId = null, string? thumbUrl = null, string? placeName = null, string? serverType = null)
        {
            string webhookUrl = _settingsService.CurrentSettings.DiscordWebhookUrl;

            if (string.IsNullOrEmpty(webhookUrl) || !webhookUrl.StartsWith("http"))
                return;

            try
            {
                var fields = new System.Collections.Generic.List<object>();
                
                if (!string.IsNullOrEmpty(accountName))
                    fields.Add(new { name = "Account", value = accountName, inline = true });
                
                if (userId.HasValue)
                    fields.Add(new { name = "User ID", value = userId.Value.ToString(), inline = true });

                if (!string.IsNullOrEmpty(placeId))
                {
                    string pVal = placeId;
                    if (!string.IsNullOrEmpty(placeName)) pVal = $"{placeName} ({placeId})";
                    fields.Add(new { name = "Game", value = pVal, inline = false });
                }

                if (!string.IsNullOrEmpty(serverType))
                     fields.Add(new { name = "Server Type", value = serverType, inline = true });

                // Only add JobID if helpful, maybe redundant with deep link?
                // if (!string.IsNullOrEmpty(jobId))
                //    fields.Add(new { name = "Job ID", value = $"[{jobId.Substring(0, 8)}...](https://www.roblox.com/games/{placeId}?jobId={jobId})", inline = false });

                var payload = new
                {
                    username = "Roblox Account Manager",
                    embeds = new[]
                    {
                        new
                        {
                            title = title,
                            description = message,
                            color = color, // Decimal color
                            timestamp = DateTime.UtcNow.ToString("o"),
                            fields = fields.ToArray(),
                            footer = new { text = "Roblox Account Manager" },
                            thumbnail = !string.IsNullOrEmpty(thumbUrl) ? new { url = thumbUrl } : 
                                       (userId.HasValue ? new { url = $"https://www.roblox.com/headshot-thumbnail/image?userId={userId}&width=150&height=150&format=png" } : null)
                        }
                    }
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await _client.PostAsync(webhookUrl, content);
            }
            catch (Exception ex) 
            {
                LogService.Error($"Failed to send webhook: {ex.Message}", "Webhook");
            }
        }
    }
}
