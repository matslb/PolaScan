﻿using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;
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
            //builder.Services.AddSingleton(new ImageHandler(polaScanApiService, timeLineService));

            if (Preferences.Default.Get(Constants.Settings.DesitnationPath, string.Empty) == string.Empty)
            {
                Preferences.Default.Set(Constants.Settings.DesitnationPath, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }

            return builder.Build();
        }
    }
}
