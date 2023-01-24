namespace PolaScan.Api;

public class Settings
{
    public string? AzureCognitiveSubscriptionKey { get; set; }
    public string? AzureCognitiveEndpoint { get; set; }
    public string? AzureBlobStorageConnectionString { get; set; }
    public string? CustomVisionPredictionKey { get; set; }
    public string? CustomVisionPredictionEndpoint { get; set; }
    public Guid? CustomVisionProjectId { get; set; }
    public string? CustomVisionIterationName { get; set; }
    public string? AzureBlobStorageContainer { get; set; }
}
