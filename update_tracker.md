# Update Tracker - Nawayasa Citramandala (DiscImageStudio Spinoff)

Dokumen ini mencatat setiap perubahan, penambahan, refaktor, dan perbaikan dalam siklus pengembangan proyek Nawayasa Citramandala DiscImageStudio.

---

### [2026-11-05 10:25:00 +07:00] - Inisialisasi Proyek & Audit Mendalam Upstream Repo
- **Action Type**: `[NEW]`
- **File Paths**:
  - `update_tracker.md`
  - `Feature-ToBe-Implemented.md`
  - `README.md`
  - `Report-To-GeminiProject.md`
- **Reasoning**:
  - Melakukan clone dan audit mendalam terhadap repositori upstream `https://github.com/JiaFeiMiao-K-Cat/DiscImageStudio` (commit release v1.4.0, ~6,700 stars).
  - Menginisialisasi fondasi tata kelola proyek (Project Governance) sesuai Antigravity Global Rules.
  - Memetakan arsitektur upstream (Core, Cd, Dvd, DvdImageSolver, BluRay, Iso, Burning, Imaging, ImageIO, App, Cli, Tools).
  - Menyusun roadmap fitur spinoff untuk peningkatan kapabilitas (cross-platform UI, computer vision auto-calibration, extended optical media support, modern dithering, direct MMC/SCSI streaming).

### [2026-11-05 11:00:00 +07:00] - Implementasi Spinoff Engine & Arsitektur Multi-Target .NET 9
- **Action Type**: `[NEW]` / `[REFACTOR]`
- **File Paths**:
  - `Directory.Build.props`
  - `NawayasaCitramandala.sln`
  - `src/NawayasaCitramandala.Core/` (Pure C# .NET 9 Core Engine, Presets, DvdTrackGeometry, ToneProfile)
  - `src/NawayasaCitramandala.Core/BlueNoiseGrayscale.cs` (Void-and-Cluster 64x64 Blue Noise Dither)
  - `src/NawayasaCitramandala.Core/DyeProfile.cs` (Optical Chemical Dye Profile: AZO, Cyanine, Phthalocyanine, BD-R HTL/LTH)
  - `src/NawayasaCitramandala.ImageIO/` (Cross-platform StbImageSharp / StbImageWriteSharp BGRA codec)
  - `src/NawayasaCitramandala.Imaging/` (Cross-platform Canvas, RingImageQuality, ImageDitherProcessor)
  - `src/NawayasaCitramandala.Iso/` (ISO9660 & UDF 2.01 filesystem builders)
  - `src/NawayasaCitramandala.Cd/` (Red Book CD-DA PCM, CIRC 111-frame latency interleaver, WAV/CUE, Mode 1)
  - `src/NawayasaCitramandala.Dvd/` (ECMA-267/359 RS-PC PO/PI, LFSR scrambler, EFMPlus 4-state automaton, dispersion Q=0xA5/0x92)
  - `src/NawayasaCitramandala.BluRay/` (ISO/IEC 30190 17PP, DC control, LDC/BIS, 3T/6T run-length bias)
  - `src/NawayasaCitramandala.Burning/` (Windows IMAPI2 streaming burner with 64MB bounded ring buffer)
  - `src/NawayasaCitramandala.Vision/` (Computer Vision auto-calibration: center & radius Hough detection, 3rd-order pitch polynomial regression)
  - `src/NawayasaCitramandala.Cli/` (Cross-platform CLI tool with `solve`, `cd-generate`, `bd-generate`, `dither`, and `autocalibrate`)
  - `tests/NawayasaCitramandala.Tests/` (11 unit tests covering DVD RS-PC/EFMPlus, CD CIRC/PCM, Blu-ray 17PP, Blue Noise, and Vision Calibrator)
- **Reasoning**:
  - Menyelesaikan pemisahan arsitektur dari monopoli .NET 10 ke .NET 9 LTS/STS yang terpasang di sistem.
  - Membangun 100% test-passing verification suite.
  - Menghadirkan fitur baru yang belum ada di upstream: Blue Noise Void-and-Cluster dithering, profil kimia dye disc (AZO, Cyanine, Phthalocyanine), dan modul Computer Vision otomatis untuk deteksi fisik piringan dan kalkulasi pitch $c_1, c_2, c_3$.
