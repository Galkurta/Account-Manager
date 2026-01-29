using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RobloxAccountManager.Models;

namespace RobloxAccountManager.Services
{
    public class SettingsService
    {
        private const string FILE_NAME = "settings.json";
        private readonly string _filePath;
        
        public AppSettings CurrentSettings { get; private set; }

        public SettingsService()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "RobloxAccountManager");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                _filePath = Path.Combine(folder, FILE_NAME);

                // Migration: Check if old settings exist in BaseDirectory and move/copy them
                string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FILE_NAME);
                if (File.Exists(oldPath) && !File.Exists(_filePath))
                {
                    try 
                    {
                        File.Copy(oldPath, _filePath); 
                        System.Diagnostics.Debug.WriteLine("Migrated settings from local to AppData");
                    } 
                    catch { }
                }
            }
            catch
            {
                // Fallback
                _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FILE_NAME);
            }

            CurrentSettings = new AppSettings();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
