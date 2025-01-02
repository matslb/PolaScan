using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PolaScan.App.Models;
public class GoogleTimelineV2
{
    [JsonProperty("rawSignals")]
    public ICollection<RawRecord> RawRecords { get; set; }
}

public class RawRecord
{
    [JsonProperty("position")]
    public LocationRecord? Record { get; set; } = null;
}

public class LocationRecord
{

    [JsonProperty("latLng")]
    private string _latLng
    {
        set
        {
            var match = Regex.Match(value, @"([\d.]+)°,\s*([\d.]+)°");
            if (match.Success)
            {
                Latitude = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                Longitude = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset TimeStamp { get; set; }

    [JsonProperty("accuracyMeters")]
    public int Accuracy { get; set; }

    [JsonProperty("SpeedMetersPerSecond")]
    public double Speed { get; set; }
}