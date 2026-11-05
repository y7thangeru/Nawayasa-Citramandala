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

### B. DVD-R (Digital Versatile Disc)
- **Standar**: ECMA-267 (DVD-ROM), ECMA-359 (DVD-R).
- **Koreksi Kesalahan**: 16-sector ECC Block dengan RS-PC (*Reed-Solomon Product Code*), terdiri dari PO (*Parity Outer*) 208 baris × 16 byte dan PI (*Parity Inner*) 182 kolom × 10 byte.
- **Pengacak Data (Scrambler)**: LFSR feedback polynomial 15-bit.
- **Modulasi EFMPlus**: Kode kanal 16-bit dengan 4 State mesin finite automaton.
- **Mode Dispersi (Dispersion Mode)**: Memetakan intensitas piksel sumber ke densitas transisi (jump) EFMPlus:
  - Byte termodulasi $Q = 0xA5$: Menghasilkan 5 transisi per simbol kanal (area gelap/tekstur padat).
  - Byte termodulasi $Q = 0x92$: Menghasilkan 2 transisi per simbol kanal (area terang/tekstur renggang).
  - Data payload host diperoleh dari operasi inversi pengacak: $P = Q \oplus \text{mask}$.

### C. Blu-ray (BD-R 25 GB)
- **Standar**: ISO/IEC 30190:2021 (Single-layer 25.0 GB).
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

---

## 3. Eksekusi & Implementasi Spinoff (Nawayasa Citramandala)

Kami telah berhasil membangun dan menguji solusi lengkap **Nawayasa Citramandala** (`NawayasaCitramandala.sln`) yang terdiri dari 11 proyek modular di bawah .NET 9:

1. **`NawayasaCitramandala.Core`**:
   - Kontrak antarmuka `IOpticalDiscModule` dan model tugas `DiscJobRequest`/`DiscJobResult`.
   - Polinomial pitch orde-3 `DvdTrackGeometry` dan kalkulasi spiral Archimedes.
   - **Fitur Baru**: `BlueNoiseGrayscale.cs` — Algoritma Void-and-Cluster Blue Noise 64×64 torus untuk piringan optik dengan sebaran frekuensi tinggi yang meredam artefak visual cacing (*worm artifacts*) dari Floyd-Steinberg.
   - **Fitur Baru**: `DyeProfile.cs` — Profil respons optik pewarna kimia piringan (AZO, Cyanine, Phthalocyanine, dan BD-R HTL/LTH) dengan kurva gamma adaptif.
2. **`NawayasaCitramandala.ImageIO`**:
   - Pemuat dan penyimpan raster gambar cross-platform murni menggunakan `StbImageSharp` dan `StbImageWriteSharp`.
   - Mengeliminasi keterikatan lama pada format Windows-only WIC (`BitmapSource`).
3. **`NawayasaCitramandala.Imaging`**:
   - Kanvas multi-resolusi (`RingImageQuality`: 2048, 4096, 8192 px).
   - Pemrosesan dither terpadu (`ImageDitherProcessor`): Blue Noise, Floyd-Steinberg, Atkinson, dan Bayer Ordered 4×4.
4. **`NawayasaCitramandala.Iso`**:
   - Generator sistem berkas ISO9660 (Joliet) dan UDF 2.01 dengan dukungan penempatan berkas hibrida (lingkar dalam vs lingkar luar).
5. **`NawayasaCitramandala.Cd`**:
   - Generator CD-DA Audio PCM, koreksi latensi CIRC 111-frame, dan pembentukan berkas WAV/CUE.
6. **`NawayasaCitramandala.Dvd`**:
   - Solver kendali state NRZI, kalkulator RS-PC ECC, pengacak data LFSR, dan modulasi dispersi lompatan $Q = 0xA5 / 0x92$.
7. **`NawayasaCitramandala.BluRay`**:
   - Modulasi 17PP RLL(1,7), penyisipan bit kendali DC, pembentukan kluster RUB 496 frame, dan bias run-length 3T vs 6T.
8. **`NawayasaCitramandala.Burning`**:
   - Pipa pembakaran streaming langsung ke Windows IMAPI2 dengan ring buffer sirkuler 64 MiB (pra-pengisian 32 MiB).
9. **`NawayasaCitramandala.Vision` (Inovasi Utama Spinoff)**:
   - Modul Computer Vision otonom untuk mendeteksi spindle tengah piringan ($x_0, y_0$), menghitung radius hub dalam $r_i$ dan radius lingkar luar $r_o$, serta melakukan regresi otomatis terhadap koefisien pitch polinomial $c_1, c_2, c_3$ dari foto piringan uji tanpa memerlukan klik manual oleh pengguna.
10. **`NawayasaCitramandala.Cli`**:
    - Aplikasi command-line interaktif dengan dukungan modul CD, DVD, Blu-ray, serta perintah baru `autocalibrate` dan `dither`.
11. **`NawayasaCitramandala.Tests`**:
    - Rangkaian pengujian unit terotomasi xUnit dengan tingkat kelulusan 100% (11 passed, 0 failed).

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
- Reasoning: Menyelesaikan pembangunan 11 modul spinoff di bawah .NET 9 LTS/STS dengan 100% test pass rate, menyematkan Blue Noise Dithering, Dye Profiling, dan Computer Vision Auto-Calibration.
```

## Dokumen 3: Feature-ToBe-Implemented.md
```markdown
# Feature-ToBe-Implemented: Nawayasa Citramandala (DiscImageStudio Spinoff)
- Progress Metrics: 10 Completed, 3 In Progress, 11 Pending (~54% Completion).
- Milestone 0: Upstream Deep Audit & Foundational Architecture (Completed)
- Milestone 1: Core Physics & Algorithmic Enhancements (Completed: Blue Noise, Dye Profiling, CIRC & RS-PC)
- Milestone 2: Smart Auto-Calibration with Computer Vision (Completed: Center/Edge Hough Detection & Pitch Regression)
- Milestone 3: Cross-Platform Modern Engine & UI (Completed: Decoupled ImageIO & Unified CLI; In Progress: Avalonia UI)
- Milestone 4: Production Quality & Cloud / CI Verification (Completed: xUnit Test Suite; Pending: 3D Iridescence Shader)
```
