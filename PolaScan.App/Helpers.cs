using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;

namespace PolaScan;

public static class Helpers
{
    private static string GetAppDataFilePath(string relativePath)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "PolaScan", relativePath);
    }
    public static string GetTempFilePath(string relativePath) => GetAppDataFilePath($"temp\\{relativePath}");

    public static async Task<string> SaveTempImage(Image image, string tempFileName)
    {
        var filename = GetAppDataFilePath(GetTempFilePath(tempFileName));
        await image.SaveAsync(filename);
        return filename;
    }

    public static void DeleteTemporaryFiles()
    {
        Directory.CreateDirectory(GetTempFilePath(string.Empty));
        var tempFiles = Directory.GetFiles(GetAppDataFilePath("temp"));
        foreach (var fileName in tempFiles)
        {
            File.Delete(fileName);
        }
    }
}
