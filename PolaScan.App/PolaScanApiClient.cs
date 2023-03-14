using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;

namespace PolaScan;

public class PolaScanApiClient
{
    private readonly string baseUrl;
    private readonly HttpClient client;
    public PolaScanApiClient()
    {
        baseUrl = "https://polascanapi.azurewebsites.net/";
        //baseUrl = "https://localhost:7231";
        client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<List<BoundingBox>> DetectPolaroidsInImage(string scanPath)
    {
        using var image = Image.Load(scanPath);
        image.Mutate(x =>
        x.Pad(image.Width + 250, image.Height + 250)
        .Resize(new Size { Width = image.Width / 4 })
        );

        var tempFileName = await Helpers.SaveTempImage(image, $"{Guid.NewGuid()}.jpg");
        image.Dispose();

        var content = GetImageStreamContent(tempFileName);
        var result = await client.PostAsync("/DetectPolaroidsInImage", content);
        content.Dispose();

        return JsonConvert.DeserializeObject<List<BoundingBox>>(await result.Content.ReadAsStringAsync());
    }
    public async Task<DateTimeOffset> DetectDateInImage(string tempImagePath, CultureInfo cultureInfo)
    {
        var content = GetImageStreamContent(tempImagePath);

        var result = await client.PostAsync("/DetectDateInImage", content);
        content.Dispose();

        var res = JsonConvert.DeserializeObject<string>(await result.Content.ReadAsStringAsync());

        if (DateTimeOffset.TryParse(res.Replace("\\n", string.Empty), cultureInfo, DateTimeStyles.AssumeUniversal, out var dateTime))
        {
            return dateTime.AddHours(12);
        }
        return DateTimeOffset.MinValue;
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

}
