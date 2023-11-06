using Image = SixLabors.ImageSharp.Image;


namespace PolaScan.App.Services.Extensions;

public static class ImagedSharpExtensions
{
    public static int GetTestDivisor(this Image img)
    {
        return img.Width > 1500 ? (img.Width / 1500) : 1;
    }

    public static int GetTestPadding(this Image img)
    {
        return 500;
    }

    public static (int width, int height) GetTestSize(this Image img)
    {
        var divisor = img.GetTestDivisor();
        var padding = img.GetTestPadding();
        return (((img.Width + padding) / divisor), ((img.Width + padding) / divisor));
    }
}
