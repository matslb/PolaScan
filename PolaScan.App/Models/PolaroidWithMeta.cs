using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using SixLabors.ImageSharp;

namespace PolaScan.App.Models;

public class PolaroidWithMeta
{
    public string FileName(int i) => $"{(Date == null ? "img" : Date.Value.Date.ToString("dd-MM-yyyy"))}-{i}.{Format}";
    public string AbsolutePath { get; set; }
    public string TempFileName { get; set; }
    public string ScanFile { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string PreviewData { get; set; }
    public float Rotation { get; set; }
    public Rectangle Crop { get; set; }
    public BoundingBox LocationInScan { get; set; }
    public LocationMeta Location { get; set; }
    public string Format { get; set; }
    public bool ToBeSaved { get; set; }
}

public class LocationMeta
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; }    
}