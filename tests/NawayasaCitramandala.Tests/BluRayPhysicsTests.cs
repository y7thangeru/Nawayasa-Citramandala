using NawayasaCitramandala.BluRay;
using NawayasaCitramandala.BluRay.Encoding;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class BluRayPhysicsTests
{
    [Fact]
    public void BluRay_RubConstants_MatchIso30190()
    {
        // 496 frames per Recording Unit Block (RUB), 1932 channel bits per frame = 958,272 channel bits
        Assert.Equal(1932, BluRayRecordingFrameEncoder.ChannelBitsPerFrame);
        Assert.Equal(496 * 1932, BluRayRecordingUnitEncoder.PhysicalClusterChannelBits);
    }

    [Fact]
    public void BluRay17Pp_ModulationTable_ProducesValidRunLengths()
    {
        // 17PP produces channel runs strictly between 2T and 8T (minimum run 2T, so no adjacent 1s)
        byte[] inputBits = new byte[] { 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 0 };
        var result = BluRay17PpEncoder.Encode(inputBits);

        // Check RLL constraints: no consecutive ones (minimum run is 2T)
        for (int i = 0; i < result.ModulationBits.Length - 1; i++)
        {
            if (result.ModulationBits[i] == 1 && result.ModulationBits[i + 1] == 1)
                Assert.Fail($"17PP violation: adjacent 1s found at position {i}. Min run is 2T.");
        }
    }
}
