using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using NawayasaCitramandala.Core;
using NawayasaCitramandala.ImageIO;

namespace NawayasaCitramandala.Cd;

public sealed record CdHybridProgress(
    string CurrentStage,
    int TrackIndex,
    int TotalTracks,
    long CompletedBytes,
    long TotalBytes,
    double OverallFraction);

public sealed record CdHybridResult(
    string MasterWavePath,
    string CueSheetPath,
    IReadOnlyList<CdAudioTrack> AudioTracks,
    CdCapacityMetrics Metrics,
    long TotalAudioBytes,
    long TotalArtworkBytes,
    TimeSpan Elapsed);

[SupportedOSPlatform("windows")]
public static class CdAudioVisualHybridGenerator
{
    public const int BytesPerSector = 2352;
    public const int SectorsPerSecond = 75;

    public static CdHybridResult Generate(
        IReadOnlyList<string> audioFiles,
        string imagePath,
        string outputWavePath,
        CdDiscParameters parameters,
        ToneProfile? tone = null,
        bool interleave = true,
        Action<CdHybridProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioFiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputWavePath);

        Stopwatch sw = Stopwatch.StartNew();
        string masterFullPath = Path.GetFullPath(outputWavePath);
        string cueFullPath = Path.ChangeExtension(masterFullPath, ".cue");
        Directory.CreateDirectory(Path.GetDirectoryName(masterFullPath)!);

        // 1. Analyze all audio tracks
        List<CdAudioTrack> analyzedTracks = [];
        for (int i = 0; i < audioFiles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            analyzedTracks.Add(CdAudioDecoder.AnalyzeTrack(audioFiles[i], i + 1));
        }

        // 2. Compute disc capacity & artwork radial zone
        CdCapacityMetrics metrics = CdAudioDecoder.CalculateCapacity(analyzedTracks, parameters);
        if (metrics.ExceedsCapacity)
        {
            throw new InvalidOperationException(
                $"Audio files exceed total disc capacity. Total audio duration is {metrics.TotalAudioDuration.TotalMinutes:F1} minutes, while standard CD capacity is {metrics.MaxDiscDuration.TotalMinutes:F1} minutes.");
        }

        // 3. Prepare master WAV file
        long totalAudioBytes = (long)metrics.TotalAudioSectors * BytesPerSector;
        long totalArtworkBytes = (long)metrics.RemainingArtworkSectors * BytesPerSector;
        long totalDataBytes = totalAudioBytes + totalArtworkBytes;

        using FileStream masterFile = new(
            masterFullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 1024,
            FileOptions.SequentialScan);

        // Write master WAV header
        CdWaveFile.WriteHeader(masterFile, totalDataBytes);

        // 4. Decode & Stream Audio Tracks
        StringBuilder cueBuilder = new();
        cueBuilder.AppendLine($"FILE \"{Path.GetFileName(masterFullPath)}\" WAVE");

        long currentSectorOffset = 0;
        for (int i = 0; i < analyzedTracks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CdAudioTrack track = analyzedTracks[i];
            progress?.Invoke(new CdHybridProgress(
                $"Decoding audio track {i + 1}/{analyzedTracks.Count}: {track.Title}",
                i + 1,
                analyzedTracks.Count + 1,
                masterFile.Position,
                totalDataBytes,
                (double)masterFile.Position / totalDataBytes));

            long trackStartSector = currentSectorOffset;
            long trackStartSecond = trackStartSector / SectorsPerSecond;
            long trackStartRemSectors = trackStartSector % SectorsPerSecond;
            long trackStartMinute = trackStartSecond / 60;
            long trackStartRemSecond = trackStartSecond % 60;

            cueBuilder.AppendLine($"  TRACK {track.TrackNumber:D2} AUDIO");
            cueBuilder.AppendLine($"    TITLE \"{track.Title.Replace("\"", "\\\"")}\"");
            cueBuilder.AppendLine($"    INDEX 01 {trackStartMinute:D2}:{trackStartRemSecond:D2}:{trackStartRemSectors:D2}");

            // Decode this track directly into the master WAV stream
            long bytesBefore = masterFile.Position;
            CdAudioDecoder.DecodeAndResampleToPcm(track.FilePath, masterFile);
            long bytesWrittenForTrack = masterFile.Position - bytesBefore;
            currentSectorOffset += bytesWrittenForTrack / BytesPerSector;
        }

        // 5. Generate & Stream Visual Artwork on Remaining Outer Sectors
        if (metrics.RemainingArtworkSectors > 0 && File.Exists(imagePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Invoke(new CdHybridProgress(
                $"Synthesizing visual surface artwork on remaining outer ring ({metrics.ArtworkStartRadiusMm:F1}–{metrics.ArtworkEndRadiusMm:F1} mm)...",
                analyzedTracks.Count + 1,
                analyzedTracks.Count + 1,
                masterFile.Position,
                totalDataBytes,
                (double)masterFile.Position / totalDataBytes));

            int visualTrackNum = analyzedTracks.Count + 1;
            long visStartSector = currentSectorOffset;
            long visStartSecond = visStartSector / SectorsPerSecond;
            long visStartRemSectors = visStartSector % SectorsPerSecond;
            long visStartMinute = visStartSecond / 60;
            long visStartRemSecond = visStartSecond % 60;

            cueBuilder.AppendLine($"  TRACK {visualTrackNum:D2} AUDIO");
            cueBuilder.AppendLine($"    TITLE \"Surface Visual Artwork\"");
            cueBuilder.AppendLine($"    INDEX 01 {visStartMinute:D2}:{visStartRemSecond:D2}:{visStartRemSectors:D2}");

            // Configure parameters for visual artwork outer ring
            CdDiscParameters artworkParams = new(
                metrics.ArtworkStartRadiusMm,
                metrics.ArtworkEndRadiusMm,
                (int)metrics.RemainingArtworkSectors,
                parameters.LinearVelocityMmPerSecond,
                parameters.StartAngleRadians,
                parameters.ImageOuterRadiusMm,
                parameters.PitchLinear,
                parameters.PitchQuadratic,
                parameters.PitchCubic);

            // Generate visual artwork PCM directly to stream
            CdTrackGenerator.GenerateToStream(
                imagePath,
                masterFile,
                artworkParams,
                interleave,
                p =>
                {
                    progress?.Invoke(new CdHybridProgress(
                        $"Encoding visual artwork sectors ({p.CompletedBytes * 100 / p.TotalBytes}%)...",
                        visualTrackNum,
                        visualTrackNum,
                        totalAudioBytes + p.CompletedBytes,
                        totalDataBytes,
                        (double)(totalAudioBytes + p.CompletedBytes) / totalDataBytes));
                },
                cancellationToken,
                masterFullPath,
                CdAudioByteOrder.LittleEndian,
                (tone ?? ToneProfile.LegacyBinary).Normalized());
        }

        // 6. Write CUE sheet
        File.WriteAllText(cueFullPath, cueBuilder.ToString(), new UTF8Encoding(false));

        sw.Stop();
        return new CdHybridResult(
            masterFullPath,
            cueFullPath,
            analyzedTracks,
            metrics,
            totalAudioBytes,
            totalArtworkBytes,
            sw.Elapsed);
    }

    public static void GenerateToStream(
        IReadOnlyList<string> audioFiles,
        string imagePath,
        Stream outputStream,
        CdDiscParameters parameters,
        ToneProfile? tone = null,
        bool interleave = true,
        Action<CdHybridProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioFiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        ArgumentNullException.ThrowIfNull(outputStream);

        // 1. Analyze all audio tracks
        List<CdAudioTrack> analyzedTracks = [];
        for (int i = 0; i < audioFiles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            analyzedTracks.Add(CdAudioDecoder.AnalyzeTrack(audioFiles[i], i + 1));
        }

        // 2. Compute disc capacity & artwork radial zone
        CdCapacityMetrics metrics = CdAudioDecoder.CalculateCapacity(analyzedTracks, parameters);
        if (metrics.ExceedsCapacity)
        {
            throw new InvalidOperationException(
                $"Audio files exceed total disc capacity. Total audio duration is {metrics.TotalAudioDuration.TotalMinutes:F1} minutes, while standard CD capacity is {metrics.MaxDiscDuration.TotalMinutes:F1} minutes.");
        }

        long totalAudioBytes = (long)metrics.TotalAudioSectors * BytesPerSector;
        long totalArtworkBytes = (long)metrics.RemainingArtworkSectors * BytesPerSector;
        long totalDataBytes = totalAudioBytes + totalArtworkBytes;

        // 3. Stream Audio Tracks
        long currentBytesWritten = 0;
        for (int i = 0; i < analyzedTracks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CdAudioTrack track = analyzedTracks[i];
            progress?.Invoke(new CdHybridProgress(
                $"Streaming audio track {i + 1}/{analyzedTracks.Count}: {track.Title}",
                i + 1,
                analyzedTracks.Count + 1,
                currentBytesWritten,
                totalDataBytes,
                (double)currentBytesWritten / totalDataBytes));

            long trackBytesWritten = 0;
            using var counter = new TrackByteCounterStream(outputStream, c => trackBytesWritten += c);
            CdAudioDecoder.DecodeAndResampleToPcm(track.FilePath, counter);
            currentBytesWritten += trackBytesWritten;
        }

        // 4. Generate & Stream Visual Artwork on Remaining Outer Sectors
        if (metrics.RemainingArtworkSectors > 0 && File.Exists(imagePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Invoke(new CdHybridProgress(
                $"Synthesizing visual surface artwork on remaining outer ring ({metrics.ArtworkStartRadiusMm:F1}–{metrics.ArtworkEndRadiusMm:F1} mm)...",
                analyzedTracks.Count + 1,
                analyzedTracks.Count + 1,
                currentBytesWritten,
                totalDataBytes,
                (double)currentBytesWritten / totalDataBytes));

            CdDiscParameters artworkParams = new(
                metrics.ArtworkStartRadiusMm,
                metrics.ArtworkEndRadiusMm,
                (int)metrics.RemainingArtworkSectors,
                parameters.LinearVelocityMmPerSecond,
                parameters.StartAngleRadians,
                parameters.ImageOuterRadiusMm,
                parameters.PitchLinear,
                parameters.PitchQuadratic,
                parameters.PitchCubic);

            CdTrackGenerator.GenerateToStream(
                imagePath,
                outputStream,
                artworkParams,
                interleave,
                p =>
                {
                    progress?.Invoke(new CdHybridProgress(
                        $"Encoding visual artwork sectors ({p.CompletedBytes * 100 / p.TotalBytes}%)...",
                        analyzedTracks.Count + 1,
                        analyzedTracks.Count + 1,
                        totalAudioBytes + p.CompletedBytes,
                        totalDataBytes,
                        (double)(totalAudioBytes + p.CompletedBytes) / totalDataBytes));
                },
                cancellationToken,
                "hybrid-direct-stream",
                CdAudioByteOrder.LittleEndian,
                (tone ?? ToneProfile.LegacyBinary).Normalized());
        }
    }

    private sealed class TrackByteCounterStream(Stream inner, Action<int> onBytesWritten) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
            onBytesWritten(count);
        }
    }
}
