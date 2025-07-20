using Microsoft.JSInterop;
using System.Text.Json;

namespace RocketBoy.Services
{
    public class KeystoreService
    {
        private readonly IJSRuntime _jsRuntime;
        private Dictionary<string, string> _keys;

        public KeystoreService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _keys = new Dictionary<string, string>();
        }

        public Dictionary<string, string> Keys => _keys;

        public async Task LoadKeys()
        {
            var keysJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "keys");
            if (!string.IsNullOrEmpty(keysJson))
            {
                _keys = JsonSerializer.Deserialize<Dictionary<string, string>>(keysJson) ?? new Dictionary<string, string>();
            }
        }

        public async Task SaveKeys()
        {
            var keysJson = JsonSerializer.Serialize(_keys);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "keys", keysJson);
        }

        public void SetKey(string key, string value)
        {
            if (_keys.ContainsKey(key))
            {
                _keys[key] = value;
            }
            else
            {
                _keys.Add(key, value);
            }
        }

        public void RemoveKey(string key)
        {
            if (_keys.ContainsKey(key))
            {
                _keys.Remove(key);
            }
        }

        public string GetKey(string key)
        {
            _keys.TryGetValue(key, out var value);
            return value;
        }
    }
}
