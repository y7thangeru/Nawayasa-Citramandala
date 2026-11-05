# CLI Reference Guide — Nawayasa Citramandala

`NawayasaCitramandala.Cli` is the unified command-line interface for batch processing, image dithering, auto-calibration, and ISO/WAV disc generation.

---

## 1. Global Syntax

```bash
dotnet run --project src/NawayasaCitramandala.Cli -- <command> [options]
```
Or, if compiled:
```bash
./NawayasaCitramandala.Cli <command> [options]
```

To display help and available commands:
```bash
NawayasaCitramandala.Cli --help
```

---

## 2. Command Index

| Command | Purpose | Target Media |
| :--- | :--- | :--- |
| `autocalibrate` | Computer Vision auto-detection of spindle, radii, and pitch coefficients | All |
| `dither` | Pre-process image with Blue Noise / error diffusion algorithms | All |
| `cd-generate` | Synthesize CD-DA audio (WAV/CUE) with CIRC latency compensation | CD-R |
| `solve` | Generate DVD-R physical sector payload with EFMPlus dispersion | DVD-R |
| `bd-generate` | Generate BD-R 25GB image with 17PP RLL(1,7) & DC control bits | BD-R |

---

## 3. Subcommand Specifications

### 3.1 `autocalibrate`
Analyzes a photograph or scan of a burned disc to estimate geometric parameters.

```bash
NawayasaCitramandala.Cli autocalibrate --photo <path> [options]
```

**Options:**
- `--photo <path>` (Required): Path to input disc photograph (PNG, JPEG).
- `--preset <name>` (Optional): Baseline preset to refine (e.g. `DVD_SL_4700MB`, `CD_700MB`, `BD_25GB`). Default: `DVD_SL_4700MB`.
- `--out <path>` (Optional): Path to write the calibrated JSON preset. Default: `calibrated_preset.json`.
- `--debug-image <path>` (Optional): Save overlay image highlighting detected circles and radial lines.

**Example:**
```bash
NawayasaCitramandala.Cli autocalibrate --photo samples/disc_photo.jpg --out presets/my_dvd_preset.json
```

---

### 3.2 `dither`
Pre-processes an input image with spatial dithering optimized for optical dye reflection.

```bash
NawayasaCitramandala.Cli dither --input <path> --output <path> [options]
```

**Options:**
- `--input <path>` (Required): Input color or grayscale image.
- `--output <path>` (Required): Output 1-bit or 2-bit dithered image.
- `--method <type>`: Dithering algorithm.
  - `bluenoise` (Default, recommended Void-and-Cluster 64×64 torus).
  - `floydsteinberg` (Standard error diffusion).
  - `atkinson` (Mac-style crisp contrast).
  - `bayer` (Ordered 4×4 matrix).
- `--dye <type>`: Optical dye absorption gamma compensation.
  - `azo` (Verbatim Dark Blue)
  - `phthalocyanine` (Silver/Gold CD-R)
  - `cyanine` (Light Green)
  - `bd_htl` (Inorganic BD-R)
  - `bd_lth` (Organic BD-R)
- `--quality <level>`: Canvas resolution (`low` = 2048px, `standard` = 4096px, `high` = 8192px).

**Example:**
```bash
NawayasaCitramandala.Cli dither --input artwork.png --output artwork_bluenoise.png --method bluenoise --dye azo
```

---

### 3.3 `cd-generate`
Generates a Compact Disc Digital Audio (CD-DA) WAV and CUE sheet with CIRC latency inversion.

```bash
NawayasaCitramandala.Cli cd-generate --image <path> --out <path> [options]
```

**Options:**
- `--image <path>` (Required): Dithered input image.
- `--out <path>` (Required): Destination output path for `.wav` (corresponding `.cue` is generated alongside).
- `--preset <name>`: CD preset name (e.g. `CD_700MB`, `CD_800MB`).
- `--inner-radius <mm>`: Inner image boundary (Default: `24.0`).
- `--outer-radius <mm>`: Outer image boundary (Default: `57.5`).

**Example:**
```bash
NawayasaCitramandala.Cli cd-generate --image album_art.png --out build/album_art.wav
```

---

### 3.4 `solve` (DVD-R Generator)
Executes the reverse constraint solver to generate an ECMA-267/359 DVD-R ISO image.

```bash
NawayasaCitramandala.Cli solve --image <path> --out <path> [options]
```

**Options:**
- `--image <path>` (Required): Dithered input image.
- `--out <path>` (Required): Destination ISO image path.
- `--preset-file <path>`: Custom calibrated JSON preset.
- `--inner-data <path>`: Real data files/folder to pack into the inner hub track.
- `--outer-data <path>`: Real data files/folder to pack into the outer perimeter track.

**Example:**
```bash
NawayasaCitramandala.Cli solve --image poster.png --out my_visual_dvd.iso --preset-file presets/my_dvd_preset.json
```

---

### 3.5 `bd-generate`
Generates an ISO/IEC 30190 compliant BD-R 25GB image with 17PP RLL(1,7) parity preserve modulation and DC control bits.

```bash
NawayasaCitramandala.Cli bd-generate --image <path> --out <path> [options]
```

**Options:**
- `--image <path>` (Required): Dithered input image.
- `--out <path>` (Required): Destination UDF/ISO image path.
- `--inner-radius <mm>`: Inner boundary (Default: `24.0`).
- `--outer-radius <mm>`: Outer boundary (Default: `58.0`).

**Example:**
```bash
NawayasaCitramandala.Cli bd-generate --image hires_mandala.png --out mandala_bluray.iso
```

---

## 4. Exit Codes

- `0`: Success.
- `1`: Invalid command line arguments.
- `2`: File or resource not found.
- `3`: Computer Vision detection failed or confidence too low.
- `4`: Mathematical constraint solver or ECC generation failure.
- `5`: Streaming I/O or burn failure.
