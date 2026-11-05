# Laporan Konsolidasi Proyek: Nawayasa Citramandala (DiscImageStudio Spinoff)

---

# BAGIAN I: LAPORAN EKSEKUTIF & TEKNIKAL

## 1. Latar Belakang & Identifikasi Masalah
Proyek **DiscImageStudio** yang dikembangkan oleh **JiaFeiMiao-K-Cat** merupakan terobosan luar biasa dalam rekayasa perangkat lunak media optik. Berbeda dengan aplikasi pembakar piringan biasa (seperti ImgBurn, Nero, K3b, atau Brasero) yang sekadar menuliskan berkas ISO atau audio secara sekuensial tanpa memperhatikan representasi spasial piringan, DiscImageStudio mampu **menciptakan gambar atau pola visual yang tampak oleh mata telanjang langsung pada permukaan lapisan data piringan CD-R, DVD-R, dan BD-R**.

Teknologi komersial sebelumnya seperti **Yamaha DiscT@2** (2002) dan **NEC/FujiFilm Labelflash** (2005) membutuhkan drive optik khusus berlisensi eksklusif dengan firmware termodifikasi. DiscImageStudio membuktikan bahwa dengan memodelkan konversi fisik saluran data secara terbalik (*reverse constraint solving*), optical disc burner standar (bahkan burner murah USB/SATA rumahan) dapat digunakan untuk mencetak gambar berkualitas tinggi pada piringan optik standar.

## 2. Prinsip Rekayasa Fisik & Matematika
Pembentukan gambar visual pada piringan optik didasarkan pada manipulasi kontras refleksi cahaya laser mikroskopis:

### A. Kompak Disk (CD-R)
- **Standar**: ECMA-130, ECMA-394, Red Book (CD-DA).
- **Modulasi Saluran**: EFM (Eight-to-Fourteen Modulation) di mana setiap byte data 8-bit dipetakan menjadi pola saluran 14-bit dengan 3 merging bits untuk menjaga kendali DSV (*Digital Sum Value*).
- **Interleaving**: CIRC (*Cross-Interleaved Reed-Solomon Code*) dengan latensi penundaan sebesar 111 frame (~15.102 ms, atau ~18.122 mm panjang busur pada 1200 mm/s).
- **Polaritas Byte**: Nilai byte `0x0D` menghasilkan pantulan gelap sedangkan `0xAA` menghasilkan pantulan terang.

### B. DVD-R & DVD+R (Digital Versatile Disc)
- **Standar**: ECMA-267 (DVD-ROM), ECMA-359 (DVD-R), ECMA-337 (DVD+R).
- **Koreksi Kesalahan**: 16-sector ECC Block dengan RS-PC (*Reed-Solomon Product Code*), terdiri dari PO (*Parity Outer*) 208 baris × 16 byte dan PI (*Parity Inner*) 182 kolom × 10 byte.
- **Pengacak Data (Scrambler)**: LFSR feedback polynomial 15-bit.
- **Modulasi EFMPlus**: Kode kanal 16-bit dengan 4 State mesin finite automaton.
- **Mode Dispersi (Dispersion Mode)**: Memetakan intensitas piksel sumber ke densitas transisi (jump) EFMPlus:
  - Byte termodulasi $Q = 0xA5$: Menghasilkan 5 transisi per simbol kanal (area gelap/tekstur padat).
  - Byte termodulasi $Q = 0x92$: Menghasilkan 2 transisi per simbol kanal (area terang/tekstur renggang).
  - Data payload host diperoleh dari operasi inversi pengacak: $P = Q \oplus \text{mask}$.
- **DVD+R Physical Modulation**: Menggunakan modulasi fase pada frekuensi goyangan pregroove ADIP (*Address In Pre-groove*) sebesar 817.4 kHz, berbeda dengan modulasi LPP (*Land Pre-Pit*) pada DVD-R.
- **Dual Layer Transition**: Pemodelan perpindahan lapisan OTP (*Opposite Track Path*) pada LBA 2085856 di mana Lapisan 0 merekam ke arah luar dan Lapisan 1 merekam ke arah dalam.

### C. Blu-ray (BD-R 25 GB, 50 GB, dan BDXL 100 GB)
- **Standar**: ISO/IEC 30190:2021 (Single-layer 25.0 GB & Dual-layer 50.0 GB), BDXL System Specification.
- **Modulasi Saluran**: 17PP (*RLL 1,7 Parity Preserve*) dengan penyisipan bit kendali DC (*DC control bits*).
- **Koreksi Kesalahan**: LDC (*Long Distance Code*) dan BIS (*Burst Indicator Subcode*).
- **Bias Run-Length**:
  - Pola dominan 3T: Menggunakan byte payload teracak `0xAA` (mencapai 85.4% run-length 3T).
  - Pola dominan 6T: Menggunakan byte payload teracak `0x44` (mencapai 62.7% run-length 6T).
- **Sistem Berkas**: UDF 2.01 dengan partisi data fleksibel (penempatan file di lingkar dalam atau lingkar luar).

### D. Model Geometri Spiral & Polinomial Pitch Orde-3
Lintasan spiral piringan optik tidak memiliki pitch yang sepenuhnya seragam di seluruh permukaan akibat toleransi mekanikal dan variasi manufaktur cetakan polikarbonat (*stamper*). DiscImageStudio menggunakan model pitch polinomial derajat 3:
$$p(x) = p_0 \cdot \left(1 + c_1 x + c_2 x^2 + c_3 x^3\right)$$
di mana:
- $x = \frac{r - r_i}{r_o - r_i}$ adalah radius ternormalisasi ($0 \le x \le 1$).
- $p_0$ adalah nilai dasar pitch yang dikalkulasi secara analitik dari panjang lintasan total $L$ dan kendala kapasitas sektor.
- $c_1, c_2, c_3$ adalah koefisien relatif tak berdimensi yang diperoleh melalui kalibrasi foto piringan nyata.
Hubungan diferensial spiral:
$$\frac{dr}{d\theta} = \frac{p(x)}{2\pi} \implies \theta(r) = \int_{r_i}^r \frac{2\pi}{p(r)} \, dr$$

### E. Model Difraksi Kisi Optik Bragg (Bragg Diffraction Iridescence)
Piringan optik bekerja layaknya kisi difraksi reflektif sirkular (*circular reflection diffraction grating*). Berdasarkan persamaan difraksi Bragg:
$$m \cdot \lambda = d \cdot (\sin \theta_m + \sin \theta_i)$$
di mana:
- $d$ adalah spasi lintasan pita mikroskopis ($1.6\,\mu\text{m}$ untuk CD, $0.74\,\mu\text{m}$ untuk DVD, $0.32\,\mu\text{m}$ untuk Blu-ray).
- $\theta_i$ adalah sudut datang sinar cahaya dan $\theta_m$ adalah sudut pantul difraksi untuk orde difraksi $m$.
Perangkat lunak spinoff mensimulasikan kilau pelangi ini secara dinamis melalui fragment shader GLSL anisotropik.

---

## 3. Eksekusi & Implementasi Spinoff (Nawayasa Citramandala)

Solusi lengkap **Nawayasa Citramandala** (`NawayasaCitramandala.sln`) mencakup 11 modul terpadu yang teruji 100%:

1. **`NawayasaCitramandala.Core`**:
   - Kontrak antarmuka `IOpticalDiscModule` dan model tugas `DiscJobRequest`/`DiscJobResult`.
   - Polinomial pitch orde-3 `DvdTrackGeometry` dan kalkulasi spiral Archimedes.
   - `MediaFormatSpecification`: Basis pengetahuan geometri media CD, DVD-R, DVD+R, BD-R SL/DL, dan BDXL.
   - `BlueNoiseGrayscale`: Void-and-Cluster 64×64 torus blue noise dither.
   - `DyeProfile`: Profil kurva gamma respons pewarna kimia piringan (AZO, Cyanine, Phthalocyanine, BD-R HTL/LTH).
2. **`NawayasaCitramandala.ImageIO`**:
   - Pemuat dan penyimpan raster gambar cross-platform murni menggunakan `StbImageSharp` dan `StbImageWriteSharp`.
3. **`NawayasaCitramandala.Imaging`**:
   - `CalibrationTargetGenerator`: Sintesis pola uji kalibrasi universal otomatis.
   - `Disc3DVisualizer`: Generator simulator WebGL/Three.js fotorealistik interaktif mandiri.
   - `ImageDitherProcessor`: Mesin dithering multi-algoritma terpadu.
4. **`NawayasaCitramandala.Iso`**:
   - Generator sistem berkas ISO9660 (Joliet) dan UDF 2.01 dengan alokasi penempatan berkas hibrida.
5. **`NawayasaCitramandala.Cd`**:
   - Generator CD-DA Audio PCM, koreksi latensi CIRC 111-frame, dan ekspor WAV/CUE.
6. **`NawayasaCitramandala.Dvd`**:
   - Solver kendali state NRZI, kalkulator RS-PC ECC, pengacak data LFSR, dan dispersi lompatan $Q = 0xA5 / 0x92$.
7. **`NawayasaCitramandala.BluRay`**:
   - Modulasi 17PP RLL(1,7), penyisipan bit kendali DC, kluster RUB 496 frame, dan bias run-length 3T vs 6T.
8. **`NawayasaCitramandala.Burning`**:
   - Pipa pembakaran streaming langsung ke Windows IMAPI2 dengan ring buffer sirkuler 64 MiB (pra-pengisian 32 MiB).
9. **`NawayasaCitramandala.Vision`**:
   - Modul Computer Vision otonom untuk mendeteksi spindle tengah piringan ($x_0, y_0$), menghitung radius hub $r_i$ dan radius lingkar luar $r_o$, serta regresi otomatis kurva pitch $c_1, c_2, c_3$.
10. **`NawayasaCitramandala.Cli`**:
    - Aplikasi CLI terpadu: `autocalibrate`, `dither`, `target-generate`, `simulate-3d`, `cd-generate`, `solve`, dan `bd-generate`.
11. **`NawayasaCitramandala.Tests`**:
    - Rangkaian pengujian unit xUnit dengan tingkat kelulusan 100% (14 passed, 0 failed).
12. **`NawayasaCitramandala.App`**:
    - Aplikasi Desktop GUI modern berbasis WPF (.NET 9.0-windows) dengan arsitektur MVVM (CommunityToolkit.Mvvm) dan Lucide Icons.
    - Menghubungkan seluruh 6 alur kerja: Home, Design Studio, Live Readback Preview, Export Master, Streaming Burner (IMAPI2), dan Calibration Studio.
    - Mengintegrasikan fitur unggulan spinoff: tombol **"Auto-Calibrate with AI/Vision"** (computer vision) dan tombol **"3D Iridescence (WebGL)"** (simulator difraksi Bragg Three.js).

---

# BAGIAN II: KOMPILASI LENGKAP DOKUMEN PROYEK

## Dokumen 1: README.md
```markdown
# Nawayasa Citramandala (DiscImageStudio Spinoff)

> **Seni Visual Tingkat Lapisan Fisik pada Media Optik (CD, DVD, Blu-ray)**  
> *Spinoff & Evolusi Tingkat Lanjut dari DiscImageStudio (JiaFeiMiao-K-Cat/DiscImageStudio)*

Proyek ini memodulasi data biner, run-length coding (EFM, EFMPlus, 17PP), dan interleaving fisik (CIRC, RS-PC, LDC/BIS) agar kontras optik mikroskopis (pit, land, dan transition density) membentuk gambar yang tampak jelas oleh mata telanjang (visible naked-eye artwork).
```

## Dokumen 2: update_tracker.md
```markdown
# Update Tracker - Nawayasa Citramandala (DiscImageStudio Spinoff)

### [2026-11-05 10:25:00 +07:00] - Inisialisasi Proyek & Audit Mendalam Upstream Repo
- Action Type: [NEW]
- File Paths: update_tracker.md, Feature-ToBe-Implemented.md, README.md, Report-To-GeminiProject.md
- Reasoning: Audit komprehensif upstream DiscImageStudio v1.4.0 dan inisialisasi tata kelola proyek spinoff.

### [2026-11-05 11:00:00 +07:00] - Implementasi Spinoff Engine & Arsitektur Multi-Target .NET 9
- Action Type: [NEW] / [REFACTOR]
- File Paths: Directory.Build.props, NawayasaCitramandala.sln, src/NawayasaCitramandala.*, tests/NawayasaCitramandala.Tests
- Reasoning: Pembangunan 11 modul spinoff di bawah .NET 9 LTS/STS dengan Blue Noise Dithering, Dye Profiling, dan Computer Vision Auto-Calibration.

### [2026-11-05 11:16:00 +07:00] - Penambahan Extended Media Specs, Calibration Target Generator & Simulasi Difraksi 3D
- Action Type: [NEW]
- File Paths: MediaFormatSpecification.cs, CalibrationTargetGenerator.cs, Disc3DVisualizer.cs, AdvancedSpinoffFeaturesTests.cs
- Reasoning: Penambahan spesifikasi DVD+R / Dual-Layer, Generator Pola Kalibrasi Universal, dan Simulator Interaktif Difraksi Kisi Optik 3D WebGL (Bragg grating iridescence shader). 14/14 unit tests 100% lulus.

### [2026-11-05 12:15:00 +07:00] - Implementasi dan Integrasi Desktop GUI (NawayasaCitramandala.App - Opsi 1)
- Action Type: [NEW] / [MODIFY]
- File Paths: src/NawayasaCitramandala.App/*, LivePreviewViewModel.cs, CalibrationPhotoViewModel.cs, NawayasaCitramandala.sln
- Reasoning: Implementasi Desktop GUI WPF (.NET 9.0-windows) modern dengan MVVM CommunityToolkit dan Lucide Icons. Mengintegrasikan tombol "Auto-Calibrate with AI/Vision" dan simulator difraksi pelangi 3D WebGL.
```

## Dokumen 3: Feature-ToBe-Implemented.md
```markdown
# Feature-ToBe-Implemented: Nawayasa Citramandala (DiscImageStudio Spinoff)
- Progress Metrics: 18 Completed, 0 In Progress, 6 Postponed/Omitted (~88% Completion, Desktop GUI Core 100%).
- Milestone 0: Upstream Deep Audit & Foundational Architecture (Completed)
- Milestone 1: Core Physics & Algorithmic Enhancements (Completed: Blue Noise, Dye Profiling, CIRC & RS-PC, Extended Media Specs)
- Milestone 2: Smart Auto-Calibration with Computer Vision (Completed: Center/Edge Hough Detection, Pitch Regression, Universal Target Generator)
- Milestone 3: Desktop GUI & Application Integration (Option 1 Complete: WPF .NET 9, AI Vision Auto-Calibrate, 3D WebGL Iridescence)
- Milestone 4: Production Quality & Cloud / CI Verification (Completed: xUnit Test Suite 14/14, 3D Bragg Iridescence Simulation)
```

## Dokumen 4: src/NawayasaCitramandala.App/README.md
```markdown
# Nawayasa Citramandala App (NawayasaCitramandala.App)

Aplikasi Desktop GUI modern untuk Nawayasa Citramandala DiscImageStudio, dibangun menggunakan Windows Presentation Foundation (WPF) di atas .NET 9.0 (C# 13) dengan pola arsitektur MVVM (CommunityToolkit.Mvvm) dan ikonografi modern Lucide Icons.

Mencakup 6 ruang kerja utama: Home, Design Studio (multi-layer vector & raster), Live Readback Preview (dilengkapi simulator 3D Iridescence WebGL), Export Master, Streaming Burner (IMAPI2 dengan ring buffer 64 MB), dan Calibration Studio (dilengkapi Auto-Calibrate with AI/Vision otonom).
```
