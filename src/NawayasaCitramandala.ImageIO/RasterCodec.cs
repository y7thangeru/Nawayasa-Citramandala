using StbImageSharp;
using StbImageWriteSharp;
using WriteComponents = StbImageWriteSharp.ColorComponents;

namespace NawayasaCitramandala.ImageIO;

public sealed record BgraRaster(int Width, int Height, byte[] Pixels);

/// <summary>
/// Cross-platform codec to read/write Straight BGRA32 and Gray8 images.
/// </summary>
public static class RasterCodec
{
    public static BgraRaster LoadBgra32(string path, long maxFileBytes = long.MaxValue, long maxPixels = int.MaxValue / 4)
    {
        using FileStream stream = File.OpenRead(Path.GetFullPath(path));
        CheckFileSize(stream, maxFileBytes);
        return Decode(stream, maxPixels);
    }

    public static BgraRaster LoadBgra32FromBytes(byte[] bytes, long maxPixels = int.MaxValue / 4)
    {
        using MemoryStream stream = new(bytes);
        return Decode(stream, maxPixels);
    }

    public static void ValidateSource(string path, long maxFileBytes, long maxPixels)
    {
        using FileStream stream = File.OpenRead(Path.GetFullPath(path));
        CheckFileSize(stream, maxFileBytes);
        _ = ReadHeader(stream, maxPixels);
    }

    public static void SavePng(string path, int width, int height, byte[] pixels, bool grayscale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (pixels.Length != checked(width * height * (grayscale ? 1 : 4)))
            throw new ArgumentException("Pixel buffer length does not match the dimensions and format.", nameof(pixels));

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!grayscale)
        {
            pixels = (byte[])pixels.Clone();
            SwapRedAndBlue(pixels);
        }

        using FileStream output = File.Create(path);
        new ImageWriter().WritePng(pixels, width, height,
            grayscale ? WriteComponents.Grey : WriteComponents.RedGreenBlueAlpha, output);
    }

    private static ImageInfo ReadHeader(Stream stream, long maxPixels)
    {
        ImageInfo info = ImageInfo.FromStream(stream)
            ?? throw new InvalidDataException("Unsupported or invalid image format. Use PNG, JPEG, BMP or GIF.");
        CheckDimensions(info.Width, info.Height, maxPixels);
        return info;
    }

    private static BgraRaster Decode(Stream stream, long maxPixels)
    {
        _ = ReadHeader(stream, maxPixels);
        stream.Position = 0;
        ImageResult image = ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        SwapRedAndBlue(image.Data);
        return new BgraRaster(image.Width, image.Height, image.Data);
    }

    private static void SwapRedAndBlue(byte[] pixels)
    {
        for (int i = 0; i < pixels.Length; i += 4)
            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
    }

    private static void CheckFileSize(FileStream stream, long maxBytes)
    {
        if (stream.Length > maxBytes)
            throw new ArgumentException($"The source image must be no larger than {maxBytes / 1_048_576d:G} MiB.", "path");
    }

    private static void CheckDimensions(int width, int height, long maxPixels)
    {
        if (width <= 0 || height <= 0 || (long)width * height > Math.Min(maxPixels, int.MaxValue / 4))
            throw new ArgumentException(FormattableString.Invariant(
                $"The source image must contain between 1 and {Math.Min(maxPixels, int.MaxValue / 4):N0} pixels."), "path");
    }
}
