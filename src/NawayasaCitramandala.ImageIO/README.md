# NawayasaCitramandala.ImageIO

Cross-platform raw image decoding, encoding, and pixel manipulation engine for **Nawayasa Citramandala**.

## 1. Overview
The `NawayasaCitramandala.ImageIO` package decouples Nawayasa Citramandala from legacy Windows-only imaging frameworks (such as `System.Windows.Media` and Windows Imaging Component / WIC `BitmapSource`). It provides fast, zero-dependency raster loading and saving across Windows, Linux, and macOS.

## 2. Key Components

### `RasterCodec`
The primary utility class offering static decoding and encoding:
- `DecodeBgra(byte[] encodedData, out int width, out int height)`: Decodes JPEG, PNG, BMP, or TGA bytes into a 32-bit BGRA raw byte array.
- `DecodeBgraFromFile(string filePath, out int width, out int height)`: Streamlines reading directly from disk.
- `EncodePng(byte[] bgraPixels, int width, int height)`: Encodes a 32-bit BGRA buffer to an uncompressed or compressed PNG byte array.
- `SavePng(byte[] bgraPixels, int width, int height, string destinationPath)`: Saves directly to disk.

### Memory Layout
All pixel buffers are represented as contiguous 32-bit BGRA (Blue, Green, Red, Alpha) byte arrays:
- Stride: $4 \times \text{width}$ bytes.
- Size: $4 \times \text{width} \times \text{height}$ bytes.
- Compatible with direct pointer blitting, SIMD vectorized processing, and OpenGL/Vulkan/DirectX texture uploads.

## 3. Dependencies
- `StbImageSharp` (2.30.15+) — Managed C# port of Sean Barrett's stb_image.
- `StbImageWriteSharp` (1.16.7+) — Managed C# port of stb_image_write.
- Zero platform-specific native C/C++ binaries required.
