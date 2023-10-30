using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using PolaScan.App.Models;

namespace PolaScan.App.Services;
public class ApplicationInitializer : ITelemetryInitializer
{
    public string UserId { get; } = Preferences.Default.Get(Constants.Settings.DeviceId, Guid.Empty.ToString());
    public string ComponentVersion { get; } = AppInfo.Current.VersionString;
    public string SessionId { get; } = Guid.NewGuid().ToString();

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.User.Id = UserId;
        telemetry.Context.Component.Version = ComponentVersion;
        telemetry.Context.Session.Id = SessionId;
    }
}
