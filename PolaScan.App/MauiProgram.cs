using System.Security.Principal;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PolaScan.App.Models;
using PolaScan.App.Services;

namespace PolaScan.App
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
                })
                .UseMauiCommunityToolkit();

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            builder.Services.AddSingleton(FolderPicker.Default);

            builder.Services.AddTransient<PolaScanApiService, PolaScanApiService>();
            builder.Services.AddSingleton<GoogleTimelineService, GoogleTimelineService>();
            builder.Services.AddSingleton<ImageHandler, ImageHandler>();

            if (Preferences.Default.Get(Constants.Settings.DesitnationPath, string.Empty) == string.Empty)
            {
                Preferences.Default.Set(Constants.Settings.DesitnationPath, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }
            Preferences.Default.Set(Constants.Settings.HideAlert, false);

            Preferences.Default.Set(nameof(ProcessingState), JsonConvert.SerializeObject(new ProcessingState()));
            Helpers.DeleteTemporaryFiles();

            return builder.Build();
        }
    }
}
