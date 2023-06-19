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
        public static string HideAlert = nameof(HideAlert);
    }
    public static class ImageProcessing
    {
        public static int ScanFilePadding = 500;
        public static int TempImageModifier = 3;

    }
    public static class FilmFormats
    {
        public static string Polaroid = nameof(Polaroid);
        public static string PolaroidGo = nameof(PolaroidGo);
        public static string InstaxMini = nameof(InstaxMini);
        public static string InstaxSquare = nameof(InstaxSquare);
        public static string InstaxWide = nameof(InstaxWide);

        public static Dictionary<string, double> Ratios = new()
        {
            { Polaroid, 1.222},
            { PolaroidGo, 1.251},
            { InstaxMini, 1.59},
            { InstaxSquare, 1.2},
            { InstaxWide, 0.79 }
        };

        public static Dictionary<string, string> Labels = new()
        {
            { Polaroid, "Polaroid I-Type"},
            { PolaroidGo, "Polaroid Go"},
            { InstaxMini, "Instax Mini"},
            { InstaxSquare, "Instax Square"},
            { InstaxWide, "Instax Wide" }
        };
    }
}
