using System.Globalization;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using SixLabors.ImageSharp;

namespace PolaScan.App.Models;

public class PolaroidWithMeta
{
    public Guid Id { get; set; }
    public string AbsolutePath { get; set; }
    public string ScanFile { get; set; }
    public DateOnly? Date { get; set; }
    public int Hour { get; set; }
    public string PreviewData { get; set; }
    public float Rotation { get; set; }
    public Rectangle Crop { get; set; }
    public BoundingBox LocationInScan { get; set; }
    public LocationMeta Location { get; set; }
    public bool HasBeenAnalyzed { get; set; }
    public string FileName(int i) => $"{(Date == null ? "img" : Date.Value.ToString("dd-MM-yyyy"))}-{i}.{Format}";
    public string TempFileName => $"{Id}.{Format}";
    public string Format => ScanFile.Split(".")[1];
}

public class LocationMeta
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; }
}
