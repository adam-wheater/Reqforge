using System.Text.Json;

namespace RocketBoy.Services;

public class SettingsService
{
    private static readonly string DefaultSaveLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RocketBoy", "SavedFiles");
    private static readonly string SettingsFilePath = Path.Combine(DefaultSaveLocation, "settings.json");

    public async Task<SettingsService?> LoadSettingsAsync()
    {
        if (File.Exists(SettingsFilePath))
        {
            var json = await File.ReadAllTextAsync(SettingsFilePath);
            return JsonSerializer.Deserialize<SettingsService>(json);
        }

        return null;
    }

    public async Task SaveSettingsAsync(SettingsService settings)
    {
        EnsureDirectoryExists(SettingsFilePath);

        var json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(SettingsFilePath, json);
    }

    private void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
