using Microsoft.Extensions.Logging;
using RocketBoy.Services;

namespace RocketBoy
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<OpenApiService>();
            builder.Services.AddScoped<ZapService>();
            builder.Services.AddScoped<ZapSettings>();
            builder.Services.AddScoped<KeystoreService>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddScoped<CollectionService>();
            builder.Services.AddScoped<OpenApiImportService>();
            builder.Services.AddScoped<RequestStore>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}