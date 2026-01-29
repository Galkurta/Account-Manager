using System;
using System.Diagnostics;
using System.IO;

namespace RobloxAccountManager.Services
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    public struct LogEntry
    {
        public DateTime Time { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }

    public static class LogService
    {
        public static event Action<LogEntry>? OnLogEntry;
        private static readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "latest.log");
        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _logHistory = new List<LogEntry>();
        private const int MaxHistorySize = 2000;

        static LogService()
        {
            // Clear previous log file on startup
            try
            {
                File.WriteAllText(_logFilePath, string.Empty);
            }
            catch { }
        }

        public static IReadOnlyList<LogEntry> GetHistory()
        {
            lock (_lock)
            {
                return new List<LogEntry>(_logHistory);
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info, string source = "General")
        {
            var entry = new LogEntry
            {
                Time = DateTime.Now,
                Level = level,
                Source = source,
                Message = message
            };

            // Format for File: [13:00:00] [INFO] [Source] Message
            string fileFormat = $"[{entry.Time:HH:mm:ss}] [{entry.Level.ToString().ToUpper()}] [{entry.Source}] {entry.Message}";
            
            // Format for Debug/Console
            Debug.WriteLine(fileFormat);
            Console.WriteLine(fileFormat);

            // Write to File and History
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, fileFormat + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }

                _logHistory.Add(entry);
                if (_logHistory.Count > MaxHistorySize)
                {
                    _logHistory.RemoveAt(0);
                }
            }

            // Broadcast to UI (UI handles its own formatting)
            OnLogEntry?.Invoke(entry);
        }

        public static void Error(string message, string source = "General")
        {
            Log(message, LogLevel.Error, source);
        }
    }
}
