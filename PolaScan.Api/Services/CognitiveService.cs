using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;

namespace PolaScan.Api.Services;

public class CognitiveService
{
    private readonly HttpClient httpClient;
    private readonly CustomVisionPredictionClient customVisionPredictionClient;
    private readonly BlobContainerClient blobContainer;
    private readonly string customVisionIterationName;
    private readonly Guid customVisionProjectId;
    private readonly Uri CustomVisionEndpoint;
    private readonly ImageAnalysisClient imageAnalysisClient;
    private readonly string storageAccountToken;

    public CognitiveService(Settings settings)
    {
        var blobServiceClient = new BlobServiceClient(settings.AzureBlobStorageConnectionString);
        blobContainer = blobServiceClient.GetBlobContainerClient(settings.AzureBlobStorageContainer);

        CustomVisionEndpoint = new Uri(settings.CustomVisionEndpoint ?? string.Empty);

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Training-Key", settings.CustomVisionKey);

        customVisionPredictionClient = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(settings.CustomVisionPredictionKey))
        {
            Endpoint = settings.CustomVisionPredictionEndpoint
        };

        customVisionIterationName = settings.CustomVisionIterationName ?? string.Empty;
        customVisionProjectId = settings.CustomVisionProjectId ?? Guid.Empty;
        storageAccountToken = settings.StorageAccountToken;

        imageAnalysisClient = new ImageAnalysisClient(new Uri(settings.VisionEndpoint), new AzureKeyCredential(settings.VisionKey));

    }

    public async Task<List<BoundingBox>> DetectPolaroidsInImage(Stream imageStream)
    {
        var result = await customVisionPredictionClient.DetectImageAsync(customVisionProjectId, customVisionIterationName, imageStream);
        imageStream.Dispose();

        await httpClient.DeleteAsync($"{CustomVisionEndpoint}customvision/v3.3/training/projects/{customVisionProjectId}/predictions?ids={result.Id}");

        return result.Predictions.Where(p => p.TagName == "Polaroid" && p.Probability > 0.999).Select(p => p.BoundingBox).ToList();
    }

    public async Task<string> DetectDateInImage(Stream imageStream, string fileName)
    {
        blobContainer.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

        var imageBlobName = $"{Guid.NewGuid()}.{fileName.Split(".")[1]}";

        await blobContainer.UploadBlobAsync(imageBlobName, imageStream);
        imageStream.Close();

        var blobUrl = new Uri($"{blobContainer.Uri}/{imageBlobName}");
        try
        {
            var result = imageAnalysisClient.Analyze(blobUrl, VisualFeatures.Read, new ImageAnalysisOptions { Language = "en" });

            return result?.Value?.Read?.ToString() ?? string.Empty;
        }
        finally
        {
            await blobContainer.DeleteBlobAsync(imageBlobName);
        }

    }

    public class ImageRequest
    {
        public string Url { get; set; }
    }

    public class CognitiveResult
    {
        public ReadResult ReadResult { get; set; }
    }

    public class ReadResult
    {
        public string Content { get; set; }
    }

}
