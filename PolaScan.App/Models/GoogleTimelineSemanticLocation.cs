using Newtonsoft.Json;

namespace PolaScan.App.Models;

public class GoogleTimelineSemanticLocation
{
    [JsonProperty("location")]
    public Location Location { get; set; }

    [JsonProperty("duration")]
    public Duration Duration { get; set; }
}

public class Location
{
    [JsonProperty("latitudeE7")]
    public int Latitude { get; set; }

    [JsonProperty("longitudeE7")]
    public int Longitude { get; set; }

    public string Name { get; set; }
    public string Address { get; set; }

    public float LocationConfidence { get; set; }
}

public class Duration
{
    [JsonProperty("startTimestamp")]
    public DateTimeOffset StartDateTime { get; set; }

    [JsonProperty("endTimeStamp")]
    public DateTimeOffset EndDateTime { get; set; }
}
