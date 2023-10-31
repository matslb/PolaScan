using Microsoft.ApplicationInsights;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PolaScan.App.Models;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;

namespace PolaScan.App.Services;

public class PolaScanApiService
{
    private readonly HttpClient client;
    private readonly TelemetryClient telemetryClient;
    public PolaScanApiService(IConfiguration configuration, ILogger<PolaScanApiService> logger, TelemetryClient telemetryClient)
    {
        var baseUrl = configuration.GetValue<string>("PolaScanApi:release");
#if DEBUG
        baseUrl = configuration.GetValue<string>("PolaScanApi:debug");
#endif
        client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        client.DefaultRequestHeaders.Add("PolaScanToken", configuration.GetValue<string>("PolaScanApi:token"));
        client.Timeout = TimeSpan.FromSeconds(20);
        this.telemetryClient = telemetryClient;
        telemetryClient.TrackEvent("Application startup");
    }

    public async Task<List<BoundingBox>> DetectPolaroidsInImage(string scanPath)
    {
        var mod = Constants.ImageProcessing.TempImageModifier;
        using var image = Image.Load(scanPath);
        image.Mutate(x =>
        x.Pad(image.Width + Constants.ImageProcessing.ScanFilePadding, image.Height + Constants.ImageProcessing.ScanFilePadding)
        .Resize(new Size { Width = image.Width / mod })
        );

        var tempFileName = await Helpers.SaveTempImage(image, $"{Guid.NewGuid()}.jpg");
        image.Dispose();

        var content = GetImageStreamContent(tempFileName);
        var result = await client.PostAsync("/DetectPolaroidsInImage", content);
        content.Dispose();

        return JsonConvert.DeserializeObject<List<BoundingBox>>(await result.Content.ReadAsStringAsync());
    }

    public async Task<DateOnly?> DetectDateInImage(string tempImagePath)
    {
        var content = GetImageStreamContent(tempImagePath);
        var res = string.Empty;
        try
        {
            var result = await client.PostAsync("/DetectDateInImage", content);
            res = JsonConvert.DeserializeObject<string>(await result.Content.ReadAsStringAsync());
        }
        catch
        {
        }
        content.Dispose();

        var culturename = Preferences.Default.Get(Constants.Settings.DateFormat, CultureInfo.CurrentCulture.Name);
        var culture = CultureInfo.GetCultureInfo(culturename);

        if (res != null && DateTime.TryParse(res.Replace("\\n", string.Empty), culture, DateTimeStyles.AssumeUniversal, out var date))
        {
            telemetryClient.TrackEvent("Date_detected");
            return DateOnly.FromDateTime(date);
        }
        return null;
    }

    private MultipartFormDataContent GetImageStreamContent(string fileName)
    {
        var multipartFormContent = new MultipartFormDataContent
        {
            { new StringContent(fileName), "FileName" },
            { new StringContent(fileName.Split(".")[1]), "Type" }
        };

        var imageContent = new StreamContent(File.OpenRead(fileName));

        multipartFormContent.Add(imageContent, name: "file", fileName);

        return multipartFormContent;
    }

    public async Task<string> GetAddressFromCoordinatesAsync(LocationMeta location)
    {

        var result = await client.GetAsync($"/location-lookup?lat={location.Latitude.ToString(CultureInfo.InvariantCulture)}&lng={location.Longitude.ToString(CultureInfo.InvariantCulture)}");
        return JsonConvert.DeserializeObject<string>(await result.Content.ReadAsStringAsync());
    }

    public async Task<string> GetStatusMessage()
    {
        try
        {
            var result = await client.GetAsync("/status");

            return (await result.Content.ReadAsStringAsync()).Trim('"') ?? null;
        }
        catch
        {
            return "Can't connect to server";
        }
    }
}
