﻿using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Newtonsoft.Json;
using PolaScan.App.Models;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;

namespace PolaScan.App.Services;

public class PolaScanApiService
{
    private readonly string baseUrl;
    private readonly HttpClient client;
    public PolaScanApiService()
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
        .Resize(new Size { Width = image.Width / 8 })
        );

        var tempFileName = await Helpers.SaveTempImage(image, $"{Guid.NewGuid()}.jpg");
        image.Dispose();

        var content = GetImageStreamContent(tempFileName);
        var result = await client.PostAsync("/DetectPolaroidsInImage", content);
        content.Dispose();

        return JsonConvert.DeserializeObject<List<BoundingBox>>(await result.Content.ReadAsStringAsync());
    }

    public async Task<DateOnly?> DetectDateInImage(string tempImagePath, CultureInfo cultureInfo)
    {
        var content = GetImageStreamContent(tempImagePath);

        var result = await client.PostAsync("/DetectDateInImage", content).ConfigureAwait(false);
        content.Dispose();

        var res = JsonConvert.DeserializeObject<string>(await result.Content.ReadAsStringAsync());

        if (DateTime.TryParse(res.Replace("\\n", string.Empty), cultureInfo, DateTimeStyles.AssumeUniversal, out var date))
        {
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
}