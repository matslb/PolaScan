using Newtonsoft.Json;
using PolaScan.App.Models;

namespace PolaScan.App.Services;

public class GoogleTimelineService
{
    private GoogleTimeline Timeline { get; set; } = new GoogleTimeline();
    private string loadedFilename = string.Empty;

    public void InitializeDetailed()
    {
        var fileName = Preferences.Default.Get(Constants.Settings.GoogleTimelineFile, "");

        if (fileName == loadedFilename)
            return;
        loadedFilename = fileName;
        try
        {
            using (var r = new StreamReader(fileName))
            {
                using JsonReader reader = new JsonTextReader(r);
                var serializer = new JsonSerializer();
                var detailedTimeLine = serializer.Deserialize<GoogleTimeline>(reader);
                Timeline.Locations.AddRange(detailedTimeLine.Locations.Where(l => l.Accuracy < 40));
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
                DateTime = location.Date.LocalDateTime
            }).ToList();
    }
}
