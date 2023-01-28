using PolaScan.Models;
using System.Diagnostics;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace PolaScan;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private PolaScanApiClient polaScanClient;
    private ImageHandler imageHandler;
    private GoogleTimelineService timelineService { get; set; }
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
        userSettings = updatedSettings;

        CompleteButton.Visibility = Visibility.Hidden;
        Helpers.DeleteTemporaryFiles();
    }

    private async void ExecuteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Mode.SelectedIndex != 2)
            LoadGoogleTimeLineData();

        // Configure open file dialog box
        var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;...";
        dialog.Multiselect = true;
        dialog.InitialDirectory = userSettings.InitialDirectory;
        // Show open file dialog box
        bool? result = dialog.ShowDialog();
        if (result == true)
        {
            SetToScanningStatus(dialog.FileNames.Length);

            var polaroidsInScan = new List<string>();

            foreach (var fileName in dialog.FileNames)
            {
                var locations = await polaScanClient.DetectPolaroidsInImage(fileName);
                polaroidsInScan.AddRange(await imageHandler.GetPolaroidsFromScan(fileName, locations));

                SetStatusText($"{polaroidsInScan.Count} photos detected");
            }

            Progress.Maximum = polaroidsInScan.Count;

            SetTitleText("Cropping and adding metadata");
            foreach (var polaroidFileName in polaroidsInScan)
            {
                Progress.Value += 1;
                SetStatusText($"Processing {Progress.Value} of {Progress.Maximum} photos");

                if (polaroidFileName == null) continue;

                var polaroidWithMeta = new PolaroidWithMeta
                {
                    OriginalPath = polaroidFileName
                };
                if (Mode.SelectedIndex != 2)
                {
                    var polaroidLipSectionName = await imageHandler.SavePolaroidLipSection(polaroidFileName);
                    if (Mode.SelectedIndex == 0)
                        polaroidWithMeta.Date = await polaScanClient.DetectDateInImage(polaroidLipSectionName, userSettings.Culture);
                    if (polaroidWithMeta.Date == DateTimeOffset.MinValue || Mode.SelectedIndex == 1)
                        polaroidWithMeta = SetDate.GetDateManually(polaroidWithMeta);
                }

                if (polaroidWithMeta != null)
                {
                    polaroidWithMeta.Location = timelineService.GetDateLocation(polaroidWithMeta.Date, userSettings.TimeOfDay);
                    await imageHandler.MoveToDestination(userSettings, polaroidWithMeta);
                }
            }
            Helpers.DeleteTemporaryFiles();

            SetToCompleteStatus(polaroidsInScan.Count);
        }
    }

    private void SetStatusText(string text)
    {
        StatusText.Text = text;
        Helpers.ProcessUITasks();
    }

    private void LoadGoogleTimeLineData()
    {
        var updatedSettings = UserSettings.GetUserSettings();
        if (timelineService == null || (userSettings.GoogleTimelineFilePath != null && userSettings.GoogleTimelineFilePath != updatedSettings.GoogleTimelineFilePath))
        {
            SetTitleText("Loading Google Timeline data");
            this.timelineService = new GoogleTimelineService(updatedSettings);
            Title.Text = "";
        }
        this.userSettings = updatedSettings;
    }

    private void SetTitleText(string text)
    {
        Title.Text = text;
        Helpers.ProcessUITasks();
    }

    private void SetToCompleteStatus(int photos)
    {
        SetTitleText("Completed");
        SetStatusText($"{photos} photos processed");
        CompleteButton.Visibility = Visibility.Visible;
        ExecuteBtn.Visibility = Visibility.Visible;
        Mode.Visibility = Visibility.Visible;
        Mode.SelectedIndex = 0;
        Helpers.ProcessUITasks();
    }

    private void SetToScanningStatus(int scans)
    {
        if (scans > 1)
            SetTitleText($"Analyzing {scans} scans");
        else
            SetTitleText("Analyzing");

        SetStatusText(string.Empty);
        Progress.Visibility = Visibility.Visible;
        ExecuteBtn.Visibility = Visibility.Hidden;
        Mode.Visibility = Visibility.Hidden;
        CompleteButton.Visibility = Visibility.Hidden;
        Progress.Value = 0;
        Helpers.ProcessUITasks();
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