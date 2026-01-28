using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RobloxAccountManager.Services
{
    public class RobloxRequestService
    {
        private const string UserAgent = "RobloxAccountManager/1.0";

        public async Task<(bool success, string message)> SendFriendRequestAsync(string cookie, long targetUserId)
        {
            try
            {
                using var client = CreateAuthenticatedClient(cookie);
                string url = $"https://friends.roblox.com/v1/users/{targetUserId}/request-friendship";
                
                // Post body required even if empty
                var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");


                var response = await ExecuteWithCsrfAsync(client, url, content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Friend request sent successfully.");
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    return (false, $"Failed ({(int)response.StatusCode}): {errorBody}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<long?> GetUserIdFromUsernameAsync(string username)
        {
            try
            {

                using var client = new HttpClient(); 
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                var payload = new { usernames = new[] { username }, excludeBannedUsers = true };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://users.roblox.com/v1/usernames/users", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    var data = doc.RootElement.GetProperty("data");
                    
                    if (data.GetArrayLength() > 0)
                    {
                        return data[0].GetProperty("id").GetInt64();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"User lookup error: {ex.Message}");
            }

            return null;
        }

        public async Task<RobloxUserInfo?> GetUserInfoAsync(long userId)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                var response = await client.GetStringAsync($"https://users.roblox.com/v1/users/{userId}");
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                var info = new RobloxUserInfo
                {
                    Id = root.GetProperty("id").GetInt64(),
                    Name = root.GetProperty("name").GetString() ?? "",
                    DisplayName = root.GetProperty("displayName").GetString() ?? "",
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""
                };


                info.AvatarUrl = await GetUserAvatarAsync(userId) ?? "https://tr.rbxcdn.com/53eb9b17fe1432a809c73a132d78f5f1/150/150/AvatarHeadshot/Png";
                

                var presence = await GetUserPresenceAsync(userId);
                if (presence != null)
                {
                    info.Presence = presence.UserPresenceType; // 0=Offline, 1=Online, 2=InGame, 3=Studio
                    info.LastLocation = presence.LastLocation;
                    info.PlaceId = presence.PlaceId;
                    
                    // Try to resolve Game Name if InGame
                    if (info.Presence == 2 && info.PlaceId.HasValue)
                    {
                        var universeId = await GetUniverseIdAsync(info.PlaceId.Value);
                        if (universeId.HasValue)
                        {
                            var gameName = await GetGameNameAsync(universeId.Value);
                            if (!string.IsNullOrEmpty(gameName))
                            {
                                info.LastLocation = gameName; // Override generic "Place" with actual name
                            }
                        }
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                LogService.Error($"Error fetching user info for {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task<long?> GetUniverseIdAsync(long placeId)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                
                string url = $"https://apis.roblox.com/universes/v1/places/{placeId}/universe";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("universeId", out var uId))
                {
                    return uId.GetInt64();
                }
            }
            catch { /* Ignore */ }
            return null;
        }

        public async Task<string?> GetGameNameAsync(long universeId)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                
                string url = $"https://games.roblox.com/v1/games?universeIds={universeId}";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    return data[0].GetProperty("name").GetString();
                }
            }
            catch { /* Ignore */ }
            return null;
        }

        public async Task<string?> GetGameNameFromPlaceIdAsync(long placeId)
        {
            var universeId = await GetUniverseIdAsync(placeId);
            if (universeId.HasValue)
            {
                return await GetGameNameAsync(universeId.Value);
            }
            return null;
        }

        public async Task<RobloxUserPresence?> GetUserPresenceAsync(long userId)
        {
             try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                var payload = new { userIds = new[] { userId } };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://presence.roblox.com/v1/presence/users", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("userPresences", out var presences) && presences.GetArrayLength() > 0)
                    {
                        var p = presences[0];
                        return ParsePresence(p);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error fetching presence: {ex.Message}");
            }
            return null;
        }

        public async Task<(bool success, RobloxUserPresence? presence)> GetAuthenticatedUserPresenceAsync(string cookie, long userId)
        {
            try
            {
                using var client = CreateAuthenticatedClient(cookie);
                var payload = new { userIds = new[] { userId } }; // Even for authenticated, we specify who we are checking (ourselves)
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Note: Presence API usually allows checking others, but for accurate "Game Id" of ourselves, checking with authentication is better/required for some privacy settings.
                var response = await client.PostAsync("https://presence.roblox.com/v1/presence/users", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("userPresences", out var presences) && presences.GetArrayLength() > 0)
                    {
                         var p = presences[0];
                         return (true, ParsePresence(p));
                    }
                }
                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }

        private RobloxUserPresence ParsePresence(JsonElement p)
        {
            return new RobloxUserPresence
            {
                UserPresenceType = p.GetProperty("userPresenceType").GetInt32(),
                LastLocation = p.GetProperty("lastLocation").GetString() ?? "Unknown",
                PlaceId = p.TryGetProperty("placeId", out var pid) && pid.ValueKind == JsonValueKind.Number ? pid.GetInt64() : null,
                GameId = p.TryGetProperty("gameId", out var gid) ? gid.GetString() : null // Add GameId parsing
            };
        }

        public async Task<string?> GetUserAvatarAsync(long userId)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                string url = $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=false";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                
                if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    return data[0].GetProperty("imageUrl").GetString();
                }
            }
            catch
            {
                // Ignore avatar errors
            }
            return null;
        }

        private HttpClient CreateAuthenticatedClient(string cookie)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            var authCookie = new Cookie(".ROBLOSECURITY", cookie)
            {
                Domain = ".roblox.com",
                Path = "/"
            };
            handler.CookieContainer.Add(authCookie);

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            // Referer is often required for Roblox APIs
            client.DefaultRequestHeaders.Add("Referer", "https://www.roblox.com/"); 
            
            return client;
        }

        private async Task<HttpResponseMessage> ExecuteWithCsrfAsync(HttpClient client, string url, HttpContent content)
        {

            var response = await client.PostAsync(url, content);


            if (response.StatusCode == HttpStatusCode.Forbidden && response.Headers.Contains("x-csrf-token"))
            {
                string csrfToken = string.Join("", response.Headers.GetValues("x-csrf-token"));
                
                // LogService.Log($"CSRF Challenge received. Token: {csrfToken.Substring(0, 5)}...");

                // Apply token and retry
                client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);


                response = await client.PostAsync(url, content);
            }

            return response;
        }
    }

    public class RobloxUserInfo
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        
        public int Presence { get; set; } // 0=Offline, 1=Online, 2=InGame, 3=Studio
        public string LastLocation { get; set; } = string.Empty;
        public long? PlaceId { get; set; }

        public string PresenceText
        {
            get
            {
                switch (Presence)
                {
                    case 1: return "Online";
                    case 2: return string.IsNullOrEmpty(LastLocation) ? "In Game" : $"In Game: {LastLocation}";
                    case 3: return "In Studio";
                    default: return "Offline";
                }
            }
        }
        
        public string PresenceColor
        {
             get
            {
                switch (Presence)
                {
                    case 1: return "#00B0FF"; // Blue for Online
                    case 2: return "#00CC66"; // Green for InGame
                    case 3: return "#FF9800"; // Orange for Studio
                    default: return "#999999"; // Grey for Offline
                }
            }
        }
    }
    
    public class RobloxUserPresence
    {
        public int UserPresenceType { get; set; }
        public string LastLocation { get; set; } = "";
        public long? PlaceId { get; set; }
        public string? GameId { get; set; }
    }
}
