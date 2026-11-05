namespace NawayasaCitramandala.Core;

/// <summary>
/// Physical optical disc chemical dye families.
/// Optical dyes exhibit varying laser absorption and reflectance properties at 780nm (CD), 650nm (DVD), and 405nm (BD).
/// </summary>
public enum OpticalDyeType
{
    StandardGeneric,
    AzoDarkBlue,        // Verbatim DataLifePlus / Mitsubishi Chemical AZO (deep blue, high visual contrast)
    CyanineGreen,       // Taiyo Yuden / classic cyanine (light cyan/green tint)
    PhthalocyanineGold, // Mitsui / MAM-A Gold (golden-greenish/silver, subtle optical contrast)
    InorganicBdrHtl,    // High-To-Low inorganic silicon/copper phase-change BD-R
    InorganicBdrLth     // Low-To-High organic BD-R
}

/// <summary>
/// Physical dye profile providing reflectance curves, optimal gamma, and recommended dither strength.
/// </summary>
public sealed record DyeCharacteristics(
    OpticalDyeType DyeType,
    string DisplayName,
    double GammaCorrection,
    double VisualContrastBoost,
    double PitReflectancePercent,
    double LandReflectancePercent)
{
    public static readonly DyeCharacteristics Generic = new(
        OpticalDyeType.StandardGeneric, "Generic / Auto", 1.0, 1.0, 15.0, 70.0);

    public static readonly DyeCharacteristics Azo = new(
        OpticalDyeType.AzoDarkBlue, "Mitsubishi AZO (Deep Blue)", 1.15, 1.35, 8.0, 68.0);

    public static readonly DyeCharacteristics Cyanine = new(
        OpticalDyeType.CyanineGreen, "Cyanine (Light Blue-Green)", 0.95, 1.10, 12.0, 65.0);

    public static readonly DyeCharacteristics Phthalocyanine = new(
        OpticalDyeType.PhthalocyanineGold, "Phthalocyanine (Gold / Silver)", 0.90, 1.25, 18.0, 75.0);

    public static readonly DyeCharacteristics BdHtl = new(
        OpticalDyeType.InorganicBdrHtl, "BD-R Inorganic HTL (Standard)", 1.05, 1.20, 10.0, 62.0);

    public static readonly DyeCharacteristics BdLth = new(
        OpticalDyeType.InorganicBdrLth, "BD-R Organic LTH", 1.10, 1.15, 14.0, 58.0);

    public static DyeCharacteristics FromType(OpticalDyeType dyeType) => dyeType switch
    {
        OpticalDyeType.AzoDarkBlue => Azo,
        OpticalDyeType.CyanineGreen => Cyanine,
        OpticalDyeType.PhthalocyanineGold => Phthalocyanine,
        OpticalDyeType.InorganicBdrHtl => BdHtl,
        OpticalDyeType.InorganicBdrLth => BdLth,
        _ => Generic
    };
}
