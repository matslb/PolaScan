using System.Windows;

namespace PolaScan
{
    /// <summary>
    /// Interaction logic for UserSettingsWindow.xaml
    /// </summary>
    public partial class ApplicationSettingsWindow : Window
    {
        private UserSettings userSettings;

        public ApplicationSettingsWindow()
        {
            InitializeComponent();
            this.userSettings = UserSettings.GetUserSettings();
            SetLabels();
        }
        private void SetLabels()
        {
            GoogleTimeLinePath.Text = userSettings.GoogleTimelineFilePath ?? "Download here: takeout.google.com";
            DestinationPath.Text = userSettings.DestinationPath ?? string.Empty;
            Helpers.ProcessUITasks();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            userSettings.DestinationPath = DestinationPath.Text;
            userSettings.Save();
            SetLabels();
            Close();
        }

        private void SelectTimelinePath_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = userSettings.InitialDirectory
            };
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.Filter = "Json (.json)|*.json"; // Filter files by extension

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                userSettings.GoogleTimelineFilePath = dialog.FileName;
                SetLabels();
            }
        }

        public static UserSettings SetSettings()
        {
            var window = new ApplicationSettingsWindow();
            window.ShowDialog();
            return window.userSettings;
        }


    }
}
