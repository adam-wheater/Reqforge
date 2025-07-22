using Newtonsoft.Json;

namespace RocketBoy.Services.Postman
{
    internal class Collection
    {
        public static async Task<Models.Collection?> Read(string path)
        {
            var text = await File.ReadAllTextAsync(path);

            var obj = JsonConvert.DeserializeObject<Models.Collection>(text);

            return obj;
        }
    }
}