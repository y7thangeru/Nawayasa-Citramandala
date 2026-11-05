using NawayasaCitramandala.Core;
using NawayasaCitramandala.ImageIO;

namespace NawayasaCitramandala.Imaging;

public enum DitherAlgorithm
{
    NoneThreshold,
    FloydSteinberg,
    Atkinson,
    BayerOrdered4x4,
    BlueNoiseVoidCluster
}

/// <summary>
/// High-performance cross-platform dither processor for disc surface imaging.
/// </summary>
public static class ImageDitherProcessor
{
    private static readonly byte[] Bayer4x4 =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };

    public static BgraRaster Process(
        BgraRaster source,
        DitherAlgorithm algorithm,
        double strength = 1.0,
        OpticalDyeType dyeType = OpticalDyeType.StandardGeneric)
    {
        ArgumentNullException.ThrowIfNull(source);
        int w = source.Width;
        int h = source.Height;
        byte[] src = source.Pixels;
        byte[] dst = new byte[w * h * 4];

        var dye = DyeCharacteristics.FromType(dyeType);
        double gamma = dye.GammaCorrection;

        if (algorithm == DitherAlgorithm.BlueNoiseVoidCluster)
        {
            byte[] binary = BlueNoiseGrayscale.DitherToBinary(w, h, src, strength);
            for (int i = 0; i < binary.Length; i++)
            {
                byte val = binary[i];
                int p = i * 4;
                dst[p] = val;
                dst[p + 1] = val;
                dst[p + 2] = val;
                dst[p + 3] = src[p + 3];
            }
            return new BgraRaster(w, h, dst);
        }

        // Grayscale conversion with dye gamma
        double[] gray = new double[w * h];
        for (int i = 0; i < gray.Length; i++)
        {
            int p = i * 4;
            double b = src[p] / 255.0;
            double g = src[p + 1] / 255.0;
            double r = src[p + 2] / 255.0;
            double lum = (0.299 * r) + (0.587 * g) + (0.114 * b);
            if (gamma != 1.0)
                lum = Math.Pow(lum, gamma);
            gray[i] = lum * 255.0;
        }

        if (algorithm == DitherAlgorithm.FloydSteinberg)
        {
            double[] buf = (double[])gray.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = (y * w) + x;
                    double oldVal = buf[idx];
                    double newVal = oldVal >= 128.0 ? 255.0 : 0.0;
                    buf[idx] = newVal;
                    double err = (oldVal - newVal) * strength;

                    if (x + 1 < w) buf[idx + 1] += err * (7.0 / 16.0);
                    if (y + 1 < h)
                    {
                        if (x - 1 >= 0) buf[idx + w - 1] += err * (3.0 / 16.0);
                        buf[idx + w] += err * (5.0 / 16.0);
                        if (x + 1 < w) buf[idx + w + 1] += err * (1.0 / 16.0);
                    }
                }
            }
            for (int i = 0; i < buf.Length; i++)
            {
                byte val = (byte)Math.Clamp(Math.Round(buf[i]), 0.0, 255.0);
                int p = i * 4;
                dst[p] = val;
                dst[p + 1] = val;
                dst[p + 2] = val;
                dst[p + 3] = src[p + 3];
            }
            return new BgraRaster(w, h, dst);
        }

        if (algorithm == DitherAlgorithm.BayerOrdered4x4)
        {
            for (int y = 0; y < h; y++)
            {
                int my = (y % 4) * 4;
                for (int x = 0; x < w; x++)
                {
                    int idx = (y * w) + x;
                    double threshold = (Bayer4x4[my + (x % 4)] / 16.0) * 255.0;
                    double effective = (128.0 * (1.0 - strength)) + (threshold * strength);
                    byte val = gray[idx] >= effective ? (byte)255 : (byte)0;
                    int p = idx * 4;
                    dst[p] = val;
                    dst[p + 1] = val;
                    dst[p + 2] = val;
                    dst[p + 3] = src[p + 3];
                }
            }
            return new BgraRaster(w, h, dst);
        }

        // Default NoneThreshold
        for (int i = 0; i < gray.Length; i++)
        {
            byte val = gray[i] >= 128.0 ? (byte)255 : (byte)0;
            int p = i * 4;
            dst[p] = val;
            dst[p + 1] = val;
            dst[p + 2] = val;
            dst[p + 3] = src[p + 3];
        }
        return new BgraRaster(w, h, dst);
    }
}
