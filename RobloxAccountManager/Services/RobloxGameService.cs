using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RobloxAccountManager.Services
{
    public class RobloxGameService
    {
        private readonly HttpClient _httpClient;

        public RobloxGameService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RobloxAccountManager/1.0");
        }

        public async Task<long?> GetUniverseId(long placeId)
        {
            try
            {
                string url = $"https://apis.roblox.com/universes/v1/places/{placeId}/universe";
                var response = await _httpClient.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(response))
                {
                    if (doc.RootElement.TryGetProperty("universeId", out var uId))
                    {
                        long uid = uId.GetInt64();
                        LogService.Log($"API: Resolved Universe ID {uid} for Place {placeId}");
                        return uid;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error fetching Universe ID: {ex.Message}");
            }
            return null;
        }

        public async Task<RobloxGameDetail?> GetGameDetails(long universeId)
        {
            try
            {
                string url = $"https://games.roblox.com/v1/games?universeIds={universeId}";
                var response = await _httpClient.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(response))
                {
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                    {
                        var updated = data[0];
                        var newDetails = new RobloxGameDetail
                        {
                            Name = updated.GetProperty("name").GetString(),
                            Description = updated.GetProperty("description").GetString(),
                            CreatorName = updated.GetProperty("creator").GetProperty("name").GetString(),
                            Playing = updated.GetProperty("playing").GetInt64(),
                            Visits = updated.GetProperty("visits").GetInt64(),
                            FavoritedCount = updated.GetProperty("favoritedCount").GetInt64(),
                            Likes = 0 
                        };
                        LogService.Log($"API: Fetched details for game '{updated.GetProperty("name").GetString()}'");
                        return newDetails;
                    }
                }
            }
            catch (Exception ex)
            {
                 LogService.Error($"Error fetching Game Details: {ex.Message}");
            }
            return null;
        }

        public async Task<string?> GetGameIcon(long universeId)
        {
            try
            {
                string url = $"https://thumbnails.roblox.com/v1/games/icons?universeIds={universeId}&returnPolicy=PlaceHolder&size=150x150&format=Png&isCircular=false";
                var response = await _httpClient.GetStringAsync(url);
                using (var doc = JsonDocument.Parse(response))
                {
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                    {
                        LogService.Log($"API: Fetched game icon.");
                        return data[0].GetProperty("imageUrl").GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error fetching Game Icon: {ex.Message}");
            }
            return null;
        }

        public async Task<List<RobloxServer>> GetPublicServers(long placeId, int limit = 50)
        {
            try
            {
                string url = $"https://games.roblox.com/v1/games/{placeId}/servers/Public?sortOrder=Asc&limit={limit}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("data", out var dataElement))
                        {
                            var servers = new List<RobloxServer>();
                            foreach (var item in dataElement.EnumerateArray())
                            {
                                var server = new RobloxServer
                                {
                                    Id = item.GetProperty("id").GetString() ?? "",
                                    MaxPlayers = item.GetProperty("maxPlayers").GetInt32(),
                                    Playing = item.GetProperty("playing").GetInt32(),
                                    Ping = item.TryGetProperty("ping", out var p) ? p.GetInt32() : 0,
                                    Fps = item.TryGetProperty("fps", out var f) ? f.GetDouble() : 0
                                };
                                servers.Add(server);
                            }
                            LogService.Log($"API: Fetched {servers.Count} public servers.");
                            return servers;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error fetching servers: {ex.Message}");
            }
            return new List<RobloxServer>();
        }
    }

    public class RobloxGameDetail
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? CreatorName { get; set; }
        public long Playing { get; set; }
        public long Visits { get; set; }
        public long FavoritedCount { get; set; }
        public long Likes { get; set; }
    }

    public class RobloxServer
    {
        public string Id { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public int Playing { get; set; }
        public int Ping { get; set; }
        public double Fps { get; set; }
        
        public string PlayerCountDisplay => $"{Playing}/{MaxPlayers}";
    }
}
