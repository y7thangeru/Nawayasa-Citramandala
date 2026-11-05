# NawayasaCitramandala.Core

Core mathematical foundations, domain models, contracts, and optical physics calculations for **Nawayasa Citramandala**.

## 1. Overview
The `NawayasaCitramandala.Core` module provides the shared types and mathematical infrastructure used across all media formats (CD, DVD, Blu-ray). It contains:
- Common abstractions for disc modules (`IOpticalDiscModule`).
- Archimedean spiral and 3rd-order track pitch polynomial geometry (`DvdTrackGeometry`).
- Void-and-Cluster Blue Noise spatial dithering matrix generator (`BlueNoiseGrayscale`).
- Chemical dye reflectance profiling (`DyeProfile`).
- Multi-level quantization profiles (`ToneProfile`, `BinaryGrayscale`, `ThreeLevelGrayscale`, `FourLevelGrayscale`).
- Project and Preset JSON persistence stores (`ProjectJsonStore`, `DiscPresetJsonStore`).

## 2. Key Types & Architecture

### `DvdTrackGeometry`
Calculates high-precision spiral mappings between physical radius $r$ (in millimeters), angle $\theta$ (in radians), and Logical Block Address (LBA):
- Evaluates the 3rd-order polynomial pitch function:
  $$p(x) = p_0 \cdot \left(1 + c_1 x + c_2 x^2 + c_3 x^3\right)$$
- Computes analytical spiral arc lengths to ensure continuous sector alignment.

### `BlueNoiseGrayscale`
Generates a $64 \times 64$ toroidal Void-and-Cluster blue-noise threshold matrix. Blue noise concentrates quantization error into high spatial frequencies where human visual sensitivity is lowest, completely eliminating diagonal spiral "worm" artifacts found in conventional Floyd-Steinberg error diffusion.

### `DyeProfile`
Pre-compensates for non-linear optical reflectance curves across disc dye chemistries:
- `Azo`: Dark blue dye (e.g. Verbatim DataLifePlus), $\gamma \approx 1.22$.
- `Phthalocyanine`: Gold/silver CD-R, $\gamma \approx 0.95$.
- `Cyanine`: Traditional emerald green CD-R, $\gamma \approx 1.08$.
- `BdHtl`: High-to-Low inorganic phase-change alloy (BD-R), $\gamma \approx 1.40$.
- `BdLth`: Low-to-High organic dye (BD-R), $\gamma \approx 1.15$.

### `IOpticalDiscModule`
The unified interface implemented by all media modules (`CdDiscModule`, `DvdDiscModule`, `BluRayDiscModule`):
```csharp
public interface IOpticalDiscModule
{
    string Id { get; }
    string DisplayName { get; }
    Task<DiscJobResult> GenerateDiscJobAsync(DiscJobRequest request, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePreviewAsync(DiscJobRequest request, CancellationToken cancellationToken = default);
}
```

## 3. Dependencies
- Pure .NET 9 LTS/STS runtime.
- System.Text.Json (for preset and configuration serialization).
- Zero external native dependencies.
