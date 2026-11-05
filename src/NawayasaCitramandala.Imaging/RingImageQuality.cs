namespace NawayasaCitramandala.Imaging;

/// <summary>
/// Preset quality levels and resolution specifications for disc surface canvas rendering.
/// </summary>
public static class RingImageQuality
{
    /// <summary>Interactive preview resolution (2048 x 2048).</summary>
    public const int PreviewResolution = 2048;

    /// <summary>Balanced high quality rendering (4096 x 4096).</summary>
    public const int HighResolution = 4096;

    /// <summary>Production master export quality for streaming burn (8192 x 8192).</summary>
    public const int MasterResolution = 8192;

    /// <summary>Alias for MasterResolution for upstream compatibility.</summary>
    public const int GenerationSize = MasterResolution;

    /// <summary>Alias for PreviewResolution for upstream compatibility.</summary>
    public const int PreviewSize = PreviewResolution;
}
