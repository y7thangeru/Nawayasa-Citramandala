using NawayasaCitramandala.Core;
using NawayasaCitramandala.Vision;
using NawayasaCitramandala.Imaging;
using NawayasaCitramandala.ImageIO;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class VisionAndDitherTests
{
    [Fact]
    public void BlueNoiseDither_ProducesPureBinaryOutput()
    {
        int w = 64;
        int h = 64;
        byte[] bgra = new byte[w * h * 4];

        // Gradient image
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int p = (y * w + x) * 4;
                byte lum = (byte)((x * 255) / (w - 1));
                bgra[p] = lum;
                bgra[p + 1] = lum;
                bgra[p + 2] = lum;
                bgra[p + 3] = 255;
            }
        }

        byte[] binary = BlueNoiseGrayscale.DitherToBinary(w, h, bgra, 1.0);
        Assert.Equal(w * h, binary.Length);

        // Every pixel must be strictly 0 or 255
        foreach (byte b in binary)
        {
            Assert.True(b == 0 || b == 255, $"Expected binary 0 or 255, got {b}");
        }
    }

    [Fact]
    public void ImageDitherProcessor_HandlesAllAlgorithms()
    {
        int w = 32;
        int h = 32;
        byte[] bgra = new byte[w * h * 4];
        Array.Fill(bgra, (byte)128); // Mid-gray

        var raster = new BgraRaster(w, h, bgra);

        foreach (DitherAlgorithm algo in Enum.GetValues<DitherAlgorithm>())
        {
            var result = ImageDitherProcessor.Process(raster, algo, 1.0, OpticalDyeType.AzoDarkBlue);
            Assert.Equal(w, result.Width);
            Assert.Equal(h, result.Height);
            Assert.Equal(w * h * 4, result.Pixels.Length);
        }
    }

    [Fact]
    public void DiscVisionCalibrator_DetectsSyntheticDisc()
    {
        int size = 256;
        byte[] bgra = new byte[size * size * 4];
        double cx = size / 2.0;
        double cy = size / 2.0;

        // Draw synthetic disc: center hole (black), data area (bright), background (black)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - cx;
                double dy = y - cy;
                double dist = Math.Sqrt((dx * dx) + (dy * dy));
                int p = (y * size + x) * 4;

                if (dist >= 35 && dist <= 110)
                {
                    // Disc data area
                    bgra[p] = 200;
                    bgra[p + 1] = 200;
                    bgra[p + 2] = 200;
                    bgra[p + 3] = 255;
                }
                else
                {
                    // Center spindle hole or outside disc
                    bgra[p] = 20;
                    bgra[p + 1] = 20;
                    bgra[p + 2] = 20;
                    bgra[p + 3] = 255;
                }
            }
        }

        var raster = new BgraRaster(size, size, bgra);
        var detection = DiscVisionCalibrator.DetectDiscBoundaries(raster);

        Assert.True(detection.Confidence > 0);
        Assert.True(detection.OuterRadiusPixels > 80);
        Assert.True(detection.InnerRadiusPixels < detection.OuterRadiusPixels);
    }
}
