# NawayasaCitramandala.Cli

Unified Command Line Interface for **Nawayasa Citramandala**.

## 1. Overview
The `NawayasaCitramandala.Cli` project provides a comprehensive command-line tool for headless rendering, batch automation, image preprocessing, and disc image compilation.

## 2. Command Index

| Command | Subsystem | Description |
| :--- | :--- | :--- |
| `autocalibrate` | Vision | Auto-detects spindle center, radii, and pitch coefficients from a disc photo. |
| `dither` | Imaging | Pre-processes artwork using Blue Noise, Floyd-Steinberg, Atkinson, or Bayer dithering with dye gamma compensation. |
| `cd-generate` | CD | Generates CD-DA audio (WAV + CUE) with CIRC latency compensation. |
| `solve` | DVD | Executes reverse-constraint solver for DVD-R EFMPlus visual sectors. |
| `bd-generate` | Blu-ray | Generates BD-R 25GB ISO image with 17PP RLL(1,7) & DC control bits. |

## 3. Quickstart Examples

### Auto-Calibrate from Photo
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- autocalibrate --photo disc_photo.jpg --out my_preset.json
```

### Pre-process Artwork with Blue Noise
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- dither --input art.png --output dithered.png --method bluenoise --dye azo
```

### Generate Visual DVD-R ISO
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- solve --image dithered.png --out visual_disc.iso --preset-file my_preset.json
```

### Generate CD-DA Audio Track
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- cd-generate --image dithered.png --out cd_art.wav
```

### Generate BD-R 25GB Image
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- bd-generate --image dithered.png --out visual_bluray.iso
```

For full option details and exit codes, see [CLI Reference Guide](../../docs/CLI_REFERENCE.md).
