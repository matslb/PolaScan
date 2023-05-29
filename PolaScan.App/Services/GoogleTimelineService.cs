using System.Globalization;
using Newtonsoft.Json;
using PolaScan.App.Models;

namespace PolaScan.App.Services;

public class GoogleTimelineService
{
    private GoogleTimeline Timeline { get; set; } = new GoogleTimeline();

    public void Initialize(DateOnly date)
    {
        if (Timeline != null && Timeline.Locations.Any(x => date.Year == x.Date.Year && date.Month == x.Date.Month))
            return;

        try
        {
            var path = $"{Preferences.Default.Get(Constants.Settings.GoogleTimelineFolder, "")}\\{date.Year}\\{date.Year}_{date.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant()}.json";
            using (var r = new StreamReader(path))
            {
                using JsonReader reader = new JsonTextReader(r);
                var serializer = new JsonSerializer();
                var semanticTimeLine = serializer.Deserialize<GoogleTimelineSemantic>(reader);
                Timeline.Locations.AddRange(semanticTimeLine.Visits.Where(v => v.PlaceVisit != null).Select(v => new GoogleTimelineLocation
                {
                    Accuracy = v.PlaceVisit.Location.LocationConfidence,
                    Date = v.PlaceVisit.Duration.StartDateTime,
                    Latitude = v.PlaceVisit.Location.Latitude,
                    Longitude = v.PlaceVisit.Location.Longitude,
                    Name = v.PlaceVisit.Location.Name ?? v.PlaceVisit.Location.Address
                }));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public LocationMeta GetDateLocation(DateOnly date, int timeOfDay)
    {
        Initialize(date);
        var closestHour = timeOfDay;
        var possibleLocations = Timeline.Locations.Where(x => x.Date.UtcDateTime.ToShortDateString() == date.ToShortDateString());
        var location = possibleLocations.OrderBy(x => x.Date.Hour > closestHour ? x.Date.Hour - closestHour : closestHour - x.Date.Hour).FirstOrDefault();
        return location != null ? new LocationMeta
        {
            Latitude = location.Latitude / 1e7,
            Longitude = location.Longitude / 1e7,
            Name = location.Name
        } : null;
    }

}
