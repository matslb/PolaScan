using System.Globalization;
using Newtonsoft.Json;
using PolaScan.App.Models;

namespace PolaScan.App.Services;

public class GoogleTimelineService
{
    private GoogleTimeline Timeline { get; set; } = new GoogleTimeline();
    private bool detailedLoaded = false;
    public void InitializeSemantic(DateOnly date)
    {
        if (Timeline.Locations.Any(x => date.Year == x.Date.Year && date.Month == x.Date.Month && x.Name != null))
            return;
        try
        {
            var path = $"{Preferences.Default.Get(Constants.Settings.GoogleTimelineFolder, "")}\\Semantic Location History\\{date.Year}\\{date.Year}_{date.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant()}.json";
            using (var r = new StreamReader(path))
            {
                using JsonReader reader = new JsonTextReader(r);
                var serializer = new JsonSerializer();
                var semanticTimeLine = serializer.Deserialize<GoogleTimelineSemantic>(reader);
                Timeline.Locations.AddRange(semanticTimeLine.Visits.Where(v => v.PlaceVisit != null).Select(v => new GoogleTimelineLocation
                {
                    Accuracy = 80 - v.PlaceVisit.Location.LocationConfidence,
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

    public void InitializeDetailed()
    {
        if (detailedLoaded)
            return;
        detailedLoaded = true;
        try
        {
            using (var r = new StreamReader($"{Preferences.Default.Get(Constants.Settings.GoogleTimelineFolder, "")}\\Records.json"))
            {
                using JsonReader reader = new JsonTextReader(r);
                var serializer = new JsonSerializer();
                var detailedTimeLine = serializer.Deserialize<GoogleTimeline>(reader);
                Timeline.Locations.AddRange(detailedTimeLine.Locations);
            }
        }
        catch (Exception e)
        {
        }
    }

    public List<LocationMeta> GetDateLocations(DateOnly date, int timeOfDay)
    {
        InitializeDetailed();
        var closestHour = timeOfDay;
        var possibleLocations = Timeline.Locations.Where(x => x.Date.UtcDateTime.ToShortDateString() == date.ToShortDateString());
        return possibleLocations
            .OrderBy(x => Math.Abs(x.Date.Hour - closestHour))
            .DistinctBy(x => $"{x.Latitude.ToString().Substring(0, 6)}|{x.Latitude.ToString().Substring(0, 6)}")
            .Take(10)
            .Select(location => new LocationMeta
            {
                Latitude = location.Latitude / 1e7,
                Longitude = location.Longitude / 1e7,
                Name = location.Name,
                DateTime = location.Date.DateTime
            }).ToList();
    }
}
