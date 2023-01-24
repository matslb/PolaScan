using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Newtonsoft.Json;
using PolaScan.Api;
using PolaScan.Api.Models;
using System.Globalization;
using System.Text;

namespace PolaScan;

public class CognitiveService
{
    private readonly HttpClient computerVisionClient;
    private readonly CustomVisionPredictionClient customVisionPredictionClient;
    private readonly BlobContainerClient blobContainer;
    private readonly string customVisionIterationName;
    private readonly Guid customVisionProjectId;

    public CognitiveService(Settings settings)
    {
        computerVisionClient = new HttpClient
        {
            BaseAddress = new Uri(settings.AzureCognitiveEndpoint ?? string.Empty),
        };
        var blobServiceClient = new BlobServiceClient(settings.AzureBlobStorageConnectionString);
        blobContainer = blobServiceClient.GetBlobContainerClient(settings.AzureBlobStorageContainer);
        computerVisionClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", settings.AzureCognitiveSubscriptionKey);

        customVisionPredictionClient = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(settings.CustomVisionPredictionKey))
        {
            Endpoint = settings.CustomVisionPredictionEndpoint
        };
        customVisionIterationName = settings.CustomVisionIterationName ?? string.Empty;
        customVisionProjectId = settings.CustomVisionProjectId ?? Guid.Empty;
    }

    public async Task<List<BoundingBox>> DetectPolaroidsInImage(Stream imageStream)
    {
        // using var imageStream = new MemoryStream(imageBytes);
        var result = await customVisionPredictionClient.DetectImageAsync(customVisionProjectId, customVisionIterationName, imageStream);
        imageStream.Dispose();

        return result.Predictions.Where(p => p.TagName == "Polaroid" && p.Probability > 0.999).Select(p => p.BoundingBox).ToList();
    }
    public async Task<string> DetectDateInImage(Stream imageStream, string fileName)
    {
        blobContainer.CreateIfNotExists();

        var imageBlobName = $"{Guid.NewGuid()}.{fileName.Split(".")[1]}";

        await blobContainer.UploadBlobAsync(imageBlobName, imageStream);

        var blobUrl = $"{blobContainer.Uri}/{imageBlobName}";

        var res = await computerVisionClient.PostAsync(
            "computervision/imageanalysis:analyze?api-version=2022-10-12-preview&features=read&model-version=latest&language=en",

            new StringContent(JsonConvert.SerializeObject(new ImageRequest { Url = blobUrl }), Encoding.UTF8, "application/json"));
        var cognitiveResult = JsonConvert.DeserializeObject<CognitiveResult>(await res.Content.ReadAsStringAsync());

        await blobContainer.DeleteBlobAsync(imageBlobName);
        imageStream.Close();

        return cognitiveResult?.ReadResult?.Content ?? string.Empty;
    }

    class ImageRequest
    {
        public string Url { get; set; }
    }
}
