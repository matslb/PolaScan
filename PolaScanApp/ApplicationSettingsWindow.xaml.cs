using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            DestinationPath.Text = userSettings.DestinationPath;
            Helpers.ProcessUITasks();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            DestinationPath.Text = userSettings.DestinationPath;
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

        private void SelectDestinationFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = userSettings.DestinationPath;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                userSettings.DestinationPath = dialog.FileName;
            }
            SetLabels();
        }
    }
}
