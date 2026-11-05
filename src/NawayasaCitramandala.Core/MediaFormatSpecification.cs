namespace NawayasaCitramandala.Core;

public enum OpticalTrackStandard
{
    CdRedBook,
    DvdMinusR,
    DvdPlusR,
    BluRayBdrSingleLayer,
    BluRayBdrDualLayer,
    BluRayBdrTripleLayerXl
}

public enum LayerTrackingDirection
{
    OppositeTrackPathOtp, // Layer 0 spirals inward->outward, Layer 1 spirals outward->inward
    ParallelTrackPathPtp  // Both layers spiral inward->outward
}

/// <summary>
/// Detailed physical layer specifications across CD, DVD-R, DVD+R, and Blu-ray media formats.
/// </summary>
public sealed record MediaFormatSpecification(
    OpticalTrackStandard Standard,
    string DisplayName,
    int Layers,
    LayerTrackingDirection TrackingDirection,
    double NominalInnerRadiusMm,
    double NominalOuterRadiusMm,
    double TrackPitchMicrons,
    double ChannelBitLengthNm,
    long TotalUserSectors,
    int SectorPayloadBytes,
    string GrooveModulation)
{
    // CD-R 80-minute (700 MB)
    public static readonly MediaFormatSpecification Cd80Min = new(
        OpticalTrackStandard.CdRedBook, "CD-R 80min (700 MB)",
        1, LayerTrackingDirection.ParallelTrackPathPtp,
        24.0, 58.0, 1.60, 277.8,
        360000, 2352, "ATIP (Absolute Time In Pregroove) FM modulation");

    // CD-R 74-minute (650 MB)
    public static readonly MediaFormatSpecification Cd74Min = new(
        OpticalTrackStandard.CdRedBook, "CD-R 74min (650 MB)",
        1, LayerTrackingDirection.ParallelTrackPathPtp,
        25.0, 58.0, 1.60, 277.8,
        333000, 2352, "ATIP FM modulation");

    // DVD-R Single Layer (4.7 GB)
    public static readonly MediaFormatSpecification DvdRSl = new(
        OpticalTrackStandard.DvdMinusR, "DVD-R SL (4.7 GB)",
        1, LayerTrackingDirection.ParallelTrackPathPtp,
        24.0, 58.0, 0.74, 133.3,
        2295104, 2048, "LPP (Land Pre-Pit) modulation");

    // DVD-R Dual Layer (8.5 GB)
    public static readonly MediaFormatSpecification DvdRDl = new(
        OpticalTrackStandard.DvdMinusR, "DVD-R DL (8.54 GB)",
        2, LayerTrackingDirection.OppositeTrackPathOtp,
        24.0, 58.0, 0.74, 146.7,
        4171712, 2048, "LPP modulation with Layer 0->Layer 1 transition at LBA 2085856");

    // DVD+R Single Layer (4.7 GB)
    public static readonly MediaFormatSpecification DvdPlusRSl = new(
        OpticalTrackStandard.DvdPlusR, "DVD+R SL (4.7 GB)",
        1, LayerTrackingDirection.ParallelTrackPathPtp,
        24.0, 58.0, 0.74, 133.3,
        2295104, 2048, "ADIP (Address In Pre-groove) 817.4 kHz wobble with phase modulation");

    // DVD+R Dual Layer (8.5 GB)
    public static readonly MediaFormatSpecification DvdPlusRDl = new(
        OpticalTrackStandard.DvdPlusR, "DVD+R DL (8.55 GB)",
        2, LayerTrackingDirection.OppositeTrackPathOtp,
        24.0, 58.0, 0.74, 146.7,
        4173824, 2048, "ADIP wobble phase modulation with seamless OTP layer break");

    // BD-R Single Layer (25 GB)
    public static readonly MediaFormatSpecification BdRSl = new(
        OpticalTrackStandard.BluRayBdrSingleLayer, "BD-R SL (25.0 GB)",
        1, LayerTrackingDirection.ParallelTrackPathPtp,
        24.0, 58.0, 0.32, 74.5,
        12219392, 2048, "High-frequency wobbled groove with H-MSK (Harmonic Minimum Shift Keying)");

    // BD-R Dual Layer (50 GB)
    public static readonly MediaFormatSpecification BdRDl = new(
        OpticalTrackStandard.BluRayBdrDualLayer, "BD-R DL (50.0 GB)",
        2, LayerTrackingDirection.OppositeTrackPathOtp,
        24.0, 58.0, 0.32, 74.5,
        24438784, 2048, "H-MSK and STW (Saw-Tooth Wobble) with OTP Layer 1 return spiral");

    // BD-R BDXL Triple Layer (100 GB)
    public static readonly MediaFormatSpecification BdRXlTl = new(
        OpticalTrackStandard.BluRayBdrTripleLayerXl, "BD-R XL TL (100.0 GB)",
        3, LayerTrackingDirection.OppositeTrackPathOtp,
        24.0, 58.0, 0.32, 55.9,
        48877568, 2048, "Multi-stack thin spacer layers with high-sensitivity recording");

    public static MediaFormatSpecification GetSpecification(OpticalTrackStandard standard) => standard switch
    {
        OpticalTrackStandard.CdRedBook => Cd80Min,
        OpticalTrackStandard.DvdMinusR => DvdRSl,
        OpticalTrackStandard.DvdPlusR => DvdPlusRSl,
        OpticalTrackStandard.BluRayBdrSingleLayer => BdRSl,
        OpticalTrackStandard.BluRayBdrDualLayer => BdRDl,
        OpticalTrackStandard.BluRayBdrTripleLayerXl => BdRXlTl,
        _ => DvdRSl
    };
}
