using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using PolaScan.App.Models;

namespace PolaScan.App.Services;

public class GoogleTimelineService
{
    private readonly TelemetryClient telemetryClient;
    private List<LocationRecord> LocationRecords { get; set; } = new List<LocationRecord>();
    private string loadedFilename = string.Empty;
    public GoogleTimelineService(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }

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
                var unfilteredTimeLine = serializer.Deserialize<GoogleTimelineV2>(reader);
                LocationRecords.AddRange(unfilteredTimeLine.RawRecords.Where(r => r.Record != null).Select(r => r.Record));
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
        var possibleLocations = LocationRecords
            .Where(x => x.TimeStamp.UtcDateTime.ToShortDateString() == date.ToShortDateString())
            .OrderBy(x => Math.Abs(x.TimeStamp.Hour - closestHour))
            .DistinctBy(x => $"{x.Latitude.ToString().Substring(0, 6)}|{x.Latitude.ToString().Substring(0, 6)}")
            .Take(10)
            .Select(location => new LocationMeta
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                DateTime = location.TimeStamp.DateTime
            }).ToList();

        telemetryClient.TrackEvent("Location_file_lookup", new Dictionary<string, string>
                {
                    {"Total_location", LocationRecords.Count().ToString()},
                    {"Possible_locations", possibleLocations.Count().ToString()}
                });
        return possibleLocations;
    }
}
