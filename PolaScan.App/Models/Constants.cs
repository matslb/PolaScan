namespace PolaScan.App.Models;

public static class Constants
{
    public static class Settings
    {
        public static string AssumedHour = nameof(AssumedHour);
        public static string DesitnationPath = nameof(DesitnationPath);
        public static string GoogleTimelineFile = nameof(GoogleTimelineFile);
        public static string CopyRightText = nameof(CopyRightText);
        public static string CameraModel = nameof(CameraModel);
        public static string DateFormat = nameof(DateFormat);
        public static string CultureName = nameof(CultureName);
        public static string ImageFormat = nameof(ImageFormat);
    }

    public static class ImageProcessing
    {
        public static int ScanFilePadding = 500;
        public static int TempImageModifier = 3;

        public static Dictionary<string, double> PhotoFormatRatios = new()
        {
            { PhotoFormat.Polaroid, 1.222},
            { PhotoFormat.PolaroidGo, 1},
            { PhotoFormat.InstaxMini, 1.58},
            { PhotoFormat.InstaxSquare, 1.2},
            { PhotoFormat.InstaxWide, 0.79 }
        };

        public static Dictionary<string, string> PhotoFormatLabels = new()
        {
            { PhotoFormat.Polaroid, "Polaroid 600 / SX-70 / I-Type"},
            { PhotoFormat.PolaroidGo, "Polaroid Go"},
            { PhotoFormat.InstaxMini, "Instax Mini"},
            { PhotoFormat.InstaxSquare, "Instax Square"},
            { PhotoFormat.InstaxWide, "Instax Wide" }
        };
    }

    public class PhotoFormat
    {
        public static string Polaroid = nameof(Polaroid);
        public static string PolaroidGo = nameof(PolaroidGo);
        public static string InstaxMini = nameof(InstaxMini);
        public static string InstaxSquare = nameof(InstaxSquare);
        public static string InstaxWide = nameof(InstaxWide);
    }
}
