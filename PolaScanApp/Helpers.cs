using SixLabors.ImageSharp;
using System.IO;
using System.Windows.Threading;

namespace PolaScan;

public static class Helpers
{
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

    public static string GetAppDataFilePath(string relativePath)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "PolaScan", relativePath);
    }

    public static async Task<string> SaveTempImage(Image image, string tempFileName)
    {
        var filename = GetAppDataFilePath($"temp\\{tempFileName}");
        await image.SaveAsync(filename);
        return filename;
    }
    
    public static void DeleteTemporaryFiles()
    {
        var tempFiles = Directory.GetFiles(GetAppDataFilePath("temp"));
        foreach (var fileName in tempFiles)
        {
            File.Delete(fileName);
        }
    }
}
