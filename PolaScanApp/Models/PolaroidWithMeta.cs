using Microsoft.VisualBasic;

namespace PolaScan.Models;

public class PolaroidWithMeta
{
    public string FileName(int i) => $"{(Date == DateTimeOffset.MinValue ? "img" : Date.Date.ToString("dd-MM-yyyy"))}-{i}.{Format}";
    public string OriginalPath { get; set; }
    public DateTimeOffset Date { get; set; }
    public LocationMeta Location { get; set; }
    public string Format => OriginalPath.Split('.')[1];
}

public class LocationMeta
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}