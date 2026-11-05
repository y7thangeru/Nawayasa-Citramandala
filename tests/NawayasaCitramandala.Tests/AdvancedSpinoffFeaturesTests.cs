using NawayasaCitramandala.Core;
using NawayasaCitramandala.Imaging;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class AdvancedSpinoffFeaturesTests
{
    [Fact]
    public void MediaFormatSpecification_AllStandards_AreAccuratelyConfigured()
    {
        var cd = MediaFormatSpecification.GetSpecification(OpticalTrackStandard.CdRedBook);
        Assert.Equal(1.60, cd.TrackPitchMicrons);
        Assert.Equal(2352, cd.SectorPayloadBytes);

        var dvdR = MediaFormatSpecification.GetSpecification(OpticalTrackStandard.DvdMinusR);
        Assert.Equal(0.74, dvdR.TrackPitchMicrons);
        Assert.Equal(2048, dvdR.SectorPayloadBytes);
        Assert.Equal(2295104, dvdR.TotalUserSectors);

        var dvdPlusR = MediaFormatSpecification.GetSpecification(OpticalTrackStandard.DvdPlusR);
        Assert.Contains("ADIP", dvdPlusR.GrooveModulation);

        var bdR = MediaFormatSpecification.GetSpecification(OpticalTrackStandard.BluRayBdrSingleLayer);
        Assert.Equal(0.32, bdR.TrackPitchMicrons);
        Assert.Equal(12219392, bdR.TotalUserSectors);

        var bdDl = MediaFormatSpecification.GetSpecification(OpticalTrackStandard.BluRayBdrDualLayer);
        Assert.Equal(2, bdDl.Layers);
        Assert.Equal(LayerTrackingDirection.OppositeTrackPathOtp, bdDl.TrackingDirection);

        var bdXl = MediaFormatSpecification.GetSpecification(OpticalTrackStandard.BluRayBdrTripleLayerXl);
        Assert.Equal(3, bdXl.Layers);
    }

    [Fact]
    public void CalibrationTargetGenerator_ProducesValidRaster()
    {
        var options = new CalibrationTargetOptions(Resolution: 512, RadialSpokesCount: 12);
        var raster = CalibrationTargetGenerator.Generate(options);

        Assert.Equal(512, raster.Width);
        Assert.Equal(512, raster.Height);
        Assert.Equal(512 * 512 * 4, raster.Pixels.Length);

        // Center pixel should be background white
        int centerP = ((256 * 512) + 256) * 4;
        Assert.Equal(255, raster.Pixels[centerP + 3]); // Alpha
    }

    [Fact]
    public void Disc3DVisualizer_GeneratesSelfContainedHtml()
    {
        string dummyBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        string html = Disc3DVisualizer.GenerateHtmlViewer(
            "Test Disc Simulation",
            dummyBase64,
            OpticalTrackStandard.DvdMinusR,
            24.0, 58.0);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("Test Disc Simulation", html);
        Assert.Contains("three.min.js", html);
        Assert.Contains("spectralColor", html); // Iridescence shader function
        Assert.Contains("uTrackPitch", html);
    }

    [Fact]
    public void Localization_DefaultsToEnglish_AndLoadsCatalogs()
    {
        NawayasaCitramandala.Localization.L.Initialize(null);
        Assert.Equal(NawayasaCitramandala.Localization.L.English, NawayasaCitramandala.Localization.L.CurrentLanguage);

        string previewText = NawayasaCitramandala.Localization.L.T("Xaml.Preview");
        Assert.Equal("Preview", previewText);

        string toneText = NawayasaCitramandala.Localization.L.T("Xaml.Tone");
        Assert.Equal("Tone", toneText);

        string exportText = NawayasaCitramandala.Localization.L.T("Xaml.NextExport");
        Assert.Equal("Next: Export", exportText);
    }
}
