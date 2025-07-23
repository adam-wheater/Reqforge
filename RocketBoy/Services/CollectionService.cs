// Services/CollectionService.cs

using RocketBoy.Models;
using System.Text.Json;

namespace RocketBoy.Services
{
    public class CollectionService
    {
        private readonly SettingsService _settingsService;

        public CollectionService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // Directory for saved collections, from your SettingsService
        private string CollectionsDir =>
            _settingsService
                .LoadSettingsAsync()
                .Result
                .CollectionsSaveLocation;

        public async Task<List<Collection>> LoadAllAsync()
        {
            var dir = CollectionsDir;
            if (!Directory.Exists(dir))
                return new List<Collection>();

            var result = new List<Collection>();
            foreach (string file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    Collection? col = JsonSerializer.Deserialize<Collection>(json);
                    if (col != null)
                        result.Add(col);
                }
                catch
                {
                    // skip malformed or incompatible files
                }
            }
            return result;
        }

        public async Task SaveAsync(Collection collection)
        {
            Directory.CreateDirectory(CollectionsDir);

            // sanitize filename
            string safeName = string.Join("_",
                collection.Name.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(CollectionsDir, $"{safeName}.json");

            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(collection, opts);
            await File.WriteAllTextAsync(path, json);
        }
    }
}