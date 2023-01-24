using PolaScan;
using PolaScan.Models;
using System.Diagnostics;
using System.Windows;

namespace PolaScan;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private PolaScanApiClient polaScanClient;
    private ImageHandler imageHandler;
    private GoogleTimelineService timelineService;
    private UserSettings userSettings;

    public MainWindow()
    {
        InitializeComponent();
        InitializeSettings();
        Progress.Visibility = Visibility.Hidden;
    }

    public void InitializeSettings(UserSettings settings = null)
    {
        var updatedSettings = settings ?? UserSettings.GetUserSettings();
        this.imageHandler = new ImageHandler();
        this.polaScanClient = new PolaScanApiClient();
        if (userSettings == null || (userSettings.GoogleTimelineFilePath != null && userSettings.GoogleTimelineFilePath != updatedSettings.GoogleTimelineFilePath))
        {
            Title.Text = $"Loading Google Timeline data";
            Helpers.ProcessUITasks();
            this.timelineService = new GoogleTimelineService(updatedSettings);
            Title.Text = "";
        }
        userSettings = updatedSettings;
    }

    private async void ExecuteBtn_Click(object sender, RoutedEventArgs e)
    {
        // Configure open file dialog box
        var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.DefaultExt = ".jpg"; // Default file extension
        dialog.Filter = "Images (.jpg)|*.jpg"; // Filter files by extension
        dialog.Multiselect = true;
        dialog.InitialDirectory = userSettings.InitialDirectory;
        // Show open file dialog box
        bool? result = dialog.ShowDialog();
        if (result == true)
        {
            Progress.Visibility = Visibility.Visible;
            ExecuteBtn.Visibility = Visibility.Hidden;
            Mode.Visibility = Visibility.Hidden;
            CompleteButton.Visibility = Visibility.Hidden;
            Progress.Value = 0;
            Title.Text = $"Slicing scans";
            Helpers.ProcessUITasks();
            foreach (var fileName in dialog.FileNames)
            {
                var locations = await polaScanClient.DetectPolaroidsInImage(fileName);
                Progress.Maximum = locations.Count;

                Helpers.ProcessUITasks();

                var polaroidsInScan = await imageHandler.GetPolaroidsFromScan(fileName, locations);

                foreach (var polaroidFileName in polaroidsInScan)
                {
                    Progress.Value += 1;
                    Title.Text = $"Processing {Progress.Value} of {Progress.Maximum} photos";
                    Helpers.ProcessUITasks();

                    if (polaroidFileName == null) continue;

                    var polaroidLipSectionName = await imageHandler.SavePolaroidLipSection(polaroidFileName);

                    var polaroidWithMeta = new PolaroidWithMeta
                    {
                        OriginalPath = polaroidFileName
                    };
                    if (Mode.SelectedIndex == 0)
                        polaroidWithMeta.Date = await polaScanClient.DetectDateInImage(polaroidLipSectionName, userSettings.DateFormat);
                    if (polaroidWithMeta.Date == DateTimeOffset.MinValue || Mode.SelectedIndex == 1)
                        polaroidWithMeta = SetDate.GetDateManually(polaroidWithMeta);

                    if (polaroidWithMeta != null)
                    {
                        polaroidWithMeta.Location = timelineService.GetDateLocation(polaroidWithMeta.Date);
                        await imageHandler.MoveToDestination(userSettings.DestinationPath, polaroidWithMeta);
                    }
                }
                imageHandler.DeleteTemporaryFiles();
            }
            Title.Text = "Completed!";
            CompleteButton.Visibility = Visibility.Visible;
            ExecuteBtn.Visibility = Visibility.Visible;
            Mode.Visibility = Visibility.Visible;
            Mode.SelectedIndex = 0;
            Helpers.ProcessUITasks();
        }
    }

    private void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("explorer.exe", userSettings.DestinationPath);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var settings = ApplicationSettingsWindow.SetSettings();
        InitializeSettings(settings);
    }
    private void HowTo_Click(object sender, RoutedEventArgs e)
    {
        var howTo = new HowTo();
        howTo.Show();
    }
}