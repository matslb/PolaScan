using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PolaScan.App.Models;
using PolaScan.App.Services;
using System.Globalization;
using System.Reflection;

namespace PolaScan.App;

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

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PolaScan.App.appsettings.json");
        var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
        builder.Configuration.AddConfiguration(config);

        SettingsSetup();

        builder.AddServices();
        builder.AddApplicationInsights();


        return builder.Build();
    }

    private static void AddApplicationInsights(this MauiAppBuilder builder)
    {
        var tconfig = TelemetryConfiguration.CreateDefault();
        tconfig.ConnectionString = builder.Configuration["ApplicationInsights"];
        tconfig.TelemetryInitializers.Add(new ApplicationInitializer());
        var telemetryClient = new TelemetryClient(tconfig);
        builder.Services.AddSingleton(telemetryClient);
    }

    private static void AddServices(this MauiAppBuilder builder)
    {
        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton<PolaScanApiService>();
        builder.Services.AddSingleton<GoogleTimelineService>();
        builder.Services.AddSingleton<ImageHandler>();
    }

    private static void SettingsSetup()
    {
        if (Preferences.Default.Get(Constants.Settings.DesitnationPath, string.Empty) == string.Empty)
        {
            Preferences.Default.Set(Constants.Settings.DesitnationPath, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        if (Preferences.Default.Get(Constants.Settings.DateFormat, string.Empty) == string.Empty)
        {
            Preferences.Default.Set(Constants.Settings.DateFormat, CultureInfo.CurrentCulture.Name);
        }

        if (Preferences.Default.Get(Constants.Settings.AssumedHour, string.Empty) == string.Empty)
        {
            Preferences.Default.Set(Constants.Settings.AssumedHour, "18");
        }

        if (Preferences.Default.Get(Constants.Settings.DeviceId, string.Empty) == string.Empty)
        {
            Preferences.Default.Set(Constants.Settings.DeviceId, Guid.NewGuid().ToString());
        }

        Preferences.Default.Set(Constants.Settings.HideAlert, false);
        Preferences.Default.Set(nameof(ProcessingState), JsonConvert.SerializeObject(new ProcessingState()));
        Helpers.DeleteTemporaryFiles();
    }
}
