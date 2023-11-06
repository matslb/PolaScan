using Azure.Storage.Blobs;

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
