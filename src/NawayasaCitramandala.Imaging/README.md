# NawayasaCitramandala.Imaging

High-resolution polar disc raster processing, canvas management, and spatial dithering algorithms.

## 1. Overview
The `NawayasaCitramandala.Imaging` module coordinates the transformation between Cartesian images and polar disc canvas coordinates. It contains:
- Canvas resolution profiles (`RingImageQuality`).
- Full multi-algorithm dithering engine (`ImageDitherProcessor`).
- Optical dye reflectance integration.

## 2. Key Components

### `RingImageQuality`
Defines standardized canvas resolutions for optical disc rendering:
- `Low`: $2048 \times 2048$ pixels (Fast preview, ~16 MB BGRA).
- `Standard`: $4096 \times 4096$ pixels (Production quality, ~64 MB BGRA).
- `High`: $8192 \times 8192$ pixels (Fine-line mastering, ~256 MB BGRA).

### `ImageDitherProcessor`
Implements advanced dithering strategies specifically calibrated for optical pit/land contrast:
1. **Blue Noise (Void-and-Cluster)**:
   - Uses the $64 \times 64$ blue noise threshold matrix from `NawayasaCitramandala.Core`.
   - Distributes quantization noise evenly across high spatial frequencies without directional bias.
2. **Floyd-Steinberg Error Diffusion**:
   - Classic $7/16, 3/16, 5/16, 1/16$ 2D error diffusion filter.
3. **Atkinson Error Diffusion**:
   - Bill Atkinson's high-contrast 1/8 error diffusion filter (preserves crisp edges in 1-bit line art).
4. **Bayer 4×4 Ordered Dithering**:
   - Fixed matrix ordered dithering for rhythmic halftone visual textures.

## 3. Usage Example

```csharp
using NawayasaCitramandala.Core;
using NawayasaCitramandala.ImageIO;
using NawayasaCitramandala.Imaging;

byte[] bgra = RasterCodec.DecodeBgraFromFile("input.png", out int width, out int height);

// Apply Blue Noise dithering with Azo dye gamma compensation
byte[] dithered = ImageDitherProcessor.Process(
    bgra, 
    width, 
    height, 
    DitherAlgorithm.BlueNoise, 
    DyeProfile.Azo
);

RasterCodec.SavePng(dithered, width, height, "output_bluenoise.png");
```
