using Newtonsoft.Json;

namespace PolaScan.App.Models;

public class GoogleTimelineSemantic
{
    [JsonProperty("timelineObjects")]
    public List<LocationWrapper> Visits { get; set; }

    public class LocationWrapper
    {
        [JsonProperty("placeVisit")]
        public GoogleTimelineSemanticLocation PlaceVisit { get; set; }
    }
}
