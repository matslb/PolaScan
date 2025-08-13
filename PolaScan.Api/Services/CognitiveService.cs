using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;

namespace PolaScan.Api.Services;

public class CognitiveService
{
    private readonly HttpClient httpClient;
    private readonly CustomVisionPredictionClient customVisionPredictionClient;
    private readonly string customVisionIterationName;
    private readonly Guid customVisionProjectId;
    private readonly Uri CustomVisionEndpoint;
    private readonly ImageAnalysisClient imageAnalysisClient;

    public CognitiveService(Settings settings)
    {
        CustomVisionEndpoint = new Uri(settings.CustomVisionEndpoint ?? string.Empty);

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Training-Key", settings.CustomVisionKey);

        customVisionPredictionClient = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(settings.CustomVisionPredictionKey))
        {
            Endpoint = settings.CustomVisionPredictionEndpoint
        };

        customVisionIterationName = settings.CustomVisionIterationName ?? string.Empty;
        customVisionProjectId = settings.CustomVisionProjectId ?? Guid.Empty;

        imageAnalysisClient = new ImageAnalysisClient(new Uri(settings.VisionEndpoint), new AzureKeyCredential(settings.VisionKey));

    }

    public async Task<List<BoundingBox>> DetectPolaroidsInImage(Stream imageStream)
    {
        var result = await customVisionPredictionClient.DetectImageAsync(customVisionProjectId, customVisionIterationName, imageStream);
        imageStream.Dispose();

        await httpClient.DeleteAsync($"{CustomVisionEndpoint}customvision/v3.3/training/projects/{customVisionProjectId}/predictions?ids={result.Id}");

        return result.Predictions.Where(p => p.TagName == "Polaroid" && p.Probability > 0.999).Select(p => p.BoundingBox).ToList();
    }

    public async Task<string> DetectDateInImage(Stream imageStream, CancellationToken ct = default)
    {
        var result = await imageAnalysisClient.AnalyzeAsync(
            BinaryData.FromStream(imageStream),
            VisualFeatures.Read,
            cancellationToken: ct);

        var read = result.Value.Read;
        if (read?.Blocks is null || read.Blocks.Count == 0) return string.Empty;

        var lines = new List<string>(capacity: 64);
        foreach (var block in read.Blocks)
            foreach (var line in block.Lines)
                lines.Add(line.Text);

        return string.Join(Environment.NewLine, lines);
    }

    public class ImageRequest
    {
        public string Url { get; set; }
    }

    public class CognitiveResult
    {
        public ReadResult ReadResult { get; set; }
    }

}
