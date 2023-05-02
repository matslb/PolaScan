using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
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

            var polaScanApiService = new PolaScanApiService(); 
            var timeLineService = new GoogleTimelineService();
            builder.Services.AddSingleton(polaScanApiService);
            builder.Services.AddSingleton(timeLineService);
            builder.Services.AddSingleton(new ImageHandler(polaScanApiService, timeLineService));

            return builder.Build();
        }
    }
}
