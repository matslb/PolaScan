using Azure.AI.Vision.Common.Options;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Azure.AI.Vision.Common.Input;

namespace PolaScan.Api.Services;

public class CognitiveService
{
    private readonly HttpClient httpClient;
    private readonly CustomVisionPredictionClient customVisionPredictionClient;
    private readonly BlobContainerClient blobContainer;
    private readonly string customVisionIterationName;
    private readonly Guid customVisionProjectId;
    private readonly Uri CustomVisionEndpoint;
    private readonly VisionServiceOptions computerVisionOptions;

    public CognitiveService(Settings settings)
    {
        var blobServiceClient = new BlobServiceClient(settings.AzureBlobStorageConnectionString);
        blobContainer = blobServiceClient.GetBlobContainerClient(settings.AzureBlobStorageContainer);

        CustomVisionEndpoint = new Uri(settings.CustomVisionEndpoint ?? string.Empty);

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", settings.AzureCognitiveSubscriptionKey);
        httpClient.DefaultRequestHeaders.Add("Training-Key", settings.CustomVisionKey);

        customVisionPredictionClient = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(settings.CustomVisionPredictionKey))
        {
            Endpoint = settings.CustomVisionPredictionEndpoint
        };
        customVisionIterationName = settings.CustomVisionIterationName ?? string.Empty;
        customVisionProjectId = settings.CustomVisionProjectId ?? Guid.Empty;

        computerVisionOptions = new VisionServiceOptions(settings.AzureCognitiveEndpoint, new AzureKeyCredential(settings.AzureCognitiveSubscriptionKey));
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
        blobContainer.CreateIfNotExists();

        var imageBlobName = $"{Guid.NewGuid()}.{fileName.Split(".")[1]}";

        await blobContainer.UploadBlobAsync(imageBlobName, imageStream).ConfigureAwait(false);
        imageStream.Close();
        var blobUrl = $"{blobContainer.Uri}/{imageBlobName}";
      
        var analysisOptions = new ImageAnalysisOptions()
        {
            Features = ImageAnalysisFeature.Text
        };

        using var analyzer = new ImageAnalyzer(computerVisionOptions, VisionSource.FromUrl(blobUrl), analysisOptions);

        var result = analyzer.Analyze();
        var res = result?.Text?.Lines[0]?.Content ?? string.Empty;
      
        
        await blobContainer.DeleteBlobAsync(imageBlobName).ConfigureAwait(false);
        
        return res;
    }
}
