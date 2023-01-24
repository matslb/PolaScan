using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PolaScan;

public class UserSettings
{
    private static string settingsFolder = "Polascan";
    private static string settingsFileName = "polaScanSettings.json";
    public string DestinationPath { get; set; } = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Pictures\\PolaScan";
    public string InitialDirectory { get; set; } = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents";
    public string GoogleTimelineFilePath { get; set; }

    private bool SettingsFileExists = false;
    private UserSettings()
    { }
    public static UserSettings GetUserSettings()
    {
        var settings = new UserSettings();
        settings.LoadSettings();
        if (!settings.SettingsFileExists)
            settings.Save();

        return settings;
    }
    private void LoadSettings()
    {
        if (File.Exists(GetLocalFilePath(settingsFileName)))
        {
            var file = File.ReadAllText(GetLocalFilePath(settingsFileName));
            var settings = JsonConvert.DeserializeObject<UserSettings>(file);
            GoogleTimelineFilePath = settings.GoogleTimelineFilePath;
            SettingsFileExists = true;
        }
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        Directory.CreateDirectory(GetLocalFilePath(""));
        File.WriteAllText(GetLocalFilePath(settingsFileName), json);
    }

    private static string GetLocalFilePath(string fileName)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, settingsFolder, fileName);
    }
}
