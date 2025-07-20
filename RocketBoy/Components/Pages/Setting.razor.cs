using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RocketBoy.Services;

namespace RocketBoy.Components.Pages
{
    public partial class Setting : ComponentBase
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public SettingsService SettingsService { get; set; }
        public Settings Settings { get; set; } = new Settings();

        protected override async Task OnInitializedAsync()
        {
            Settings = await SettingsService.LoadSettingsAsync() ?? new Settings();
        }

        private async Task SaveSettings()
        {
            await SettingsService.SaveSettingsAsync(Settings);
            await JSRuntime.InvokeVoidAsync("alert", "Settings saved successfully.");
        }
    }
}
