using Azure;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using PolaScan.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.IO;

namespace PolaScan;

public class ImageHandler
{
    public List<string> SavedTemporaryFiles { get; set; }
    private readonly static string tempFolder = "temp";
    private readonly static int testImageModifier = 4;
    private readonly static int padding = 250;
    public ImageHandler()
    {
        SavedTemporaryFiles = new();
        Directory.CreateDirectory(Helpers.GetAppDataFilePath(tempFolder));
    }

    public async Task MoveToDestination(string destinationRootPath, PolaroidWithMeta polaroid, UserSettings userSettings)
    {

        var destinationPath = $"{destinationRootPath}\\{polaroid.Date.Year}\\{polaroid.Date.Month}";
        Directory.CreateDirectory(destinationPath);
        var i = 0;
        var uniqueName = destinationPath + $"\\{polaroid.FileName(i)}";
        while (File.Exists(uniqueName))
        {
            uniqueName = destinationPath + $"\\{polaroid.FileName(i)}";
            i++;
        }
        using Image image = Image.Load(polaroid.OriginalPath);
        image.Metadata.ExifProfile ??= new();
        if (polaroid.Location != null)
        {
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, GPSRational(polaroid.Location.Latitude));
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitude, GPSRational(polaroid.Location.Longitude));
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, polaroid.Location.Latitude > 0 ? "N" : "S");
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitudeRef, polaroid.Location.Longitude > 0 ? "E" : "W");
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSAltitude, Rational.FromDouble(100));
        }
        image.Metadata.ExifProfile.SetValue(ExifTag.Software, nameof(PolaScan));
        image.Metadata.ExifProfile.SetValue(ExifTag.Make, "Polaroid");
        image.Metadata.ExifProfile.SetValue(ExifTag.Model, userSettings.CameraModel);
        image.Metadata.ExifProfile.SetValue(ExifTag.Copyright, userSettings.Copyright);
        image.Metadata.ExifProfile.SetValue(ExifTag.DateTimeOriginal, polaroid.Date.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture));
        await image.SaveAsync(uniqueName);
    }

    public async Task<string> SavePolaroidLipSection(string fileName)
    {
        using Image image = Image.Load(fileName);
        image.Mutate(x => x
             .Crop(new Rectangle(0, Convert.ToInt32(Math.Floor(image.Height * 0.8)), image.Width, Convert.ToInt32(Math.Floor(image.Height * 0.2))))
             .Grayscale()
             .GaussianSharpen()
             );
        var tempFileName = $"{Guid.NewGuid()}.{fileName.Split(".")[1]}";
        tempFileName = await Helpers.SaveTempImage(image, tempFileName);
        image.Dispose();
        return tempFileName;
    }

    public async Task<ICollection<string>> GetPolaroidsFromScan(string scanFile, List<BoundingBox>? locations)
    {
        var polaroidsInScan = new List<string>();

        string compressedScanFileName = await SaveCompressedTestImage(scanFile);

        foreach (var position in locations)
        {
            var degrees = GetImageRotationDegrees(compressedScanFileName, (position));
            var crop = GetImageCropRectangle(compressedScanFileName, (position), degrees);

            using Image<Rgba32> image = Image.Load<Rgba32>(scanFile);

            /// Cropping image with margin to adjust rotation
            image.Mutate(x => x
                .Pad(image.Width + padding, image.Height + padding));
            image.Mutate(x => x
                .Crop(PolaroidSizeWithMargin(image, position, 1))
                .Rotate(degrees)
                .Crop(crop)
                .BackgroundColor(Color.White)
                );

            var tempFileName = $"{Guid.NewGuid()}.{scanFile.Split(".")[1]}";

            tempFileName = await Helpers.SaveTempImage(image, tempFileName);

            polaroidsInScan.Add(tempFileName);
        }
        return polaroidsInScan;
    }

    private async Task<string> SaveCompressedTestImage(string fileName)
    {
        using var image = Image.Load<Rgba32>(fileName);
        image.Mutate(x =>
        x.Pad(image.Width + padding, image.Height + padding)
        .Resize(new Size { Width = image.Width / testImageModifier })
        .GaussianSharpen()
        .GaussianBlur()
        .GaussianSharpen()
        .DetectEdges()
        .BlackWhite()
        );

        var tempFileName = $"{Guid.NewGuid()}.{fileName.Split(".")[1]}";
        tempFileName = await Helpers.SaveTempImage(image, tempFileName);
        image.Dispose();
        return tempFileName;
    }

    private Rectangle GetImageCropRectangle(string fileName, BoundingBox position, float degrees)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(fileName);

        image.Mutate(x => x
              .Crop(PolaroidSizeWithMargin(image, position, testImageModifier))
              .Rotate(degrees)
           );

        var (leftCrop, topCrop) = GetImageCorners(image, true);
        var (rightCrop, non) = GetImageCorners(image, false);
        topCrop += 5;
        var width = rightCrop - leftCrop;
        var height = (int)Math.Min((width * 1.215), image.Height - topCrop);
        var crop = new Rectangle(
            x: leftCrop * testImageModifier,
            y: topCrop * testImageModifier,
            width: width * testImageModifier,
            height: height * testImageModifier
       );

        image.Dispose();
        return crop;
    }

    private (int side, int top) GetImageCorners(Image<Rgba32> image, bool left = true)
    {
        var verticalHits = new List<int>();
        var side = 0;
        var top = 0;

        image.ProcessPixelRows(accessor =>
        {
            var pixelRange = image.Width / 4;

            for (int y = 10; y < accessor.Height / 2; y++)
            {
                var pixelsInLine = new List<(int x, int y)>();
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                ref Rgba32 topPixel = ref pixelRow[pixelRange];

                // Finn øverste linje fra toppen
                if (IsWhitePixel(topPixel))
                {
                    top = y;
                    pixelsInLine.Clear();

                    // Iterer over piksler mot venstre til Det ikke lenger er noen. 
                    var startingpoint = left ? pixelRange : image.Width - pixelRange;
                    for (int j = 0; j < startingpoint; j++)
                    {
                        var x = left ? startingpoint - j : startingpoint + j;

                        ref Rgba32 possibleCorner = ref pixelRow[x];

                        if (IsWhitePixel(possibleCorner))
                        {
                            possibleCorner = Color.Red;
                            pixelsInLine.Insert(0, (x, y));
                        }
                        else if (pixelsInLine.Count > 20)
                        {
                            // Sjekk hver piksel om det finnes flere hvite under den.
                            foreach (var coords in pixelsInLine)
                            {
                                for (int i = 1; i < 21; i++)
                                {
                                    if (!IsWhitePixel(image[coords.x, coords.y + i]))
                                        break;

                                    Span<Rgba32> rowCheck = accessor.GetRowSpan(coords.y + i);
                                    ref Rgba32 p = ref rowCheck[coords.x];
                                    p = Color.Green;

                                    if (i == 20)
                                    {
                                        top = coords.y;
                                        side = coords.x;
                                    }
                                }
                            }
                            pixelsInLine.Clear();
                        }
                        else
                        {
                            side = 0;
                            break;
                        }

                        if (side != 0)
                            break;
                    }
                }
                if (side != 0)
                {
                    break;
                }
            }
        });
        return (side, top);
    }

    private static bool IsWhitePixel(Rgba32 pixel) => (pixel.B >= 150 && pixel.G >= 150 && pixel.R >= 150 && pixel.A != 0)
        || (pixel.B == 0 && pixel.G == 128 && pixel.R == 0 && pixel.A != 0)
        || (pixel.B == 0 && pixel.G == 0 && pixel.R == 255 && pixel.A != 0);

    private static float GetImageRotationDegrees(string fileName, BoundingBox position)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(fileName);

        /// Cropping image with margin to adjust rotation
        image.Mutate(x => x
              .Crop(PolaroidSizeWithMargin(image, position, testImageModifier))
              );
        float degrees = 0;

        var leftTopOfPolaroid = -1;
        var rightTopOfPolaroid = -1;
        var pictureIsNotLevel = true;

        while (pictureIsNotLevel && degrees < 20)
        {
            float iterationDegrees = 0;
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    ref Rgba32 leftPixel = ref pixelRow[accessor.Width / 4];

                    if (IsWhitePixel(leftPixel))
                    {
                        leftTopOfPolaroid = y;
                        break;
                    }
                }

                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    ref Rgba32 rightPixel = ref pixelRow[accessor.Width - (accessor.Width / 4)];
                    if (IsWhitePixel(rightPixel))
                    {
                        rightTopOfPolaroid = y;
                        break;
                    }
                }
                if (leftTopOfPolaroid > rightTopOfPolaroid)
                    iterationDegrees = (float)0.1;

                if (leftTopOfPolaroid < rightTopOfPolaroid)
                    iterationDegrees = (float)-0.1;

                if (leftTopOfPolaroid == rightTopOfPolaroid)
                    pictureIsNotLevel = false;
            });

            degrees += iterationDegrees;
            image.Mutate(x =>
                 x.Rotate(iterationDegrees)
             );
        }

        image.Dispose();

        return degrees;
    }

    private static Rational[] GPSRational(double x)
    {
        uint denominator = 1;
        double absAngleInDeg = Math.Abs(x);
        var degreesInt = (uint)(absAngleInDeg);
        absAngleInDeg -= degreesInt;
        var minutesInt = (uint)(absAngleInDeg * 60.0);
        absAngleInDeg -= minutesInt / 60.0;
        var secondsInt = (uint)(absAngleInDeg * 3600.0);
        return new Rational[3]{
            new Rational(degreesInt, denominator),
            new Rational(minutesInt, denominator),
            new Rational(secondsInt*1000000, 1000000),
        };
    }
    private static Rectangle PolaroidSizeWithMargin(Image image, BoundingBox position, int mod) => new Rectangle(
             (int)(image.Width * position.Left) - 60 / mod,
             (int)(image.Height * position.Top) - 70 / mod,
             (int)(image.Width * position.Width) + 100 / mod,
             (int)(image.Height * position.Height) + 100 / mod);

}