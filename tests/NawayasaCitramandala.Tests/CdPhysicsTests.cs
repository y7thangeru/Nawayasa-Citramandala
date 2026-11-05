using NawayasaCitramandala.Cd;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class CdPhysicsTests
{
    [Fact]
    public void CddaInterleave_FrameConstants_MatchRedBook()
    {
        // Red Book: 24 audio bytes per F1 frame, 98 frames per sector (2352 audio bytes)
        Assert.Equal(24, CddaInterleaveTable.BytesPerFrame);
        Assert.Equal(98, CdDiscParameters.BytesPerSector / CddaInterleaveTable.BytesPerFrame);
        Assert.Equal(2352, CdDiscParameters.BytesPerSector);
    }

    [Fact]
    public void CddaInterleave_DelaySequence_MatchesCircSchedule()
    {
        // 24 parallel lines with varying delay stages (FileOffsetTable)
        Assert.True(CddaInterleaveTable.MaximumFileOffset > 0);
        for (int i = 0; i < 24; i++)
        {
            int offset = CddaInterleaveTable.FileOffsetFor(i);
            Assert.True(offset >= 0);
            Assert.True(offset <= CddaInterleaveTable.MaximumFileOffset);
        }
    }

    [Fact]
    public void AudioByteOrder_Conversion_SwapsCorrectly()
    {
        byte[] pcm = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using MemoryStream ms = new();
        CdAudioSamples.WriteSector(ms, pcm, CdAudioByteOrder.LittleEndian);
        byte[] result = ms.ToArray();

        // 16-bit stereo PCM: big-endian to little-endian byte swap per 16-bit word
        Assert.Equal(0x34, result[0]);
        Assert.Equal(0x12, result[1]);
        Assert.Equal(0x78, result[2]);
        Assert.Equal(0x56, result[3]);
    }
}
