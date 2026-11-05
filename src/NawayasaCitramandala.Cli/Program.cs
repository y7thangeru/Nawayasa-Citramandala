using NawayasaCitramandala.Core;
using NawayasaCitramandala.Cd;
using NawayasaCitramandala.Dvd;
using NawayasaCitramandala.BluRay;
using NawayasaCitramandala.Vision;
using NawayasaCitramandala.Imaging;
using NawayasaCitramandala.ImageIO;

namespace NawayasaCitramandala.Cli;

public static class Program
{
    private static readonly DiscModuleCatalog Catalog = new(
    [
        new CdDiscModule(),
        new DvdDiscModule(),
        new BluRayDiscModule(BluRayImageLoader.Load)
    ]);

    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            PrintBanner();
            PrintHelp();
            return 0;
        }

        string cmd = args[0].ToLowerInvariant();

        if (cmd == "autocalibrate")
        {
            return RunAutoCalibration(args.Skip(1).ToArray());
        }

        if (cmd == "dither")
        {
            return RunDither(args.Skip(1).ToArray());
        }

        if (cmd == "target-generate")
        {
            return RunTargetGenerate(args.Skip(1).ToArray());
        }

        if (cmd == "simulate-3d")
        {
            return RunSimulate3D(args.Skip(1).ToArray());
        }

        try
        {
            DiscJobRequest request = new(args[0], args.Skip(1));
            IOpticalDiscModule module = Catalog.Resolve(request.Command);
            using CancellationTokenSource cancellation = new();

            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                if (!module.Descriptor.SupportsCancellation) return;
                e.Cancel = true;
                cancellation.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;
            try
            {
                DiscJobResult result = module.ExecuteAsync(request, new ConsoleProgress(), cancellation.Token)
                    .GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(result.Summary))
                    Console.WriteLine(result.Summary);
                return result.ExitCode;
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Job cancelled by user.");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
========================================================================
       NAWAYASA CITRAMANDALA - DISC IMAGE STUDIO (SPINOFF v2.0)
     Physical Layer Optical Disc Artwork Synthesis & Streaming Burn
========================================================================");
        Console.ResetColor();
        Console.WriteLine("Supports CD-R (Audio PCM / Mode 1), DVD-R (EFMPlus dispersion), BD-R (17PP 3T/6T)");
        Console.WriteLine();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: nawayasa-studio COMMAND [OPTIONS]");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Standard Optical Disc Modules:");
        Console.ResetColor();
        foreach (IOpticalDiscModule module in Catalog.Modules)
        {
            Console.WriteLine($"\n  [{module.Descriptor.DisplayName}] (Family: {module.Descriptor.Family})");
            foreach (DiscCommandDescriptor command in module.Descriptor.Commands)
                Console.WriteLine($"    {command.Name,-24} {command.Description}");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Spinoff Advanced Commands:");
        Console.ResetColor();
        Console.WriteLine("  autocalibrate            Analyze disc photograph with Computer Vision to find center and pitch curve.");
        Console.WriteLine("  dither                   Apply advanced Blue Noise or Error Diffusion dithering with dye profiling.");
        Console.WriteLine("  target-generate          Synthesize an all-in-one universal calibration disc artwork.");
        Console.WriteLine("  simulate-3d              Generate an interactive 3D WebGL simulator with Bragg optical diffraction iridescence.");
        Console.WriteLine();
    }

    private static int RunAutoCalibration(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: autocalibrate --photo <path-to-photo.jpg|png> [--inner-radius 24.0] [--outer-radius 58.0]");
            return 1;
        }

        string photoPath = "";
        double innerR = 24.0;
        double outerR = 58.0;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--photo" && i + 1 < args.Length) photoPath = args[++i];
            else if (args[i] == "--inner-radius" && i + 1 < args.Length) innerR = double.Parse(args[++i]);
            else if (args[i] == "--outer-radius" && i + 1 < args.Length) outerR = double.Parse(args[++i]);
        }

        if (string.IsNullOrEmpty(photoPath) || !File.Exists(photoPath))
        {
            Console.Error.WriteLine("Error: Please provide a valid photo path via --photo");
            return 1;
        }

        Console.WriteLine($"Analyzing disc photograph: {photoPath}");
        BgraRaster raster = RasterCodec.LoadBgra32(photoPath);
        Console.WriteLine($"Image loaded: {raster.Width}x{raster.Height} px");

        var detection = DiscVisionCalibrator.DetectDiscBoundaries(raster);
        Console.WriteLine($"Disc Center: ({detection.CenterX:F1}, {detection.CenterY:F1}) px");
        Console.WriteLine($"Resolution: {detection.PixelsPerMillimetre:F2} pixels/mm");
        Console.WriteLine($"Inner Hub Radius: {detection.EstimatedInnerRadiusMm:F2} mm ({detection.InnerRadiusPixels:F0} px)");
        Console.WriteLine($"Outer Edge Radius: {detection.EstimatedOuterRadiusMm:F2} mm ({detection.OuterRadiusPixels:F0} px)");
        Console.WriteLine($"Detection Confidence: {detection.Confidence:P0}");

        var profile = DiscVisionCalibrator.CalibrateFromPhoto(raster, innerR, outerR);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n[Computed 3rd-Order Pitch Polynomial]:");
        Console.WriteLine($"  c1 (linear)    : {profile.PitchLinearC1:F6}");
        Console.WriteLine($"  c2 (quadratic) : {profile.PitchQuadraticC2:F6}");
        Console.WriteLine($"  c3 (cubic)     : {profile.PitchCubicC3:F6}");
        Console.WriteLine($"  Rotation Angle : {profile.RotationAngleDegrees:F2} deg");
        Console.WriteLine($"  Residual RMS   : {profile.ResidualRmsError:F4} deg");
        Console.ResetColor();

        return 0;
    }

    private static int RunDither(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dither <input-image> <output-image> [--algo blue-noise|floyd-steinberg|bayer] [--dye azo|cyanine|phthalocyanine|bd-htl]");
            return 1;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        DitherAlgorithm algo = DitherAlgorithm.BlueNoiseVoidCluster;
        OpticalDyeType dye = OpticalDyeType.StandardGeneric;

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--algo" && i + 1 < args.Length)
            {
                string a = args[++i].ToLowerInvariant();
                algo = a switch
                {
                    "floyd-steinberg" => DitherAlgorithm.FloydSteinberg,
                    "bayer" => DitherAlgorithm.BayerOrdered4x4,
                    _ => DitherAlgorithm.BlueNoiseVoidCluster
                };
            }
            else if (args[i] == "--dye" && i + 1 < args.Length)
            {
                string d = args[++i].ToLowerInvariant();
                dye = d switch
                {
                    "azo" => OpticalDyeType.AzoDarkBlue,
                    "cyanine" => OpticalDyeType.CyanineGreen,
                    "phthalocyanine" => OpticalDyeType.PhthalocyanineGold,
                    "bd-htl" => OpticalDyeType.InorganicBdrHtl,
                    _ => OpticalDyeType.StandardGeneric
                };
            }
        }

        Console.WriteLine($"Loading source image: {inputPath}");
        BgraRaster src = RasterCodec.LoadBgra32(inputPath);
        Console.WriteLine($"Applying dither algorithm: {algo} with optical dye profile: {dye}");
        BgraRaster result = ImageDitherProcessor.Process(src, algo, 1.0, dye);
        RasterCodec.SavePng(outputPath, result.Width, result.Height, result.Pixels, grayscale: false);
        Console.WriteLine($"Dithered output saved: {outputPath}");
        return 0;
    }

    private static int RunTargetGenerate(string[] args)
    {
        string outputPath = args.Length > 0 ? args[0] : "calibration_target.png";
        int res = 2400;
        int spokes = 24;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--resolution" && i + 1 < args.Length) res = int.Parse(args[++i]);
            else if (args[i] == "--spokes" && i + 1 < args.Length) spokes = int.Parse(args[++i]);
        }

        Console.WriteLine($"Synthesizing Universal Calibration Target: {res}x{res} px, {spokes} spokes...");
        var options = new CalibrationTargetOptions(Resolution: res, RadialSpokesCount: spokes);
        var targetRaster = CalibrationTargetGenerator.Generate(options);
        RasterCodec.SavePng(outputPath, targetRaster.Width, targetRaster.Height, targetRaster.Pixels, grayscale: false);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Calibration target artwork created successfully: {outputPath}");
        Console.ResetColor();
        return 0;
    }

    private static int RunSimulate3D(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: simulate-3d <disc-image.png> <output.html> [--type cd|dvd|bluray] [--title 'My Disc Art']");
            return 1;
        }

        string imagePath = args[0];
        string htmlPath = args[1];
        OpticalTrackStandard media = OpticalTrackStandard.DvdMinusR;
        string title = "Nawayasa Citramandala Disc Art";

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--type" && i + 1 < args.Length)
            {
                string t = args[++i].ToLowerInvariant();
                media = t switch
                {
                    "cd" => OpticalTrackStandard.CdRedBook,
                    "bluray" or "bd" => OpticalTrackStandard.BluRayBdrSingleLayer,
                    _ => OpticalTrackStandard.DvdMinusR
                };
            }
            else if (args[i] == "--title" && i + 1 < args.Length)
            {
                title = args[++i];
            }
        }

        if (!File.Exists(imagePath))
        {
            Console.Error.WriteLine($"Error: Input disc image not found: {imagePath}");
            return 1;
        }

        Console.WriteLine($"Building 3D physical iridescence simulation for: {imagePath}");
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string base64 = Convert.ToBase64String(imageBytes);

        string html = Disc3DVisualizer.GenerateHtmlViewer(title, base64, media);
        File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Interactive 3D Simulation generated: {htmlPath}");
        Console.WriteLine("Open this HTML file in any modern web browser to view the disc rotating under dynamic light with Bragg diffraction rainbow sheen!");
        Console.ResetColor();
        return 0;
    }

    private sealed class ConsoleProgress : IProgress<DiscJobProgress>
    {
        public void Report(DiscJobProgress value)
        {
            if (!string.IsNullOrWhiteSpace(value.Message))
            {
                string frac = value.Fraction.HasValue ? $"[{value.Fraction.Value:P0}] " : "";
                Console.WriteLine($"{frac}{value.Message}");
            }
        }
    }
}
