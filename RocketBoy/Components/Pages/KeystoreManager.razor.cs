using Microsoft.AspNetCore.Components;
using RocketBoy.Services;

namespace RocketBoy.Components.Pages;

public partial class KeystoreManager : ComponentBase
{
    [Inject] public KeystoreService KeystoreService { get; set; }
    private string KeyName { get; set; } = string.Empty;
    private string KeyValue { get; set; } = string.Empty;
    private Dictionary<string, string> Keys => KeystoreService.Keys;
    private string ZapApiKey { get; set; } = string.Empty;
    private string ZapBaseUrl { get; set; } = "http://localhost:8080";

    protected override async Task OnInitializedAsync()
    {
        await KeystoreService.LoadKeys();
        ZapApiKey = KeystoreService.GetKey("ZapApiKey") ?? string.Empty;
        ZapBaseUrl = KeystoreService.GetKey("ZapBaseUrl") ?? "http://localhost:8080";
    }

    private async Task SaveZapSettings()
    {
        if (!string.IsNullOrEmpty(ZapApiKey))
        {
            KeystoreService.SetKey("ZapApiKey", ZapApiKey);
        }

        if (!string.IsNullOrEmpty(ZapBaseUrl))
        {
            KeystoreService.SetKey("ZapBaseUrl", ZapBaseUrl);
        }

        await KeystoreService.SaveKeys();
    }

    private async Task SaveKey()
    {
        if (!string.IsNullOrEmpty(KeyName) && !string.IsNullOrEmpty(KeyValue))
        {
            KeystoreService.SetKey(KeyName, KeyValue);
            await KeystoreService.SaveKeys();
            KeyName = string.Empty;
            KeyValue = string.Empty;
        }
    }

    private async Task DeleteKey()
    {
        if (!string.IsNullOrEmpty(KeyName))
        {
            KeystoreService.RemoveKey(KeyName);
            await KeystoreService.SaveKeys();
            KeyName = string.Empty;
            KeyValue = string.Empty;
        }
    }
}