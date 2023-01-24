using PolaScan.Models;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PolaScan
{
    /// <summary>
    /// Interaction logic for SetDate.xaml
    /// </summary>
    public partial class SetDate : Window
    {
        private PolaroidWithMeta polaroid;
        public SetDate(PolaroidWithMeta polaroid)
        {
            InitializeComponent();
            this.polaroid = polaroid;

            SetImage(polaroid.OriginalPath);
        }

        private void SetImage(string originalPath)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(Path.GetFullPath(originalPath), UriKind.Absolute);
            image.EndInit();
            Polaroid.Source = image;
            ProcessUITasks();
        }

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            var date = Date.SelectedDate;
            date.Value.AddHours(10);
            polaroid.Date = date.Value;
            this.Close();
        }
        private void Disgard_Click(object sender, RoutedEventArgs e)
        {
            polaroid = null;
            this.Close();
        }
        public static PolaroidWithMeta? GetDateManually(PolaroidWithMeta polaroid)
        {
            var setDateWindow = new SetDate(polaroid);
            setDateWindow.ShowDialog();

            return setDateWindow.polaroid;
        }
        public static void ProcessUITasks()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        private void Date_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ExecuteBtn.Visibility = Visibility.Visible;
            ProcessUITasks();
        }
    }
}
