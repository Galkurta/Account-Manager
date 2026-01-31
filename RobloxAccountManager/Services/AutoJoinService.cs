using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RobloxAccountManager.Services
{
    public class AutoJoinService
    {
        private readonly RobloxRequestService _requestService;
        private readonly SettingsService _settingsService;
        private CancellationTokenSource? _cts;
        private bool _isRunning;


        private class AutoJoinSession
        {
            public long UserId { get; set; }
            public string AuthCookie { get; set; } = string.Empty;
            public Guid? LastGameId { get; set; }
            public long? PlaceId { get; set; } // Required for protocol launch
        }

        private readonly ConcurrentDictionary<long, AutoJoinSession> _monitoredSessions = new();

        public int CheckIntervalSeconds { get; set; } = 5;


        public event Action<string>? OnLog;
        public event Action<bool>? OnStatusChanged; // Global running status
        public event Action<long, string>? OnSessionStatusChanged; // Per-session status updates (UserId, StatusMessage)
        
 
        public Func<long, string?, string, Task>? RelaunchCallback { get; set; }

        public bool IsRunning => _isRunning;

        public int AutoRejoinDelaySeconds
        {
            get => _settingsService.CurrentSettings.AutoRejoinDelaySeconds;
            set
            {
                _settingsService.CurrentSettings.AutoRejoinDelaySeconds = value;
                _settingsService.SaveSettings();
            }
        }

        private readonly DiscordWebhookService? _webhookService;

        public AutoJoinService(RobloxRequestService requestService, DiscordWebhookService? webhookService = null)
        {
            _requestService = requestService;
            _settingsService = new SettingsService();
            _webhookService = webhookService;
        }

        public void StartMonitoring(long userId, string cookie)
        {
            if (_monitoredSessions.ContainsKey(userId)) return;

            var session = new AutoJoinSession 
            { 
                UserId = userId, 
                AuthCookie = cookie,
                LastGameId = null,
                PlaceId = null
            };
            
            _monitoredSessions.TryAdd(userId, session);
            Log($"Started monitoring user: {userId}");

            // Ensure loop is running
            if (!_isRunning) StartLoop();
        }

        public void StopMonitoring(long userId)
        {
            if (_monitoredSessions.TryRemove(userId, out _))
            {
                Log($"Stopped monitoring user: {userId}");
            }

            if (_monitoredSessions.IsEmpty)
            {
                StopLoop();
            }
        }

        public bool IsMonitoring(long userId)
        {
            return _monitoredSessions.ContainsKey(userId);
        }

        private void StartLoop()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _cts = new CancellationTokenSource();
            OnStatusChanged?.Invoke(true);
            
            Log("Auto Rejoiner Service Started.");
            Task.Run(() => LoopAsync(_cts.Token));
        }

        private void StopLoop()
        {
            if (!_isRunning) return;
            
            _cts?.Cancel();
            _isRunning = false;
            OnStatusChanged?.Invoke(false);
            Log("Auto Rejoiner Service Stopped (No accounts).");
        }

        private async Task LoopAsync(CancellationToken token)
        {
            try
            {
                 // Initial Delay to let things settle?
                 await Task.Delay(2000, token);

                while (!token.IsCancellationRequested)
                {
                    // Check all sessions
                    foreach (var kvp in _monitoredSessions)
                    {
                        var session = kvp.Value;
                        await CheckSessionAndRejoinIfNeeded(session, token);
                    }

                    await Task.Delay(CheckIntervalSeconds * 1000, token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Log($"Error in loop: {ex.Message}");
                StopLoop();
            }
        }

        private async Task CheckSessionAndRejoinIfNeeded(AutoJoinSession session, CancellationToken token)
        {
            try
            {

                var (success, presence) = await _requestService.GetAuthenticatedUserPresenceAsync(session.AuthCookie, session.UserId);
                
                if (!success || presence == null) 
                {
                    Log($"[{session.UserId}] Presence check failed (API Error).");
                    return; 
                }

                bool isInGame = presence.UserPresenceType == 2;
                // Log($"[{session.UserId}] Presence: {presence.UserPresenceType} ({presence.LastLocation})");
                Guid? currentGameId = null;
                if (isInGame && !string.IsNullOrEmpty(presence.GameId) && Guid.TryParse(presence.GameId, out var gid))
                {
                    currentGameId = gid;
                    // If we have presence, we likely have PlaceId too if request service provides it? 
                    // GetAuthenticatedUserPresenceAsync returns UserPresence which has PlaceId.
                    if (presence.PlaceId != null) session.PlaceId = presence.PlaceId;
                }


                if (isInGame)
                {
                    OnSessionStatusChanged?.Invoke(session.UserId, "Playing");
                    // Update knowledge if changed
                    if (session.LastGameId != currentGameId)
                    {
                        session.LastGameId = currentGameId;
                        // Log($"[{session.UserId}] In-Game: {currentGameId}");
                    }
                }
                else
                {
                    // Disconnect detected: User was in-game but is now idle
                    if (session.LastGameId.HasValue)
                    {
                        if (session.PlaceId.HasValue)
                        {
                            Log($"[{session.UserId}] Disconnect detected! Waiting {_settingsService.CurrentSettings.AutoRejoinDelaySeconds}s to verify...");
                            OnSessionStatusChanged?.Invoke(session.UserId, $"Waiting {_settingsService.CurrentSettings.AutoRejoinDelaySeconds}s...");
                            
                            // Webhook handled by ProcessManager.Exited

                            await Task.Delay(_settingsService.CurrentSettings.AutoRejoinDelaySeconds * 1000, token);
                            
                            // Double Check: Did they just teleport?
                            var (retrySuccess, retryPresence) = await _requestService.GetAuthenticatedUserPresenceAsync(session.AuthCookie, session.UserId);
                            if (retrySuccess && retryPresence != null && retryPresence.UserPresenceType == 2)
                            {
                                Log($"[{session.UserId}] User reconnected/teleported. Rejoin cancelled.");
                                OnSessionStatusChanged?.Invoke(session.UserId, "Playing");

                                // Update info
                                if (!string.IsNullOrEmpty(retryPresence.GameId) && Guid.TryParse(retryPresence.GameId, out var newGid))
                                {
                                    session.LastGameId = newGid;
                                }
                                if (retryPresence.PlaceId != null) session.PlaceId = retryPresence.PlaceId;
                                return;
                            }

                            OnSessionStatusChanged?.Invoke(session.UserId, "Rejoining...");

                            if (RelaunchCallback != null)
                            {
                                string? jobId = session.LastGameId.Value.ToString();
                                string placeId = session.PlaceId.Value.ToString();
                                
                                // Check if this is a sub-place (Restricted?)
                                var rootPlaceId = await _requestService.GetRootPlaceIdAsync(session.PlaceId.Value);
                                if (rootPlaceId.HasValue && rootPlaceId.Value != session.PlaceId.Value)
                                {
                                    Log($"[{session.UserId}] Detected sub-place ({session.PlaceId}). Falling back to Root Place ({rootPlaceId}) to avoid launch loop.");
                                    placeId = rootPlaceId.Value.ToString();
                                    jobId = null; // Cannot use sub-place JobId for Root Place
                                }

                                await RelaunchCallback.Invoke(session.UserId, jobId, placeId);
                            }
                        }
                        else
                        {
                            // Missing PlaceID
                            Log($"[{session.UserId}] Disconnect, but missing Place ID. Cannot rejoin.");
                            OnSessionStatusChanged?.Invoke(session.UserId, "Error: Missing PlaceID");
                            session.LastGameId = null;
                        }
                    }
                    else
                    {
                         OnSessionStatusChanged?.Invoke(session.UserId, "Monitoring (Idle)");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[{session.UserId}] Error checking: {ex.Message}");
                OnSessionStatusChanged?.Invoke(session.UserId, "Error Check");
            }
        }
        

        public void UpdateSessionInfo(long userId, long placeId, string jobId)
        {
             if (_monitoredSessions.TryGetValue(userId, out var session))
             {
                 if (Guid.TryParse(jobId, out var gid))
                 {
                    session.LastGameId = gid;
                    session.PlaceId = placeId;
                 }
             }
        }

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            LogService.Log(message, LogLevel.Info, "AutoJoin"); 
        }
    }
}
