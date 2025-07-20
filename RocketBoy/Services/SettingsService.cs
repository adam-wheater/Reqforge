using RocketBoy.Models;
using System.Text.Json;

namespace RocketBoy.Services
{
    public class SettingsService
    {
        private static readonly string DefaultSaveLocation =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "RocketBoy", "SavedFiles");
        private static readonly string SettingsFilePath =
            Path.Combine(DefaultSaveLocation, "settings.json");

        public async Task<Settings> LoadSettingsAsync()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                return JsonSerializer.Deserialize<Settings>(json)!
                       ?? new Settings();
            }
            return new Settings();
        }

        public async Task SaveSettingsAsync(Settings settings)
        {
            var dir = Path.GetDirectoryName(SettingsFilePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
    }
}
