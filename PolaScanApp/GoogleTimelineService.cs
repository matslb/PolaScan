using Newtonsoft.Json;
using PolaScan.Models;
using System.IO;

namespace PolaScan;

public class GoogleTimelineService
{
    private GoogleTimeline Timeline { get; set; }

    public GoogleTimelineService(UserSettings userSettings)
    {
        try
        {
            using (StreamReader r = new StreamReader(userSettings.GoogleTimelineFilePath))
            {
                using JsonReader reader = new JsonTextReader(r);
                JsonSerializer serializer = new JsonSerializer();
                Timeline = serializer.Deserialize<GoogleTimeline>(reader);
            }
        }
        catch (Exception e)
        {
        }
        if (Timeline == null || Timeline.Locations == null)
            Timeline = new() { Locations = new() };
    }

    public LocationMeta? GetDateLocation(DateTimeOffset date, int timeOfDay)
    {
        var closestHour = timeOfDay;
        var possibleLocations = Timeline.Locations.Where(x => x.Date.UtcDateTime.ToShortDateString() == date.Date.ToShortDateString());
        var location = possibleLocations.OrderBy(x => x.Date.Hour > closestHour ? x.Date.Hour - closestHour : closestHour - x.Date.Hour).FirstOrDefault();
        return location != null ? new LocationMeta { Latitude = location.Latitude / 1e7, Longitude = location.Longitude / 1e7 } : null;
    }

}
