using NawayasaCitramandala.Core;
using NawayasaCitramandala.Dvd.Engine.Encoding;
using Xunit;

namespace NawayasaCitramandala.Tests;

public class DvdPhysicsTests
{
    [Fact]
    public void DvdEccBlock_ChannelBitCount_MatchesStandard()
    {
        // ECMA-267 and ECMA-359 define:
        // 13 sync frames * 2 * (32 sync bits + 1456 data channel bits) = 38,688 bits per sector
        // 16 sectors per ECC block = 619,008 bits
        Assert.Equal(38688, DvdEccBlockEncoder.ChannelBitsPerSector);
        Assert.Equal(619008, DvdEccBlockEncoder.ChannelBitsPerSector * DvdEccBlockEncoder.SectorCount);
    }

    [Fact]
    public void GaloisField256_Operations_AreConsistent()
    {
        // Multiplicative identity in GF(2^8)
        for (int a = 0; a < 256; a++)
        {
            Assert.Equal((byte)a, GaloisField256.Multiply((byte)a, 1));
            Assert.Equal(0, GaloisField256.Multiply((byte)a, 0));
        }
    }

    [Fact]
    public void TrackGeometry_CubicPitch_BuildsValidSpiral()
    {
        // Create 3rd-order track geometry with valid pitch coefficients
        DvdTrackGeometry geom = DvdTrackGeometry.Create(
            innerRadiusMm: 24.0,
            outerRadiusMm: 58.0,
            trackLengthMm: 38688.0 * 2295104 * 133.3e-6,
            pitchLinear: 0.02,
            pitchQuadratic: -0.01,
            pitchCubic: 0.005);

        Assert.Equal(24.0, geom.InnerRadiusMm);
        Assert.Equal(58.0, geom.OuterRadiusMm);
        Assert.True(geom.TotalAngleRadians > 0);
    }
}
