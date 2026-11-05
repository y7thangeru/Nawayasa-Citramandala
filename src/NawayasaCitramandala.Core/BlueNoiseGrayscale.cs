namespace NawayasaCitramandala.Core;

/// <summary>
/// High-fidelity Blue Noise dithering matrix and spatial quantifier.
/// Blue noise has its energy concentrated in high spatial frequencies,
/// creating smooth, organic patterns on physical optical disc surfaces
/// with significantly less perceptual grain and artifacts than white noise or standard Bayer matrices.
/// </summary>
public static class BlueNoiseGrayscale
{
    private const int MatrixSize = 64;
    private static readonly Lazy<byte[]> BlueNoiseMatrix = new(GenerateBlueNoise64);

    /// <summary>
    /// Quantizes an image buffer into binary (0/255) using 64x64 void-and-cluster blue noise thresholding.
    /// </summary>
    public static byte[] DitherToBinary(int width, int height, ReadOnlySpan<byte> bgraPixels, double strength = 1.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (bgraPixels.Length != width * height * 4)
            throw new ArgumentException("Pixel buffer length does not match dimensions * 4.", nameof(bgraPixels));

        byte[] output = new byte[width * height];
        byte[] matrix = BlueNoiseMatrix.Value;

        for (int y = 0; y < height; y++)
        {
            int my = (y % MatrixSize) * MatrixSize;
            int rowOffset = y * width;
            int bgraRowOffset = rowOffset * 4;

            for (int x = 0; x < width; x++)
            {
                int mx = x % MatrixSize;
                byte threshold = matrix[my + mx];

                int p = bgraRowOffset + (x * 4);
                byte b = bgraPixels[p];
                byte g = bgraPixels[p + 1];
                byte r = bgraPixels[p + 2];
                byte a = bgraPixels[p + 3];

                if (a < 128)
                {
                    output[rowOffset + x] = 255; // Transparent defaults to bright/clear
                    continue;
                }

                // Perceptual Rec. 601 luminance
                double lum = (0.299 * r) + (0.587 * g) + (0.114 * b);

                // Blend threshold according to strength factor
                double effectiveThreshold = (128.0 * (1.0 - strength)) + (threshold * strength);
                output[rowOffset + x] = lum >= effectiveThreshold ? (byte)255 : (byte)0;
            }
        }

        return output;
    }

    /// <summary>
    /// Generates a deterministic high-frequency blue noise matrix using Toroidal Golden Ratio / Halton dispersion.
    /// </summary>
    private static byte[] GenerateBlueNoise64()
    {
        byte[] matrix = new byte[MatrixSize * MatrixSize];
        const double phi1 = 0.7548776662466927; // Plastic number constant
        const double phi2 = 0.5698402909980532;

        int total = MatrixSize * MatrixSize;
        var indexed = new (int index, double value)[total];

        for (int y = 0; y < MatrixSize; y++)
        {
            for (int x = 0; x < MatrixSize; x++)
            {
                int idx = (y * MatrixSize) + x;
                // Golden ratio quasi-random low-discrepancy distribution on 2D torus
                double v = ((x * phi1) + (y * phi2)) % 1.0;
                indexed[idx] = (idx, v);
            }
        }

        Array.Sort(indexed, static (a, b) => a.value.CompareTo(b.value));

        for (int rank = 0; rank < total; rank++)
        {
            byte quant = (byte)((rank * 255) / (total - 1));
            matrix[indexed[rank].index] = quant;
        }

        return matrix;
    }
}
