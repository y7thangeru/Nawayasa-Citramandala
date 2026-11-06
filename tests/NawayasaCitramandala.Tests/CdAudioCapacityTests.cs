using NawayasaCitramandala.Cd;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class CdAudioCapacityTests
{
    [Fact]
    public void CalculateCapacity_ZeroTracks_ReturnsFullDiscForArtwork()
    {
        var tracks = Array.Empty<CdAudioTrack>();
        CdCapacityMetrics metrics = CdAudioDecoder.CalculateCapacity(tracks);

        Assert.Equal(TimeSpan.Zero, metrics.TotalAudioDuration);
        Assert.Equal(0u, metrics.TotalAudioSectors);
        Assert.Equal(359_849u, metrics.TotalDiscSectors);
        Assert.Equal(359_849u, metrics.RemainingArtworkSectors);
        Assert.Equal(0.0, metrics.AudioDiscPercentage);
        Assert.Equal(100.0, metrics.ArtworkDiscPercentage);
        Assert.Equal(24.5, metrics.AudioStartRadiusMm);
        Assert.Equal(24.5, metrics.AudioEndRadiusMm);
        Assert.Equal(24.5, metrics.ArtworkStartRadiusMm);
        Assert.Equal(56.8, metrics.ArtworkEndRadiusMm);
        Assert.Equal(32.3, Math.Round(metrics.ArtworkRadialWidthMm, 1));
        Assert.False(metrics.ExceedsCapacity);
    }

    [Fact]
    public void CalculateCapacity_HalfCapacityAudio_SplitsSurfaceAccurately()
    {
        // 40 minutes of audio = 40 * 60 * 75 = 180,000 sectors
        var track1 = new CdAudioTrack
        {
            TrackNumber = 1,
            FilePath = "track1.mp3",
            Title = "Song One",
            Duration = TimeSpan.FromMinutes(20),
            AudioBytes = 20 * 60 * 44100 * 4,
            TotalSectors = 20 * 60 * 75,
        };
        var track2 = new CdAudioTrack
        {
            TrackNumber = 2,
            FilePath = "track2.flac",
            Title = "Song Two",
            Duration = TimeSpan.FromMinutes(20),
            AudioBytes = 20 * 60 * 44100 * 4,
            TotalSectors = 20 * 60 * 75,
        };

        var tracks = new[] { track1, track2 };
        CdCapacityMetrics metrics = CdAudioDecoder.CalculateCapacity(tracks);

        Assert.Equal(TimeSpan.FromMinutes(40), metrics.TotalAudioDuration);
        Assert.Equal(180_000u, metrics.TotalAudioSectors);
        Assert.Equal(179_849u, metrics.RemainingArtworkSectors);
        Assert.True(metrics.AudioDiscPercentage > 49.0 && metrics.AudioDiscPercentage < 51.0);
        Assert.True(metrics.ArtworkDiscPercentage > 49.0 && metrics.ArtworkDiscPercentage < 51.0);

        // Audio end radius must be between inner (24.5) and outer (56.8)
        Assert.True(metrics.AudioEndRadiusMm > 40.0 && metrics.AudioEndRadiusMm < 45.0);
        // Artwork radial width must be positive
        Assert.True(metrics.ArtworkRadialWidthMm > 12.0 && metrics.ArtworkRadialWidthMm < 16.0);
        Assert.Equal(metrics.ArtworkStartRadiusMm, metrics.AudioEndRadiusMm);
        Assert.False(metrics.ExceedsCapacity);
    }

    [Fact]
    public void CalculateCapacity_OverCapacity_SetsExceedsCapacityTrue()
    {
        // 90 minutes of audio = 90 * 60 * 75 = 405,000 sectors (exceeds 359,849)
        var track = new CdAudioTrack
        {
            TrackNumber = 1,
            FilePath = "long.wav",
            Title = "Long Mix",
            Duration = TimeSpan.FromMinutes(90),
            AudioBytes = 90 * 60 * 44100 * 4,
            TotalSectors = 90 * 60 * 75,
        };

        CdCapacityMetrics metrics = CdAudioDecoder.CalculateCapacity(new[] { track });

        Assert.True(metrics.ExceedsCapacity);
        Assert.Equal(TimeSpan.Zero, metrics.RemainingAudioDuration);
        Assert.Equal(0u, metrics.RemainingArtworkSectors);
        Assert.Equal(0.0, metrics.ArtworkRadialWidthMm);
        Assert.Equal(100.0, metrics.AudioDiscPercentage);
        Assert.Equal(0.0, metrics.ArtworkDiscPercentage);
    }
}
