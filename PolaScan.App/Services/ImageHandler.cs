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


namespace PolaScan.App.Services;

public class ImageHandler
{
    public Dictionary<string, string> SavedTemporaryFiles { get; set; }
    private readonly static int tempImageModifier = Constants.ImageProcessing.TempImageModifier;
    private readonly static int padding = Constants.ImageProcessing.ScanFilePadding;
    private readonly PolaScanApiService polaScanService;
    private readonly GoogleTimelineService timelineService;

    public ImageHandler(PolaScanApiService polaScanService, GoogleTimelineService timelineService)
    {
        SavedTemporaryFiles = new();
        Directory.CreateDirectory(Helpers.GetTempFilePath(string.Empty));
        this.polaScanService = polaScanService;
        this.timelineService = timelineService;
    }

    public void ClearTempFiles()
    {
        SavedTemporaryFiles.Clear();
        Helpers.DeleteTemporaryFiles();
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
        using var image = Image.Load(polaroid.AbsolutePath);
        image.Metadata.ExifProfile ??= new();
        if (polaroid.Location != null)
        {
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, GPSRational(polaroid.Location.Latitude));
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitude, GPSRational(polaroid.Location.Longitude));
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, polaroid.Location.Latitude > 0 ? "N" : "S");
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitudeRef, polaroid.Location.Longitude > 0 ? "E" : "W");
            image.Metadata.ExifProfile.SetValue(ExifTag.GPSAltitude, Rational.FromDouble(100));
            image.Metadata.ExifProfile.SetValue(ExifTag.ImageDescription, $"{polaroid.Location.Name}");
        }
        image.Metadata.ExifProfile.SetValue(ExifTag.Software, nameof(PolaScan));
        image.Metadata.ExifProfile.SetValue(ExifTag.Make, "Polaroid");
        image.Metadata.ExifProfile.SetValue(ExifTag.Model, Preferences.Default.Get(Constants.Settings.CameraModel, ""));
        image.Metadata.ExifProfile.SetValue(ExifTag.Copyright, Preferences.Default.Get(Constants.Settings.CopyRightText, ""));

        if (polaroid.Date != null)
        {
            var hour = new TimeOnly(polaroid.Hour, 0, 0);
            var dateTime = polaroid.Date.Value.ToDateTime(hour, DateTimeKind.Unspecified);
            image.Metadata.ExifProfile.SetValue(ExifTag.DateTimeOriginal, dateTime.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        await image.SaveAsync(uniqueName);
    }

    public async Task<PolaroidWithMeta> GetDateOnPolaroid(PolaroidWithMeta polaroid)
    {
        var lip = await GetPolaroidLipSection(polaroid).ConfigureAwait(false);
        polaroid.Date = await polaScanService.DetectDateInImage(lip, CultureInfo.CurrentCulture).ConfigureAwait(false);
        if (polaroid.Date != null && polaroid.Date != DateOnly.MinValue)
        {
            polaroid.Location = timelineService.GetDateLocation(polaroid.Date!.Value, polaroid.Hour);
        }

        return polaroid;
    }

    private async Task<string> GetPolaroidLipSection(PolaroidWithMeta polaroid)
    {
        using var image = Image.Load(polaroid.AbsolutePath);
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

    private async Task<string> SaveCompressedScanFile(string scanFile)
    {
        var tempName = await SaveCompressedTestImage(scanFile).ConfigureAwait(false);
        SavedTemporaryFiles.Add(scanFile, tempName);
        return tempName;
    }

    public async Task<PolaroidWithMeta> GetPolaroidFromScan(PolaroidWithMeta polaroid)
    {

        if (!SavedTemporaryFiles.TryGetValue(polaroid.ScanFile, out var compressedScanFileName))
        {
            compressedScanFileName = await SaveCompressedScanFile(polaroid.ScanFile);
        }

        var degrees = await GetImageRotationDegrees(compressedScanFileName, polaroid.LocationInScan).ConfigureAwait(false);
        var crop = await GetImageCropRectangle(compressedScanFileName, polaroid.LocationInScan, degrees);

        polaroid.Crop = crop;
        polaroid.Format = polaroid.ScanFile.Split(".")[1];
        polaroid.Rotation = degrees;

        polaroid = await CutPolaroidFromScan(polaroid).ConfigureAwait(false);
        polaroid = await GetDateOnPolaroid(polaroid).ConfigureAwait(false);

        return polaroid;
    }

    public async Task<PolaroidWithMeta> CutPolaroidFromScan(PolaroidWithMeta polaroid)
    {
        using var scanFile = Image.Load<Rgba32>(polaroid.ScanFile, out var format);

        try
        {
            // Cropping image with margin to adjust rotation
            scanFile.Mutate(x => x
                .Pad(scanFile.Width + padding, scanFile.Height + padding));
            scanFile.Mutate(x => x
                .Crop(PolaroidSizeWithMargin(scanFile, polaroid.LocationInScan, 1))
                .Rotate(polaroid.Rotation)
                .Crop(polaroid.Crop)
                .BackgroundColor(Color.White)
                );
        }
        catch
        {
            polaroid.Crop = PolaroidSizeWithMargin(scanFile, polaroid.LocationInScan, 1);

            scanFile.Mutate(x => x
              .Pad(scanFile.Width + padding, scanFile.Height + padding));
            scanFile.Mutate(x => x
                .Crop(PolaroidSizeWithMargin(scanFile, polaroid.LocationInScan, 1))
                .BackgroundColor(Color.White)
                );
        }

        if (polaroid.AbsolutePath == null)
        {
            var tempFileName = $"{Guid.NewGuid()}.{polaroid.Format}";
            polaroid.TempFileName = tempFileName;
            var path = await Helpers.SaveTempImage(scanFile, tempFileName);
            polaroid.AbsolutePath = path;
        }

        await Helpers.SaveTempImage(scanFile, polaroid.TempFileName);

        scanFile.Mutate(x => x.Resize(scanFile.Width / tempImageModifier, scanFile.Height / tempImageModifier));
        polaroid.PreviewData = scanFile.ToBase64String(format);
        scanFile.Dispose();

        return polaroid;
    }

    private async Task<string> SaveCompressedTestImage(string fileName)
    {
        using var image = Image.Load<Rgba32>(fileName);
        image.Mutate(x =>
        x
        .Resize(new Size { Width = image.Width / tempImageModifier })
        .GaussianSharpen()
        .GaussianBlur()
        .GaussianSharpen()
        .DetectEdges()
        .Pad(image.Width + padding / tempImageModifier, image.Height + padding / tempImageModifier)
        .BlackWhite()
        );

        var tempFileName = $"{Guid.NewGuid()}.{fileName.Split(".")[1]}";
        tempFileName = await Helpers.SaveTempImage(image, tempFileName);
        image.Dispose();
        return tempFileName;
    }

    private async Task<Rectangle> GetImageCropRectangle(string fileName, BoundingBox position, float degrees)
    {
        using var image = Image.Load<Rgba32>(fileName);

        image.Mutate(x => x
              .Crop(PolaroidSizeWithMargin(image, position, tempImageModifier))
              .Rotate(degrees)
           );

        await Helpers.SaveTempImage(image, Guid.NewGuid().ToString() + ".jpg");

        var (leftCrop, leftTop) = await GetImageCorner(image, true).ConfigureAwait(false);
        var (rightCrop, rightTop) = await GetImageCorner(image, false).ConfigureAwait(false);

        var width = 10 + (rightCrop - leftCrop) * tempImageModifier;
        var height = (int)(width * Constants.ImageProcessing.HeightToWidthRatio);
        var crop = new Rectangle(
             x: (leftCrop * tempImageModifier) - 10,
             y: 10 + ((leftTop + rightTop) / 2) * tempImageModifier,
            width: width,
            height: height
       );

        image.Dispose();
        return crop;
    }

    private static async Task<(int side, int top)> GetImageCorner(Image<Rgba32> image, bool left = true)
    {
        var verticalHits = new List<int>();
        var side = 0;
        var top = 0;
        var consectutiveReq = 50;
        image.ProcessPixelRows(accessor =>
        {
            var pixelRange = image.Width / 3;

            for (var y = 10; y < accessor.Height / 2; y++)
            {
                var pixelsInLine = new List<(int x, int y)>();
                var pixelRow = accessor.GetRowSpan(y);

                ref Rgba32 topPixel = ref pixelRow[pixelRange];

                if (IsWhitePixel(topPixel))
                {
                    top = y;
                    pixelsInLine.Clear();

                    var startingpoint = left ? pixelRange : image.Width - pixelRange;
                    for (var j = 0; j < startingpoint; j++)
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
                                for (var i = 1; i < consectutiveReq + 1; i++)
                                {
                                    if (!IsWhitePixel(image[coords.x, coords.y + i]))
                                        break;

                                    var rowCheck = accessor.GetRowSpan(coords.y + i);
                                    ref Rgba32 p = ref rowCheck[coords.x];
                                    //p = Color.Green;

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

    private static bool IsWhitePixel(Rgba32 pixel) => pixel.B >= 100 && pixel.G >= 100 && pixel.R >= 100 && pixel.A != 0
        || pixel.B == 0 && pixel.G == 128 && pixel.R == 0 && pixel.A != 0;

    private static async Task<float> GetImageRotationDegrees(string fileName, BoundingBox position)
    {
        using var image = Image.Load<Rgba32>(fileName);

        // Cropping image with margin to adjust rotation
        image.Mutate(x => x
              .Crop(PolaroidSizeWithMargin(image, position, tempImageModifier))
              );
        float degrees = 0;

        var leftTop = -1;
        var rightTop = -1;
        var pictureIsNotLevel = true;

        var fromMiddle = (int)(image.Width / 5);

        while (pictureIsNotLevel && degrees < 30 && degrees > -30)
        {
            if (degrees != 0 && Math.Abs(rightTop - leftTop) < 5)
            {
                (var leftCrop, leftTop) = await GetImageCorner(image, true);
                (var rightCrop, rightTop) = await GetImageCorner(image, false);
            }
            else
            {
                leftTop = FindTopOfPolaroid(image, fromMiddle);
                rightTop = FindTopOfPolaroid(image, image.Width - fromMiddle);
            }

            float iterationDegrees = 0;
            var diff = Math.Abs(rightTop - leftTop);
            var deg = diff > 1 ? 1 : 0.1;

            if (leftTop < rightTop)
                iterationDegrees = (float)-deg;

            if (leftTop > rightTop)
                iterationDegrees = (float)deg;

            if (leftTop == rightTop)
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
            for (var y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);

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
                else
                {
                    currentPixel = Color.Blue;
                }
            }

        });
        return targetPixel;
    }

    private static Rational[] GPSRational(double x)
    {
        uint denominator = 1;
        var absAngleInDeg = Math.Abs(x);
        var degreesInt = (uint)absAngleInDeg;
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
    private static Rectangle PolaroidSizeWithMargin(Image image, BoundingBox position, int mod)
    {
        var left = (int)(image.Width * position.Left);
        left -= Math.Max(100 / mod, 0);

        var top = (int)(image.Height * position.Top);
        top -= Math.Max(100 / mod, 0);

        var width = (int)(image.Width * position.Width);
        width += Math.Min((width / 3) / mod, image.Width - (width + left));

        var height = (int)(image.Height * position.Height);
        height += Math.Min(height / 2, image.Height - (height + top));

        return new Rectangle(left, top, width, height);
    }
}
