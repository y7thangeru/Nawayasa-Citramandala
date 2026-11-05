using NawayasaCitramandala.ImageIO;

namespace NawayasaCitramandala.Imaging;

public sealed record CalibrationTargetOptions(
    int Resolution = 2400,
    double InnerRadiusMm = 24.0,
    double OuterRadiusMm = 58.0,
    int RadialSpokesCount = 24,
    double SpokeLineWidthPixels = 6.0,
    bool IncludeConcentricRings = true,
    bool IncludeStepWedge = true);

/// <summary>
/// Synthesizes high-precision calibration disc artwork containing:
/// - Radial spokes for measuring 3rd-order track pitch polynomial curvature (c1, c2, c3).
/// - Concentric rings at 5mm intervals for verifying scale and eccentricity.
/// - Gray contrast step wedges for testing optical dye reflectance response.
/// </summary>
public static class CalibrationTargetGenerator
{
    private const double StandardOuterRadiusMm = 60.0;

    public static BgraRaster Generate(CalibrationTargetOptions? options = null)
    {
        options ??= new CalibrationTargetOptions();
        int size = options.Resolution;
        byte[] bgra = new byte[size * size * 4];

        // Background: Clean white (255, 255, 255, 255)
        Array.Fill(bgra, (byte)255);

        double cx = size / 2.0;
        double cy = size / 2.0;
        double pixelsPerMm = (size / 2.0) / StandardOuterRadiusMm;

        double innerRadiusPix = options.InnerRadiusMm * pixelsPerMm;
        double outerRadiusPix = options.OuterRadiusMm * pixelsPerMm;

        // 1. Draw radial spokes (dark lines on white ground)
        if (options.RadialSpokesCount > 0)
        {
            double angleStep = (2.0 * Math.PI) / options.RadialSpokesCount;
            for (int i = 0; i < options.RadialSpokesCount; i++)
            {
                double angle = i * angleStep;
                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);

                // Cardinal axes (0, 90, 180, 270) are drawn slightly wider
                bool isCardinal = (i % (options.RadialSpokesCount / 4)) == 0;
                double halfWidth = (isCardinal ? options.SpokeLineWidthPixels * 1.5 : options.SpokeLineWidthPixels) / 2.0;

                // Step along ray from inner to outer radius
                for (double r = innerRadiusPix; r <= outerRadiusPix; r += 0.5)
                {
                    double px = cx + (r * cos);
                    double py = cy + (r * sin);

                    // Draw line thickness perpendicular to ray
                    for (double w = -halfWidth; w <= halfWidth; w += 0.5)
                    {
                        int ix = (int)Math.Round(px - (w * sin));
                        int iy = (int)Math.Round(py + (w * cos));

                        if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                        {
                            int p = (iy * size + ix) * 4;
                            bgra[p] = 0;     // B
                            bgra[p + 1] = 0; // G
                            bgra[p + 2] = 0; // R
                            bgra[p + 3] = 255;
                        }
                    }
                }
            }
        }

        // 2. Draw concentric calibrated rings at 5mm intervals
        if (options.IncludeConcentricRings)
        {
            for (double rMm = 25.0; rMm <= 55.0; rMm += 5.0)
            {
                double ringPix = rMm * pixelsPerMm;
                int steps = (int)(2.0 * Math.PI * ringPix * 2);

                for (int s = 0; s < steps; s++)
                {
                    double theta = (s * 2.0 * Math.PI) / steps;
                    for (double dr = -1.5; dr <= 1.5; dr += 0.5)
                    {
                        int ix = (int)Math.Round(cx + ((ringPix + dr) * Math.Cos(theta)));
                        int iy = (int)Math.Round(cy + ((ringPix + dr) * Math.Sin(theta)));

                        if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                        {
                            int p = (iy * size + ix) * 4;
                            bgra[p] = 20;
                            bgra[p + 1] = 20;
                            bgra[p + 2] = 20;
                            bgra[p + 3] = 255;
                        }
                    }
                }
            }
        }

        // 3. Draw step wedge patches in the inner hub zone (testing optical dye contrast)
        if (options.IncludeStepWedge)
        {
            byte[] contrastSteps = [0, 64, 128, 192, 255];
            double wedgeR1 = (options.InnerRadiusMm - 4.0) * pixelsPerMm;
            double wedgeR0 = (options.InnerRadiusMm - 8.0) * pixelsPerMm;

            if (wedgeR0 > 0 && wedgeR1 > wedgeR0)
            {
                int numPatches = contrastSteps.Length;
                double patchAngleSpan = (2.0 * Math.PI) / numPatches;

                for (int pIdx = 0; pIdx < numPatches; pIdx++)
                {
                    byte grayVal = contrastSteps[pIdx];
                    double startA = pIdx * patchAngleSpan;
                    double endA = (pIdx + 0.85) * patchAngleSpan;

                    for (double r = wedgeR0; r <= wedgeR1; r += 0.5)
                    {
                        int steps = (int)(r * (endA - startA) * 2);
                        for (int s = 0; s < steps; s++)
                        {
                            double a = startA + (s * (endA - startA) / steps);
                            int ix = (int)Math.Round(cx + (r * Math.Cos(a)));
                            int iy = (int)Math.Round(cy + (r * Math.Sin(a)));

                            if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                            {
                                int p = (iy * size + ix) * 4;
                                bgra[p] = grayVal;
                                bgra[p + 1] = grayVal;
                                bgra[p + 2] = grayVal;
                                bgra[p + 3] = 255;
                            }
                        }
                    }
                }
            }
        }

        return new BgraRaster(size, size, bgra);
    }
}
