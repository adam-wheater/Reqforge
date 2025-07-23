using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RocketBoy.Models;
using RocketBoy.Services;

namespace RocketBoy.Components.Pages
{
    public partial class Setting : ComponentBase
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public SettingsService SettingsService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        private Settings Settings { get; set; } = new Settings();

        protected override async Task OnInitializedAsync()
        {
            Settings = await SettingsService.LoadSettingsAsync();
        }

        private void Keystore() => NavigationManager.NavigateTo("/keystore");

        private async Task SaveSettings()
        {
            await SettingsService.SaveSettingsAsync(Settings);
            await JSRuntime.InvokeVoidAsync("alert", "Settings saved successfully.");
        }
    }
}