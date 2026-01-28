using System;
using System.Diagnostics;

namespace RobloxAccountManager.Services
{
    public static class LogService
    {
        public static event Action<string>? OnLog;

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formatted = $"[{timestamp}] {message}";
            
            // Log to Debug Output (VS)
            Debug.WriteLine(formatted);
            
            // Log to Console (Terminal)
            Console.WriteLine(formatted);

            // Broadcast to UI
            OnLog?.Invoke(formatted);
        }

        public static void Error(string message)
        {
            Log($"[ERROR] {message}");
        }
    }
}
