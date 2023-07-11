using System.IO;
using System.Text;
using Azure;
using Azure.AI.Vision.Common.Input;
using Azure.AI.Vision.Common.Options;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Newtonsoft.Json;

namespace PolaScan.Api.Services;

public class StatusService
{
    private readonly BlobClient blobClient;

    public StatusService(Settings settings)
    {
        blobClient = new BlobClient(settings.AzureBlobStorageConnectionString, settings.StatusMessageContainer, settings.StatusMessageBlob);
    }

    public async Task<string> GetStatusMessage()
    {
        try
        {
            var result = await blobClient.DownloadStreamingAsync();
            var reader = new StreamReader(result.Value.Content);
            return reader.ReadToEnd().Trim('"');
        }
        catch
        {
            return string.Empty;
        }
    }
}
