using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PolaScan
{
    /// <summary>
    /// Interaction logic for UserSettingsWindow.xaml
    /// </summary>
    public partial class ApplicationSettingsWindow : Window
    {
        private readonly UserSettings userSettings;

        public ApplicationSettingsWindow()
        {
            InitializeComponent();
            this.userSettings = UserSettings.GetUserSettings();
            CultureInfo[] cinfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            CultureBox.SelectedValuePath = "Value";
            CultureBox.DisplayMemberPath = "Label";
            foreach (var item in cinfos.Select(c => new SelectBoxItem(Regex.Replace(c.DateTimeFormat.ShortDatePattern.ToUpper(), @"[^dDmMyY]", " ").Trim().Replace(" ", "/"), c)).DistinctBy(x => x.Label))
            {
                CultureBox.Items.Add(item);
            }
            for (int i = 0; i < 24; i++)
            {
                TimeOfDay.Items.Add(i);
            }
            SetLabels();
        }
        private void SetLabels()
        {
            GoogleTimeLinePath.Text = userSettings.GoogleTimelineFilePath ?? "Download here: takeout.google.com";
            DestinationPath.Text = userSettings.DestinationPath;
            CultureBox.SelectedValue = userSettings.Culture;
            TimeOfDay.SelectedIndex = userSettings.TimeOfDay;
            Copyright.Text = userSettings.Copyright;
            CameraModel.Text = userSettings.CameraModel;
            Helpers.ProcessUITasks();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            DestinationPath.Text = userSettings.DestinationPath;
            userSettings.Copyright = Copyright.Text;
            userSettings.CameraModel = CameraModel.Text;
            userSettings.TimeOfDay = int.Parse(TimeOfDay.Text);
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

        private void CultureBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            userSettings.CultureName = (CultureBox.SelectedItem as SelectBoxItem).Value.Name;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (int.TryParse($"{TimeOfDay.Text}{e.Text}", out int val))
                e.Handled = !regex.IsMatch(e.Text) || val >= 0 || val <= 23;
            e.Handled = true;
        }

        public record SelectBoxItem(string Label, CultureInfo Value);
    }
}
