using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Management;
using RobloxAccountManager.Core;
using RobloxAccountManager.Models;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

namespace RobloxAccountManager.Services
{
    public class RobloxProcessManager
    {
        private const string ROBLOX_MUTEX_NAME = "ROBLOX_singletonMutex";
        
        public ObservableCollection<RobloxSession> ActiveSessions { get; } = new ObservableCollection<RobloxSession>();
        private readonly RobloxRequestService _requestService;
        private readonly DiscordWebhookService? _webhookService;

        public event Action<long, long, string>? SessionJobIdUpdated; 
        
        private readonly System.Threading.Timer _monitoringTimer;
        private readonly SettingsService _settingsService;

        public RobloxProcessManager(SettingsService settingsService, DiscordWebhookService? webhookService = null)
        {
            _settingsService = settingsService;
            _requestService = new RobloxRequestService();
            _webhookService = webhookService;
            _monitoringTimer = new System.Threading.Timer(MonitorMemoryUsage, null, 1000, 1000);
        }

        private void MonitorMemoryUsage(object? state)
        {
            if (ActiveSessions.Count == 0) return;

            try
            {
                // Get System Free RAM
                ulong freeRam = 0;
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    freeRam = memStatus.ullAvailPhys;
                }
                
                double freeMb = Math.Round(freeRam / 1024.0 / 1024.0, 0);

                var sessions = ActiveSessions.ToList(); // Snapshot
                foreach (var session in sessions)
                {
                    try
                    {
                        var proc = Process.GetProcessById(session.ProcessId);
                        proc.Refresh(); // Important to get latest stats
                        long usedBytes = proc.WorkingSet64;
                        double usedMb = Math.Round(usedBytes / 1024.0 / 1024.0, 0);

                        // Update on UI Thread if needed, but ObservableObject handles PropertyChanged.
                        // However, since we are on background thread, we should check if dispatching is needed for collection items?
                        // ObservableObject properties raise PropertyChanged. WPF data binding marshals to UI thread automatically for single properties 
                        // typically, but safe to verify.
                        
                        // Actually, updating properties on a background thread for an object bound to UI *usually* throws in WPF unless 
                        // you are using BindingOperations.EnableCollectionSynchronization or dispatching.
                        // Let's modify directly; if it throws we'll add dispatch.
                        
                        // Update values
                        if (session.RamUsageMb != usedMb) session.RamUsageMb = usedMb;
                        if (session.FreeRamMb != freeMb) session.FreeRamMb = freeMb;
                    }
                    catch
                    {
                        // Process might have exited
                    }
                }
            }
            catch (Exception ex)
            {
                 // Log verbose/debug only or throttle? for now error is fine but might span.
                 // Let's us Debug log level equivalent (warning maybe?) to avoid spamming "Success" log view.
                 System.Diagnostics.Debug.WriteLine($"Memory Monitor Error: {ex.Message}");
                 // Also LogService for visibility if severe
                 if (!(ex is System.Threading.Tasks.TaskCanceledException))
                 {
                    LogService.Error($"Memory Monitor Error: {ex.Message}", "Process");
                 }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer); 

        public class RobloxUserInfo
        {
            public long Id { get; set; }
            public string? Name { get; set; }
            public string? DisplayName { get; set; }
            public string? AvatarUrl { get; set; }
        }

        public async System.Threading.Tasks.Task<RobloxUserInfo?> GetUserInfo(string cookie)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Cookie", $".ROBLOSECURITY={cookie}");
                    

                    var response = await client.GetAsync("https://users.roblox.com/v1/users/authenticated");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        // Simple manual parse or use System.Text.Json
                        // {"id":123,"name":"Foo","displayName":"Bar"}
                        
                        using (var doc = System.Text.Json.JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            var info = new RobloxUserInfo
                            {
                                Id = root.GetProperty("id").GetInt64(),
                                Name = root.GetProperty("name").GetString(),
                                DisplayName = root.GetProperty("displayName").GetString()
                            };


                            var thumbResp = await client.GetAsync($"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={info.Id}&size=150x150&format=Png&isCircular=false");
                            if (thumbResp.IsSuccessStatusCode)
                            {
                                var thumbJson = await thumbResp.Content.ReadAsStringAsync();
                                using (var thumbDoc = System.Text.Json.JsonDocument.Parse(thumbJson))
                                {
                                    // {"data":[{"targetId":...,"state":"Completed","imageUrl":"..."}]}
                                    var data = thumbDoc.RootElement.GetProperty("data");
                                    if (data.GetArrayLength() > 0)
                                    {
                                        info.AvatarUrl = data[0].GetProperty("imageUrl").GetString();
                                    }
                                }
                            }
                            // Fallback if avatar fails
                            if (string.IsNullOrEmpty(info.AvatarUrl)) 
                                info.AvatarUrl = "https://tr.rbxcdn.com/53eb9b17fe1432a809c73a132d78f5f1/150/150/AvatarHeadshot/Png";

                            return info;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to fetch user info: {ex.Message}", "Process");
            }
            return null;
        }

        public async System.Threading.Tasks.Task<string> LaunchAccount(string cookie, long userId, string accountName, string? avatarUrl, string? placeId = null, string? jobId = null, string? accessCode = null, string? proxyUrl = null)
        {
            try
            {
                LogService.Log($"Preparing to launch account {(accessCode != null ? "(Private Server)" : "")} {(proxyUrl != null ? "(With Proxy)" : "")}...", LogLevel.Info, "Process");
                

                string authTicket = "";
                try 
                {
                    authTicket = await _requestService.GetAuthenticationTicket(cookie, proxyUrl);
                }
                catch (Exception authEx)
                {
                    var msg = $"Auth Ticket Error: {authEx.Message}";
                    LogService.Error(msg, "Process");
                    return msg;
                }

                if (string.IsNullOrEmpty(authTicket))
                {
                    LogService.Error("Failed to generate authentication ticket (Empty response).", "Process");
                    return "Failed to generate authentication ticket (Empty response).";
                }


                string? playerPath = GetLatestRobloxVersion();
                if (string.IsNullOrEmpty(playerPath))
                {
                    LogService.Error("Roblox Player not found.", "Process");
                    return "Roblox Player not found.";
                }
                
                LogService.Log($"Found Roblox Player at: {playerPath}", LogLevel.Info, "Process");
                

                

                await KillRobloxMutexSafe();
                LogService.Log("Cleaned up mutexes (Safe Mode).", LogLevel.Info, "Process");


                long launchTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                
                // Random BrowserTrackerID per launch (Standard Practice)
                // Matches Electron: Math.floor(Math.random() * 1000000000000)
                long browserTrackerId = (long)(new Random().NextDouble() * 1000000000000);

                // Construct PlaceLauncher URL
                // Base: https://assetgame.roblox.com/game/PlaceLauncher.ashx
                string placeLauncherUrl = "";
                long actualPlaceId = placeId == null ? 1818 : long.Parse(placeId); // Default to Crossroads if null

                // --- Restricted Place Check ---
                // If launching into a Sub-Place directly could cause a loop/fail, fallback to Root Place.
                if (placeId != null) 
                {
                    try 
                    {
                        var rootPlaceId = await _requestService.GetRootPlaceIdAsync(actualPlaceId);
                        if (rootPlaceId.HasValue && rootPlaceId.Value != actualPlaceId)
                        {
                            LogService.Log($"Detected attempt to launch into Sub-Place ({actualPlaceId}). Fallback to Root Place ({rootPlaceId.Value}).", LogLevel.Warning, "Process");
                            actualPlaceId = rootPlaceId.Value;
                            jobId = null; // Invalidate JobID for sub-place
                            // accessCode = null; // Private Servers are usually Root Place bound anyway.
                        }
                    }
                    catch { /* Ignore check failure */ }
                }

                if (!string.IsNullOrWhiteSpace(accessCode))
                {
                     // Private Server
                     placeLauncherUrl = $"https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestPrivateGame&browserTrackerId={browserTrackerId}&placeId={actualPlaceId}&accessCode={accessCode}&linkCode={accessCode}&isPlayTogetherGame=false";
                     LogService.Log($"Joining Private Server via AccessCode", LogLevel.Info, "Process");
                }
                else if (!string.IsNullOrWhiteSpace(jobId))
                {
                     // Specific Server (Job ID)
                     // Electron: request=RequestGameJob&browserTrackerId=...&placeId=...&gameId=...
                     placeLauncherUrl = $"https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestGameJob&browserTrackerId={browserTrackerId}&placeId={actualPlaceId}&gameId={jobId}&isPlayTogetherGame=false";
                     LogService.Log($"Joining via Job ID: {jobId}", LogLevel.Info, "Process");
                }
                else
                {
                     // Standard Game Join
                     // Electron: request=RequestGame&browserTrackerId=...&placeId=...
                     placeLauncherUrl = $"https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestGame&browserTrackerId={browserTrackerId}&placeId={actualPlaceId}&isPlayTogetherGame=false";
                     LogService.Log(placeId == null ? "Launching App (Hub)" : $"Joining Place: {actualPlaceId}", LogLevel.Info, "Process");
                }


                // URL Encode the PlaceLauncher URL
                // IMPORTANT: Use Uri.EscapeDataString to match JS encodeURIComponent (uses %20 instead of +)
                string encodedPlaceLauncherUrl = Uri.EscapeDataString(placeLauncherUrl);


                var settings = new SettingsService().CurrentSettings; 
                if (settings.AutoLaunchExecutor && !string.IsNullOrEmpty(settings.ExecutorPath) && File.Exists(settings.ExecutorPath))
                {
                    string execName = Path.GetFileNameWithoutExtension(settings.ExecutorPath);
                    if (Process.GetProcessesByName(execName).Length == 0)
                    {
                        try
                        {
                            LogService.Log($"Auto-launching: {execName}...", LogLevel.Info, "Executor");
                            Process.Start(new ProcessStartInfo(settings.ExecutorPath) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(settings.ExecutorPath) });
                            await System.Threading.Tasks.Task.Delay(2000); // Give it a moment to initialize
                        }
                        catch (Exception ex)
                        {
                             LogService.Error($"Failed to launch: {ex.Message}", "Executor");
                        }
                    }
                    else
                    {
                         LogService.Log($"{execName} is already running. Skipping launch.", LogLevel.Warning, "Executor");
                    }
                }

                // Construct Protocol URL
                // roblox-player:1+launchmode:play+gameinfo:${authTicket}+launchtime:${launchTime}+placelauncherurl:${encodedUrl}+browsertrackerid:${trackerId}+robloxLocale:en_us+gameLocale:en_us+channel:+LaunchExp:InApp
                string protocolUrl = $"roblox-player:1+launchmode:play+gameinfo:{authTicket}+launchtime:{launchTime}+placelauncherurl:{encodedPlaceLauncherUrl}+browsertrackerid:{browserTrackerId}+robloxLocale:en_us+gameLocale:en_us+channel:+LaunchExp:InApp";

                LogService.Log($"Launching Protocol URL (TrackerID: {browserTrackerId})", LogLevel.Info, "Process");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{protocolUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (!string.IsNullOrEmpty(proxyUrl))
                {
                    // Attempt to pass proxy env vars
                    // Note: 'start' command usually drops env vars unless we are careful, 
                    // and 'cmd' env vars won't necessarily reach the protocol handler (Roblox)
                    // if it is already running or managed by Explorer. 
                    // But we set them anyway as best effort.
                    startInfo.EnvironmentVariables["HTTP_PROXY"] = proxyUrl;
                    startInfo.EnvironmentVariables["HTTPS_PROXY"] = proxyUrl;
                    startInfo.EnvironmentVariables["ALL_PROXY"] = proxyUrl;
                }

                Process? ret = Process.Start(startInfo);
                if (ret == null) 
                {
                    LogService.Error("Failed to start launch command.", "Process");
                    _webhookService?.SendNotificationAsync("Launch Failed", $"Failed to start process for **{accountName}**.", 16711680, accountName, userId, placeId, null, avatarUrl); // Red
                    return "Failed to start launch command.";
                }

                // Fetch Place/Map Name
                string? placeName = null;
                if (!string.IsNullOrEmpty(placeId) && long.TryParse(placeId, out long pid))
                {
                    placeName = await _requestService.GetGameNameFromPlaceIdAsync(pid);
                }

                // Determine Server Type
                string serverType = "Public Server";
                if (!string.IsNullOrEmpty(accessCode)) serverType = "Private Server";
                else if (!string.IsNullOrEmpty(jobId)) serverType = "Reserved/Job";

                _webhookService?.SendNotificationAsync("Account Launched", $"Successfully launched **{accountName}**.", 65280, accountName, userId, placeId, jobId, avatarUrl, placeName, serverType); // Green

                // Start async tracking of the real process
                
                TrackLaunchedSession(browserTrackerId, userId, accountName, avatarUrl, placeName, placeId, jobId, serverType); 

                // Wait a moment for process initialization then clean again
                await System.Threading.Tasks.Task.Delay(2000);
                await KillRobloxMutexSafe();
                LogService.Log("Post-launch mutex cleanup complete.", LogLevel.Info, "Process");
                
                return $"Launched via Protocol (TrackerID: {browserTrackerId})";
            }
            catch (Exception ex)
            {
                LogService.Error($"Error launching: {ex.Message}", "Process");
                return $"Error launching: {ex.Message}";
            }
        }

        private async void TrackLaunchedSession(long browserTrackerId, long userId, string accountName, string? avatarUrl, string? placeName, string? placeId, string? jobId, string? serverType)
        {
             // Wait for the actual RobloxPlayerBeta process to appear
             LogService.Log($"Tracking session for {accountName} (ID: {browserTrackerId})...", LogLevel.Info, "Process");
             
             int attempts = 0;
             while (attempts < 20) // Try for 20 seconds
             {
                 await System.Threading.Tasks.Task.Delay(1000);
                 attempts++;

                 var processes = Process.GetProcessesByName("RobloxPlayerBeta");
                 foreach (var p in processes)
                 {
                     // Check if we are already tracking this PID
                     if (System.Linq.Enumerable.Any(ActiveSessions, s => s.ProcessId == p.Id)) continue;
                     
                     // Check Command Line for BrowserTrackerId
                     string? cmdLine = GetCommandLine(p.Id);
                     if (cmdLine != null && cmdLine.Contains(browserTrackerId.ToString()))
                     {
                         // Found it!
                         var session = new RobloxSession
                         {
                             ProcessId = p.Id,
                             AccountName = accountName,
                             UserId = userId,
                             LaunchTime = DateTime.Now,
                             BrowserTrackerId = browserTrackerId,
                              LaunchMode = "Protocol",
                              Status = "Running",
                              PlaceId = placeId,
                              JobId = jobId,
                              PlaceName = placeName ?? "Unknown",
                              AvatarUrl = avatarUrl,
                              ServerType = serverType
                         };

                         // Hook exit
                         p.EnableRaisingEvents = true;
                         p.Exited += (s, e) => 
                         {
                             session.Status = "Closed";
                             System.Windows.Application.Current.Dispatcher.Invoke(() => ActiveSessions.Remove(session));

                             // Trigger Webhook
                             long.TryParse(session.PlaceId, out long pid);
                             _webhookService?.SendNotificationAsync("Account Disconnected", $"**{session.AccountName}** closed session.", 16753920, session.AccountName, session.UserId, session.PlaceId, session.JobId, session.AvatarUrl, session.PlaceName, session.ServerType);
                         };

                         System.Windows.Application.Current.Dispatcher.Invoke(() => ActiveSessions.Add(session));
                         LogService.Log($"Session Linked: PID {p.Id} -> {accountName}", LogLevel.Success, "Process");
                         
                         // Start reading logs to find JobId/PlaceName if missing
                         _ = Task.Run(() => MonitorLogFile(session));

                         return;
                     }
                 }
             }
             LogService.Error($"Starting session tracking failed for {accountName} (Process not found).", "Process");
        }

        private async Task MonitorLogFile(RobloxSession session)
        {
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "logs");
            if (!Directory.Exists(logDir)) return;

            // Wait for log file to appear (max 30s)
            FileInfo? logFile = null;
            DateTime minCreationTime = session.LaunchTime.AddSeconds(-10); // Buffer
            
            for (int i = 0; i < 30; i++)
            {
                try 
                {
                    var file = new DirectoryInfo(logDir).GetFiles("*.log")
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault(f => f.CreationTime >= minCreationTime && f.LastWriteTime >= minCreationTime);
                        
                    if (file != null)
                    {
                        // Double check: if multiple accounts launching, try to guess by locking?
                        // Or just scan for JobId.
                        logFile = file;
                        break;
                    }
                }
                catch { }
                await Task.Delay(1000);
            }

            if (logFile == null) return;
            LogService.Log($"Found Log: {logFile.Name} for Session {session.BrowserTrackerId}", LogLevel.Info, "Monitor");

            // Monitor log
            try 
            {
                 using (var stream = new FileStream(logFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                 using (var reader = new StreamReader(stream))
                 {
                     while (session.Status == "Running")
                     {
                         string? line = await reader.ReadLineAsync();
                         if (line != null)
                         {
                             // Check for JobId
                             if (string.IsNullOrEmpty(session.JobId))
                             {
                                 // Pattern 1: Explicit JobId=...
                                 var match = Regex.Match(line, @"JobId=\s*([a-fA-F0-9\-]{36})");
                                 if (match.Success)
                                 {
                                     AssignJobId(session, match.Groups[1].Value);
                                 }
                                 else
                                 {
                                     // Pattern 2: ! Joining game '...' place ...
                                     match = Regex.Match(line, @"! Joining game '(?<jobid>[a-fA-F0-9\-]{36})' place (?<placeid>\d+)");
                                     if (match.Success)
                                     {
                                          AssignJobId(session, match.Groups["jobid"].Value);
                                          
                                          // Update PlaceLogic
                                          string pidStr = match.Groups["placeid"].Value;
                                          if (string.IsNullOrEmpty(session.PlaceId) || session.PlaceId != pidStr)
                                          {
                                              session.PlaceId = pidStr;
                                              if (session.PlaceName == "Unknown" && long.TryParse(pidStr, out long pid))
                                              {
                                                  string? newName = await _requestService.GetGameNameFromPlaceIdAsync(pid);
                                                  if (!string.IsNullOrEmpty(newName)) session.PlaceName = newName;
                                              }
                                          }
                                     }
                                     else
                                     {
                                         // Fallback: just JobId
                                         match = Regex.Match(line, @"! Joining game '(?<jobid>[a-fA-F0-9\-]{36})'");
                                         if (match.Success) AssignJobId(session, match.Groups["jobid"].Value);
                                     }
                                 }
                             }
                             
                             // Check for Universe/Place via connection logs if possible
                             // Or if JobId is found, we assume we joined.
                             
                             // Check for Disconnect
                         }
                         else
                         {
                             // End of stream, wait
                             await Task.Delay(1000);
                             // Check if process is still alive? session.Status handles that via Exited event.
                         }
                         
                         // Stop if we have what we need?
                         // User wants JobId. If found, we can maybe stop? 
                         // But users might switch games? 
                         // Teleports change JobId.
                         // So we should keep monitoring.
                     }
                 }
            }
            catch (Exception ex)
            {
                 LogService.Error($"Error reading log: {ex.Message}", "Monitor");
            }
        }



        private void AssignJobId(RobloxSession session, string jid)
        {
             session.JobId = jid;
             session.Status = "In Game"; 
             LogService.Log($"Found JobId: {jid}", LogLevel.Success, "Monitor");

             // Notify listeners (AutoJoinService)
             long.TryParse(session.PlaceId, out long pid);
             SessionJobIdUpdated?.Invoke(session.UserId, pid, jid);
        }

        private string? GetCommandLine(int processId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}"))
                using (var objects = searcher.Get())
                {
                   foreach (var obj in objects)
                   {
                       return obj["CommandLine"]?.ToString();
                   }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"GetCommandLine Failed: {ex.Message}", "Process");
            }
            return null;
        }

        public void CloseAllRobloxMutexes()
        {
            var processes = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (var p in processes)
            {
                try
                {
                    CloseRobloxMutex(p.Id);
                }
                catch 
                {
                    // Ignore access denied etc on individual processes
                }
            }
        }

        // Private GetAuthenticationTicket removed in favor of RobloxRequestService


        private string? GetLatestRobloxVersion()
        {
            try
            {
                // 1. Check Custom Path from Settings first
                string customPath = _settingsService.CurrentSettings.CustomRobloxPath;

                if (!string.IsNullOrWhiteSpace(customPath) && System.IO.Directory.Exists(customPath))
                {
                    var dir = new System.IO.DirectoryInfo(customPath);
                    
                    // Case A: Direct version folder
                    string directExe = System.IO.Path.Combine(customPath, "RobloxPlayerBeta.exe");
                    if (System.IO.File.Exists(directExe)) return directExe;

                    // Case B: Root Versions folder
                    var latest = System.Linq.Enumerable.OrderByDescending(
                        dir.GetDirectories().Where(d => d.Name.StartsWith("version-") && System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "RobloxPlayerBeta.exe"))), 
                        d => d.LastWriteTime).FirstOrDefault();
                    
                    if (latest != null)
                    {
                        return System.IO.Path.Combine(latest.FullName, "RobloxPlayerBeta.exe");
                    }
                }

                // 2. Default Search Logic
                var candidatePaths = new List<string>
                {
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions"),
                    @"C:\Program Files (x86)\Roblox\Versions"
                };

                foreach (var versionsPath in candidatePaths)
                {
                    if (System.IO.Directory.Exists(versionsPath))
                    {
                        var dir = new System.IO.DirectoryInfo(versionsPath);
                        var latest = System.Linq.Enumerable.OrderByDescending(dir.GetDirectories(), d => d.LastWriteTime).FirstOrDefault();
                        
                        if (latest != null)
                        {
                            string exePath = System.IO.Path.Combine(latest.FullName, "RobloxPlayerBeta.exe");
                            if (System.IO.File.Exists(exePath)) return exePath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Error finding Roblox version: {ex.Message}");
            }
            return null;
        }

        public bool CloseRobloxMutex(int processId)
        {
            var handles = GetSystemHandles();
            foreach (var handleEntry in handles)
            {
                if (handleEntry.ProcessId != processId) continue;

                string? handleName = GetHandleName(handleEntry, processId);
                if (handleName != null && handleName.Contains(ROBLOX_MUTEX_NAME))
                {
                    return CloseRemoteHandle(handleEntry, processId);
                }
            }
            return false;
        }

        public void CloseAccountSession(long userId)
        {
            try
            {
                var session = ActiveSessions.FirstOrDefault(s => s.UserId == userId);
                if (session != null)
                {
                    try
                    {
                        var proc = Process.GetProcessById(session.ProcessId);
                        proc.Kill();
                        LogService.Log($"[ProcessManager] Killed process {session.ProcessId} for User {userId}");
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"[ProcessManager] Failed to kill process: {ex.Message}");
                    }
                    
                    // Allow the Exited event to handle removal, or force it here if needed
                    // But Exited event is safer.
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"[ProcessManager] Error closing session: {ex.Message}");
            }
        }

        private List<NativeHelper.SYSTEM_HANDLE_INFORMATION> GetSystemHandles()
        {
            List<NativeHelper.SYSTEM_HANDLE_INFORMATION> handleList = new List<NativeHelper.SYSTEM_HANDLE_INFORMATION>();
            int initSize = 0x10000;
            IntPtr buffer = Marshal.AllocHGlobal(initSize);
            int returnLength = 0;

            try
            {
                while (NativeHelper.NtQuerySystemInformation(
                    NativeHelper.SYSTEM_INFORMATION_CLASS.SystemHandleInformation,
                    buffer,
                    initSize,
                    out returnLength) == NativeHelper.STATUS_INFO_LENGTH_MISMATCH)
                {
                    initSize = returnLength;
                    Marshal.FreeHGlobal(buffer);
                    buffer = Marshal.AllocHGlobal(initSize);
                }
                
                long handleCount = Marshal.ReadIntPtr(buffer).ToInt64();
                IntPtr ptr = new IntPtr(buffer.ToInt64() + IntPtr.Size);
                int structSize = Marshal.SizeOf(typeof(NativeHelper.SYSTEM_HANDLE_INFORMATION));

                for (int i = 0; i < handleCount; i++)
                {
                    NativeHelper.SYSTEM_HANDLE_INFORMATION info = 
                        Marshal.PtrToStructure<NativeHelper.SYSTEM_HANDLE_INFORMATION>(ptr);
                    handleList.Add(info);
                    ptr = new IntPtr(ptr.ToInt64() + structSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return handleList;
        }

        private string? GetHandleName(NativeHelper.SYSTEM_HANDLE_INFORMATION handleInfo, int processId)
        {
            IntPtr sourceProcessHandle = IntPtr.Zero;
            IntPtr duplicatedHandle = IntPtr.Zero;

            try
            {
                sourceProcessHandle = NativeHelper.OpenProcess(
                    NativeHelper.ProcessAccessFlags.DuplicateHandle,
                    false,
                    processId);

                if (sourceProcessHandle == IntPtr.Zero) return null;

                if (!NativeHelper.DuplicateHandle(
                    sourceProcessHandle,
                    (IntPtr)handleInfo.Handle,
                    NativeHelper.GetCurrentProcess(),
                    out duplicatedHandle,
                    0,
                    false,
                    NativeHelper.DUPLICATE_SAME_ACCESS))
                {
                    return null;
                }

                return QueryObjectName(duplicatedHandle);
            }
            finally
            {
                if (duplicatedHandle != IntPtr.Zero) NativeHelper.CloseHandle(duplicatedHandle);
                if (sourceProcessHandle != IntPtr.Zero) NativeHelper.CloseHandle(sourceProcessHandle);
            }
        }

        private string? QueryObjectName(IntPtr handle)
        {
            int length = 0x1000;
            IntPtr buffer = Marshal.AllocHGlobal(length);
            int returnLength;

            try
            {
                uint status = NativeHelper.NtQueryObject(
                    handle,
                    NativeHelper.OBJECT_INFORMATION_CLASS.ObjectNameInformation,
                    buffer,
                    length,
                    out returnLength
                );

                if (status == NativeHelper.STATUS_SUCCESS)
                {
                    IntPtr stringPtr = Marshal.ReadIntPtr(buffer, IntPtr.Size == 8 ? 8 : 4);
                    if (stringPtr != IntPtr.Zero)
                        return Marshal.PtrToStringUni(stringPtr);
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return null;
        }

        private bool CloseRemoteHandle(NativeHelper.SYSTEM_HANDLE_INFORMATION handleInfo, int processId)
        {
             IntPtr sourceProcessHandle = IntPtr.Zero;
             IntPtr targetHandle = IntPtr.Zero;
             try
             {
                 sourceProcessHandle = NativeHelper.OpenProcess(
                     NativeHelper.ProcessAccessFlags.DuplicateHandle,
                     false,
                     processId);
                 
                 // DUPLICATE_CLOSE_SOURCE closes it in the source process.
                 // We don't really want the handle in our process, but DuplicateHandle requires a target.
                 // We can target our own process and then close it immediately.
                 return NativeHelper.DuplicateHandle(
                     sourceProcessHandle,
                     (IntPtr)handleInfo.Handle,
                     NativeHelper.GetCurrentProcess(),
                     out targetHandle,
                     0,
                     false,
                     NativeHelper.DUPLICATE_CLOSE_SOURCE);
             }
             finally
             {
                 if (targetHandle != IntPtr.Zero)
                     NativeHelper.CloseHandle(targetHandle);

                 if (sourceProcessHandle != IntPtr.Zero)
                    NativeHelper.CloseHandle(sourceProcessHandle);
             }
        }
        // NEW: Safe Mutex Cleanup using PowerShell (Matches Electron)
        private async System.Threading.Tasks.Task KillRobloxMutexSafe()
        {
            try
            {
               await System.Threading.Tasks.Task.Run(() =>
               {
                   string script = @"
$names = @('ROBLOX_singletonEvent', 'ROBLOX_singletonMutex')
foreach ($name in $names) {
  try {
    $mutex = [System.Threading.Mutex]::OpenExisting($name)
    if ($mutex) {
      try { $mutex.ReleaseMutex() } catch {}
      $mutex.Close()
      $mutex.Dispose()
    }
  } catch {
    try {
      $evt = [System.Threading.EventWaitHandle]::OpenExisting($name)
      if ($evt) {
        $evt.Close()
        $evt.Dispose()
      }
    } catch {}
  }
}";
                   string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
                   var psi = new ProcessStartInfo
                   {
                       FileName = "powershell",
                       Arguments = $"-EncodedCommand {encoded}",
                       UseShellExecute = false,
                       CreateNoWindow = true
                   };
                   Process.Start(psi)?.WaitForExit();
               });
            }
            catch (Exception ex)
            {
                LogService.Error($"Error cleaning mutex: {ex.Message}");
            }
        }


    }
}
