using System.IO;
using System.Runtime.Versioning;
using NAudio.Wave;
using NawayasaCitramandala.Core;

namespace NawayasaCitramandala.Cd;

public sealed record CdAudioTrack
{
    public int TrackNumber { get; set; }
    public string FilePath { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public long AudioBytes { get; set; }
    public uint TotalSectors { get; set; }
    public string DurationFormatted => $"{(int)Duration.TotalMinutes:D2}:{Duration.Seconds:D2}";
    public string FileSizeFormatted => $"{AudioBytes / (1024 * 1024.0):F1} MB";
}

public sealed record CdCapacityMetrics
{
    public TimeSpan TotalAudioDuration { get; init; }
    public TimeSpan RemainingAudioDuration { get; init; }
    public TimeSpan MaxDiscDuration { get; init; } = TimeSpan.FromMinutes(79) + TimeSpan.FromSeconds(58);
    public uint TotalAudioSectors { get; init; }
    public uint TotalDiscSectors { get; init; } = 359_849;
    public uint RemainingArtworkSectors { get; init; }
    public double AudioDiscPercentage { get; init; }
    public double ArtworkDiscPercentage { get; init; }
    public double AudioStartRadiusMm { get; init; } = 24.5;
    public double AudioEndRadiusMm { get; init; }
    public double ArtworkStartRadiusMm { get; init; }
    public double ArtworkEndRadiusMm { get; init; } = 56.8;
    public double ArtworkRadialWidthMm { get; init; }
    public bool ExceedsCapacity => TotalAudioSectors > TotalDiscSectors;
}

[SupportedOSPlatform("windows")]
public static class CdAudioDecoder
{
    public const int SampleRate = 44100;
    public const int BitsPerSample = 16;
    public const int Channels = 2;
    public const int BytesPerSample = Channels * (BitsPerSample / 8); // 4 bytes
    public const int BytesPerSector = 2352;
    public const int SectorsPerSecond = 75;

    public static CdAudioTrack AnalyzeTrack(string filePath, int trackNumber = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Audio file not found.", filePath);
        }

        string title = Path.GetFileNameWithoutExtension(filePath);
        using var reader = new MediaFoundationReader(filePath);
        TimeSpan duration = reader.TotalTime;
        long sampleCount = (long)(duration.TotalSeconds * SampleRate);
        long rawPcmBytes = sampleCount * BytesPerSample;
        uint sectors = (uint)Math.Ceiling(duration.TotalSeconds * SectorsPerSecond);

        return new CdAudioTrack
        {
            TrackNumber = trackNumber,
            FilePath = filePath,
            Title = title,
            Duration = duration,
            AudioBytes = rawPcmBytes,
            TotalSectors = sectors,
        };
    }

    public static CdCapacityMetrics CalculateCapacity(
        IReadOnlyList<CdAudioTrack> tracks,
        CdDiscParameters? parameters = null)
    {
        double innerRadius = parameters?.InnerRadiusMm ?? 24.5;
        double outerRadius = parameters?.OuterRadiusMm ?? 56.8;
        uint maxSectors = (uint)(parameters?.Sectors ?? 359_849);

        uint totalAudioSectors = 0;
        long totalTicks = 0;
        foreach (var t in tracks)
        {
            totalAudioSectors += t.TotalSectors;
            totalTicks += t.Duration.Ticks;
        }

        TimeSpan totalAudioDuration = TimeSpan.FromTicks(totalTicks);
        double maxSeconds = (double)maxSectors / SectorsPerSecond;
        TimeSpan maxDuration = TimeSpan.FromSeconds(maxSeconds);
        TimeSpan remainingAudioDuration = totalAudioDuration < maxDuration
            ? maxDuration - totalAudioDuration
            : TimeSpan.Zero;

        uint remainingArtworkSectors = totalAudioSectors < maxSectors
            ? maxSectors - totalAudioSectors
            : 0;

        double fractionUsed = Math.Clamp((double)totalAudioSectors / maxSectors, 0.0, 1.0);
        double audioDiscPct = fractionUsed * 100.0;
        double artworkDiscPct = (1.0 - fractionUsed) * 100.0;

        // Radius calculation based on track surface area: r^2 = r_in^2 + fraction * (r_out^2 - r_in^2)
        double audioEndRadius = Math.Sqrt((innerRadius * innerRadius) + (fractionUsed * ((outerRadius * outerRadius) - (innerRadius * innerRadius))));
        double artworkStartRadius = audioEndRadius;
        double artworkRadialWidth = Math.Max(0.0, outerRadius - artworkStartRadius);

        return new CdCapacityMetrics
        {
            TotalAudioDuration = totalAudioDuration,
            RemainingAudioDuration = remainingAudioDuration,
            MaxDiscDuration = maxDuration,
            TotalAudioSectors = totalAudioSectors,
            TotalDiscSectors = maxSectors,
            RemainingArtworkSectors = remainingArtworkSectors,
            AudioDiscPercentage = audioDiscPct,
            ArtworkDiscPercentage = artworkDiscPct,
            AudioStartRadiusMm = innerRadius,
            AudioEndRadiusMm = audioEndRadius,
            ArtworkStartRadiusMm = artworkStartRadius,
            ArtworkEndRadiusMm = outerRadius,
            ArtworkRadialWidthMm = artworkRadialWidth,
        };
    }

    public static void DecodeAndResampleToPcm(string filePath, Stream outputPcmStream)
    {
        using var reader = new MediaFoundationReader(filePath);
        var targetFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
        using var resampler = new MediaFoundationResampler(reader, targetFormat)
        {
            ResamplerQuality = 60 // High quality Sinc interpolation
        };

        IWaveProvider provider = resampler;
        byte[] buffer = new byte[BytesPerSector * 16];
        int bytesRead;
        long totalBytesWritten = 0;
        while ((bytesRead = provider.Read(buffer.AsSpan())) > 0)
        {
            outputPcmStream.Write(buffer, 0, bytesRead);
            totalBytesWritten += bytesRead;
        }

        // Pad with silence to match exact 2352-byte sector boundary
        int remainder = (int)(totalBytesWritten % BytesPerSector);
        if (remainder > 0)
        {
            int pad = BytesPerSector - remainder;
            byte[] silence = new byte[pad];
            outputPcmStream.Write(silence, 0, pad);
        }
    }
}
