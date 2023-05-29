using Newtonsoft.Json;

namespace PolaScan.App.Models;

public class GoogleTimelineLocation
{
    [JsonProperty("latitudeE7")]
    public int Latitude { get; set; }

    [JsonProperty("longitudeE7")]
    public int Longitude { get; set; }
    public float Accuracy { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Date { get; set; }

    public string Name { get; set; }
}
