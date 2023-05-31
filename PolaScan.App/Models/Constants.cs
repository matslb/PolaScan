namespace PolaScan.App.Models;

public static class Constants
{
    public static class Settings
    {
        public static string AssumedHour = nameof(AssumedHour);
        public static string DesitnationPath = nameof(DesitnationPath);
        public static string GoogleTimelineFolder = nameof(GoogleTimelineFolder);
        public static string CopyRightText = nameof(CopyRightText);
        public static string CameraModel = nameof(CameraModel);
        public static string DateFormat = nameof(DateFormat);
        public static string CultureName = nameof(CultureName);
    }

    public static class ImageProcessing
    {
        public static int ScanFilePadding = 250;
        public static int TempImageModifier = 5;
        public static double HeightToWidthRatio = 1.222;
    }
}
