using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using PolaScan.App.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;

namespace PolaScan.App;

public class ImageHandler
{
    public Dictionary<string, string> SavedTemporaryFiles { get; set; }
    private readonly static int testImageModifier = 4;
    private readonly static int padding = 250;
    public ImageHandler()
    {
        SavedTemporaryFiles = new();
        Directory.CreateDirectory(Helpers.GetTempFilePath(string.Empty));
    }

    public async Task MoveToDestination(PolaroidWithMeta polaroid)
    {
        var destSetting = Preferences.Default.Get(Constants.Settings.DesitnationPath, "");
        var destinationPath = polaroid.Date != null ? $"{destSetting}\\{polaroid.Date.Value.Year}\\{polaroid.Date.Value.Month}" : destSetting;
        Directory.CreateDirectory(destinationPath);
        var i = 0;
        var uniqueName = destinationPath + $"\\{polaroid.FileName(i)}";
        while (File.Exists(uniqueName))
        {
            uniqueName = destinationPath + $"\\{polaroid.FileName(i)}";
            i++;
        }
        using Image image = Image.Load(polaroid.AbsolutePath);
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
        image.Metadata.ExifProfile.SetValue(ExifTag.Model, Preferences.Default.Get(Constants.Settings.CameraModel, ""));
        image.Metadata.ExifProfile.SetValue(ExifTag.Copyright, Preferences.Default.Get(Constants.Settings.CopyRightText, ""));
        
        if(polaroid.Date != null)
            image.Metadata.ExifProfile.SetValue(ExifTag.DateTimeOriginal, polaroid.Date.Value.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture));
        
        await image.SaveAsync(uniqueName);
    }

    public async Task<string> SavePolaroidLipSection(PolaroidWithMeta polaroid)
    {
        using Image image = Image.Load(polaroid.AbsolutePath);
        image.Mutate(x => x
             .Crop(new Rectangle(0, Convert.ToInt32(Math.Floor(image.Height * 0.8)), image.Width, Convert.ToInt32(Math.Floor(image.Height * 0.2))))
             .Grayscale()
             .GaussianSharpen()
             );
        var tempFileName = $"{Guid.NewGuid()}.{polaroid.Format}";
        tempFileName = await Helpers.SaveTempImage(image, tempFileName);
        image.Dispose();
        return tempFileName;
    }

    public async Task<ICollection<PolaroidWithMeta>> GetPolaroidsFromScan(string scanFile, List<BoundingBox>? locations)
    {
        var polaroidsInScan = new List<PolaroidWithMeta>();

        string compressedScanFileName = await SaveCompressedTestImage(scanFile);

        foreach (var position in locations)
        {
            var degrees = GetImageRotationDegrees(compressedScanFileName, (position));
            var crop = GetImageCropRectangle(compressedScanFileName, (position), degrees);

            var polaroid = new PolaroidWithMeta()
            {
                LocationInScan = position,
                Crop = crop,
                Format = scanFile.Split(".")[1],
                Rotation = degrees,
                ScanFile = scanFile,
            };

            polaroid = await CutPolaroidFromScan(polaroid);

            polaroidsInScan.Add(polaroid);
        }
        return polaroidsInScan;
    }

    public async Task<PolaroidWithMeta> CutPolaroidFromScan(PolaroidWithMeta polaroid)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(polaroid.ScanFile, out var format);

        /// Cropping image with margin to adjust rotation
        image.Mutate(x => x
            .Pad(image.Width + padding, image.Height + padding));
        image.Mutate(x => x
            .Crop(PolaroidSizeWithMargin(image, polaroid.LocationInScan, 1))
            .Rotate(polaroid.Rotation)
            .Crop(polaroid.Crop)
            );

        if (polaroid.AbsolutePath == null)
        {
            var tempFileName = $"{Guid.NewGuid()}.{polaroid.Format}";
            polaroid.TempFileName = tempFileName;
            var path = await Helpers.SaveTempImage(image, tempFileName);
            polaroid.AbsolutePath = path;
        }

        await Helpers.SaveTempImage(image, polaroid.TempFileName);

        image.Mutate(x => x.Resize(image.Width / testImageModifier, image.Height / testImageModifier));
        polaroid.PreviewData = image.ToBase64String(format);
        image.Dispose();

        return polaroid;
    }

    private async Task<string> SaveCompressedTestImage(string fileName)
    {
        using var image = Image.Load<Rgba32>(fileName);
        image.Mutate(x =>
        x
        .Resize(new Size { Width = image.Width / testImageModifier })
        .GaussianSharpen()
        .GaussianBlur()
        .GaussianSharpen()
        .DetectEdges()
        .Pad((image.Width) + (padding / testImageModifier), (image.Height) + (padding / testImageModifier))
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

        var (leftCrop, leftTop) = GetImageCorner(image, true);
        var (rightCrop, rightTop) = GetImageCorner(image, false);


        var topCrop = leftTop + 5;
        var width = rightCrop - leftCrop;
        var height = (int)Math.Min((width * 1.21590909), image.Height - topCrop);
        var crop = new Rectangle(
            x: leftCrop * testImageModifier,
            y: topCrop * testImageModifier,
            width: width * testImageModifier,
            height: height * testImageModifier
       );

        image.Dispose();
        return crop;
    }

    private static (int side, int top) GetImageCorner(Image<Rgba32> image, bool left = true)
    {
        var verticalHits = new List<int>();
        var side = 0;
        var top = 0;
        var consectutiveReq = 40;
        image.ProcessPixelRows(accessor =>
        {
            var pixelRange = image.Width / 3;

            for (int y = 10; y < accessor.Height / 2; y++)
            {
                var pixelsInLine = new List<(int x, int y)>();
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                ref Rgba32 topPixel = ref pixelRow[pixelRange];

                if (IsWhitePixel(topPixel))
                {
                    top = y;
                    pixelsInLine.Clear();

                    var startingpoint = left ? pixelRange : image.Width - pixelRange;
                    for (int j = 0; j < startingpoint; j++)
                    {
                        var x = left ? startingpoint - j : startingpoint + j;

                        ref Rgba32 possibleCorner = ref pixelRow[x];

                        if (IsWhitePixel(possibleCorner))
                        {
                            pixelsInLine.Insert(0, (x, y));
                        }
                        else if (pixelsInLine.Count > consectutiveReq)
                        {
                            foreach (var coords in pixelsInLine)
                            {
                                for (int i = 1; i < consectutiveReq + 1; i++)
                                {
                                    if (!IsWhitePixel(image[coords.x, coords.y + i]))
                                        break;

                                    Span<Rgba32> rowCheck = accessor.GetRowSpan(coords.y + i);
                                    ref Rgba32 p = ref rowCheck[coords.x];
                                    // p = Color.Green;

                                    if (i == consectutiveReq)
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
    private static bool IsWhitePixel(Rgba32 pixel) => (pixel.B >= 100 && pixel.G >= 100 && pixel.R >= 100 && pixel.A != 0)
        || (pixel.B == 0 && pixel.G == 128 && pixel.R == 0 && pixel.A != 0);

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

        while (pictureIsNotLevel && degrees < 30 && degrees > -30)
        {
            float iterationDegrees = 0;
            var diff = Math.Abs(rightTopOfPolaroid - leftTopOfPolaroid);
            double deg = diff >= 1 ? (double)diff / 6 : 0.2;

            int fromMiddle = (int)(image.Width / 3.5);

            leftTopOfPolaroid = FindTopOfPolaroid(image, fromMiddle);
            rightTopOfPolaroid = FindTopOfPolaroid(image, image.Width - fromMiddle);

            if (leftTopOfPolaroid < rightTopOfPolaroid)
                iterationDegrees = (float)-deg;

            if (leftTopOfPolaroid > rightTopOfPolaroid)
                iterationDegrees = (float)deg;

            if (leftTopOfPolaroid == rightTopOfPolaroid)
                pictureIsNotLevel = false;

            degrees += iterationDegrees;
            image.Mutate(x =>
                 x.Rotate(iterationDegrees)
             );
        }

        image.Dispose();

        return degrees;
    }

    private static int FindTopOfPolaroid(Image<Rgba32> image, int x)
    {
        var consecutive = 0;
        var targetPixel = -1;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                ref Rgba32 currentPixel = ref pixelRow[x];

                if (IsWhitePixel(currentPixel))
                {
                    consecutive++;
                    if (consecutive == 3)
                    {
                        targetPixel = y;
                        break;
                    }
                }
            }

        });
        return targetPixel;
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