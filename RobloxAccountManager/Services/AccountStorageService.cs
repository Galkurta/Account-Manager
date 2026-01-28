using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RobloxAccountManager.Models;

namespace RobloxAccountManager.Services
{
    public class AccountStorageService
    {
        private readonly string _filePath;

        public AccountStorageService()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "RobloxAccountManager");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                _filePath = Path.Combine(folder, "accounts.json");
            }
            catch
            {
                // Fallback to local execution directory if AppData fails
                _filePath = "accounts.json";
            }
        }

        public void SaveAccounts(IEnumerable<RobloxAccount> accounts)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(accounts, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save accounts: {ex.Message}");
            }
        }

        public List<RobloxAccount> LoadAccounts()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<RobloxAccount>();

                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<RobloxAccount>>(json) ?? new List<RobloxAccount>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load accounts: {ex.Message}");
                return new List<RobloxAccount>();
            }
        }
    }
}
