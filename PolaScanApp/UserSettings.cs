using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;

namespace PolaScan;

public class UserSettings
{
    private static string settingsFileName = "polaScanSettings.json";
    public string DestinationPath { get; set; } = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Pictures\\PolaScan";
    public string InitialDirectory { get; set; } = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Documents";
    public string GoogleTimelineFilePath { get; set; }
    public string CultureName = CultureInfo.CurrentCulture.Name;
    public CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);
    public string Copyright { get; set; } = Environment.UserName;
    public string CameraModel { get; set; } = "Polaroid 600 / SX-70";

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
        if (File.Exists(Helpers.GetAppDataFilePath(settingsFileName)))
        {
            var file = File.ReadAllText(Helpers.GetAppDataFilePath(settingsFileName));
            var settings = JsonConvert.DeserializeObject<UserSettings>(file);
            GoogleTimelineFilePath = settings.GoogleTimelineFilePath;
            DestinationPath = settings.DestinationPath;
            InitialDirectory = settings.InitialDirectory;
            CultureName = settings.CultureName;
            Copyright = settings.Copyright;
            CameraModel = settings.CameraModel;
            SettingsFileExists = true;
        }
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        Directory.CreateDirectory(Helpers.GetAppDataFilePath(""));
        File.WriteAllText(Helpers.GetAppDataFilePath(settingsFileName), json);
    }


}
