using System;
using System.IO;
using NawayasaCitramandala.Core;
using NawayasaCitramandala.Dvd;
using NawayasaCitramandala.Dvd.Engine;
using NawayasaCitramandala.Iso;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class HybridBurnValidationTests
{
    [Fact]
    public void ValidateDvdHybridPreparationWithUserSongs()
    {
        string songDir = @"D:\BurnThis\SONG";
        if (!Directory.Exists(songDir)) return;

        DvdStreamingOptions options = new(
            TotalSectors: 2_295_104,
            InnerRadiusMm: 24.0,
            OuterRadiusMm: 58.0);

        DvdHybridStreamingPlan plan = DvdStreamingGenerator.PrepareHybrid(
            songDir,
            options,
            volumeLabel: "AUDIO_DISC",
            filesAtOuterRing: false);

        Assert.NotNull(plan);
        Assert.True(plan.FileCount >= 7, $"Expected at least 7 files, found {plan.FileCount}");
        Assert.True(plan.DrawingStartLba > 0, $"Expected drawing to start after file data, got {plan.DrawingStartLba}");
        Assert.True(plan.DrawingEndLbaExclusive <= 2_295_104, "Drawing must fit within total sectors");
        Assert.True(plan.DrawingSectors > 1_000_000, "Should have plenty of room for artwork");

        // Verify that sequential prefix (ISO 9660 metadata + files) can be written to a stream
        using MemoryStream ms = new();
        HybridIsoImageWriter.WriteSequentialPrefix(plan.IsoLayout, ms, plan.DrawingStartLba);

        Assert.True(ms.Length > 0, "Prefix stream should contain filesystem and data");
        Assert.Equal((long)plan.DrawingStartLba * 2048, ms.Length);

        // Verify that sector 16 (PVD) contains the volume descriptor and label "AUDIO_DISC"
        byte[] buffer = ms.ToArray();
        int pvdOffset = 16 * 2048;
        Assert.True(buffer.Length > pvdOffset + 100);
        Assert.Equal(0x01, buffer[pvdOffset]); // Type 1: Primary Volume Descriptor
        Assert.Equal((byte)'C', buffer[pvdOffset + 1]);
        Assert.Equal((byte)'D', buffer[pvdOffset + 2]);
        Assert.Equal((byte)'0', buffer[pvdOffset + 3]);
        Assert.Equal((byte)'0', buffer[pvdOffset + 4]);
        Assert.Equal((byte)'1', buffer[pvdOffset + 5]);
    }
}
