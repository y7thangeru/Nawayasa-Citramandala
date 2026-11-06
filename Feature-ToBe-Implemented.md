# Feature-ToBe-Implemented: Nawayasa Citramandala (DiscImageStudio Spinoff)

Daftar pelacakan siklus hidup fitur (Feature Lifecycle Tracking) untuk evolusi dan pengembangan spinoff **Nawayasa Citramandala DiscImageStudio**.

## Progress Metrics
- **Total Milestone**: 6
- **Planned Features**: 28
- **Completed**: 21
- **In Progress**: 0
- **Postponed / Omitted**: 6
- **Planned / Queued**: 1
- **Overall Completion**: ~75% (Core Engine, Desktop GUI, 4-Step Wizard, and Audio CD + Visual Hybrid Engine 100% Complete)

---

## Milestone 0: Upstream Deep Audit & Foundational Architecture
- [x] **Audit Arsitektur Upstream** (`JiaFeiMiao-K-Cat/DiscImageStudio` v1.4.0)
  - Analisis modul Core, Cd, Dvd, DvdImageSolver, BluRay, Iso, Burning, App, Cli.
  - Audit model fisik (CIRC 111-frame latency, EFMPlus 4-state dispersion, 17PP 3T/6T bias, 3rd-order track pitch polynomial).
- [x] **Project Governance Setup**
  - Pembuatan `update_tracker.md`, `Feature-ToBe-Implemented.md`, `README.md`, `Report-To-GeminiProject.md`.
- [x] **Multi-Target SDK Compatibility (.NET 9 & .NET 10)**
  - Mengadaptasi konfigurasi target framework agar kompatibel dengan .NET 9 LTS/STS yang terpasang di host serta forward-compatible ke .NET 10.
  - Konfigurasi `Directory.Build.props` dengan `InternalsVisibleTo` otomatis untuk pengujian terisolasi.

---

## Milestone 1: Core Physics & Algorithmic Enhancements
- [x] **CD-DA Latency & CIRC Boundary Interleaving**
  - Implementasi tabel CIRC delay schedule, inversi file offsets, dan pemformatan audio PCM little-endian / big-endian swap.
- [x] **DVD RS-PC ECC & EFMPlus Dispersion Engine**
  - Galois Field $GF(2^8)$ matrix multiplication, LFSR scrambler inversi $P = Q \oplus \text{mask}$, dan pembentukan payload transisi $0xA5 / 0x92$.
- [x] **Blu-ray 17PP RLL(1,7) & DC Control Engine**
  - Modulasi 17PP bebas adjacent 1s (min run 2T), kluster RUB 496 frame, dan bias run-length 3T vs 6T.
- [x] **Advanced Dithering & Tone Engine**
  - Implementasi **Blue Noise Void-and-Cluster (64×64 torus matrix)** untuk persepsi visual permukaan piringan optik tanpa artefak frekuensi rendah.
  - Dukungan **Optical Dye Profiling**: Kurva gamma adaptif untuk pewarna organik (AZO, Cyanine, Phthalocyanine) dan anorganik (BD-R HTL/LTH).
- [x] **Extended Media Support**
  - Spesifikasi fisik untuk DVD+R (ADIP address wobble modulation), DVD-DL (OTP/PTP transition at LBA 2085856), BD-R DL (50 GB), dan BDXL TL (100 GB).

---

## Milestone 2: Smart Auto-Calibration with Computer Vision
- [x] **Automated Edge & Center Detection (NawayasaCitramandala.Vision)**
  - Mendeteksi pusat piringan ($x_0, y_0$) dan lingkaran inner hub / outer perimeter ($r_i, r_o$) secara otomatis dari foto smartphone/scanner tanpa klik manual titik per titik (teruji pada resolusi 2400×2400 px dengan confidence 95%).
- [x] **Automated Radial Line Curvature Extraction**
  - Algoritma visual feature tracking untuk mengekstrak deviasi garis radial dan mengestimasi koefisien pitch polinomial $c_1, c_2, c_3$ secara otomatis.
- [x] **Universal Calibration Target Generator**
  - Generator target kalibrasi terpadu (`CalibrationTargetGenerator`): Kombinasi jeruji radial kardinal, cincin konsentris terkalibrasi 5mm, dan baji kontras abu-abu (*gray step wedges*) dalam satu gambar siap bakar.

---

## Milestone 3: Desktop GUI & Application Integration (Option 1 Complete)
- [x] **Decoupled Cross-Platform Core & Imaging Engine**
  - Memisahkan seluruh pemrosesan citra dari dependensi Windows desktop binaries (WIC / System.Windows.Media) menggunakan `BgraRaster` murni dan `StbImageSharp`/`StbImageWriteSharp`.
- [x] **Unified CLI Tool (`NawayasaCitramandala.Cli`)**
  - Tool CLI modern dengan perintah `autocalibrate`, `dither`, `target-generate`, `simulate-3d`, `cd-generate`, `solve`, dan `bd-generate`.
- [x] **Modern Desktop GUI (`NawayasaCitramandala.App` - WPF / .NET 9)**
  - Aplikasi Desktop GUI modern berbasis MVVM (CommunityToolkit.Mvvm) dan Lucide Icons.
  - Alur kerja lengkap: Home, Design Canvas, Live Readback Preview, Export Master, Streaming Disc Burner (IMAPI2), dan Calibration Studio.
  - **Easy 4-Step Disc Wizard (`WizardView` & `WizardViewModel`)**:
    - [x] **Step 1: Load Image & Format**: Pemilihan format media (CD-R, DVD-R, BD-R) dan pemuat gambar dengan pratinjau thumbnail instan.
    - [x] **Step 2: Position on Disc Layout**: Kanvas interaktif tata letak piringan dengan simulasi batas annulus fisik, lubang spindle, kontrol Scale, Rotasi, Offset X/Y, tombol presisi cepat (Fit, Center, Reset), dan dither Blue Noise.
    - [x] **Step 3: Finalize & Burn**: Ringkasan payload & sektor, deteksi otomatis optical drive recorder via Windows IMAPI2, pemilihan kecepatan bakar, dan progress bar streaming burn real-time.
    - [x] **Step 4: Save Design Option**: Opsi menyimpan proyek desain (.discproj) agar dapat diburn kembali di kemudian hari, serta tombol ekspor master ISO/WAV.
  - **Full English Localization Standardization**:
    - [x] Standardisasi seluruh teks dan katalog preset (`built-in-disc-presets.json`, `DiscPresetJsonStore`, `L.cs`) ke bahasa Inggris secara penuh.
  - **Inovasi GUI Spinoff**:
    - Tombol **"Auto-Calibrate with AI/Vision"** di tab Kalibrasi untuk penentuan batas dan kurva pitch piringan otomatis via algoritma Computer Vision tanpa klik manual.
    - Tombol **"3D Iridescence (WebGL)"** di tab Live Preview untuk membuka simulator kilau difraksi pelangi kisi Bragg interaktif di web browser.
    - Tombol **"Start Disc Wizard"** di Home View dan navigasi Sidebar untuk alur cepat ramah pemula.
- [ ] **Cross-Platform Direct SCSI/MMC Burning (Opsi 2 - Ditiadakan sesuai instruksi pengguna)**
- [ ] **Batch Processing & Headless Automated Artwork Pipeline (Opsi 3 - Ditunda untuk pengembangan lanjutan)**

---

## Milestone 4: Production Quality & Cloud / CI Verification
- [x] **Full Unit Test & Regression Suite**
  - 18 unit test lulus 100% tanpa error, memvalidasi matematika Galois Field, ECC DVD, latensi CIRC CD, batasan run-length 17PP Blu-ray, auto-kalibrasi Vision, Target Generator, Simulasi 3D, serta perhitungan kapasitas audio & partisi permukaan artwork CD.
- [x] **Export to Physical Visualization 3D Simulation**
  - Simulator fotorealistik 3D interaktif WebGL/Three.js mandiri (`Disc3DVisualizer` / `simulate-3d`) yang mensimulasikan kilau pelangi difraksi optik kisi Bragg (*Bragg optical diffraction iridescence shader*) dengan interaksi orbit 3D dan kontrol pencahayaan dinamis.

---

## Milestone 5: Hybrid Media & Multi-Track Authoring (Audio CD + Surface Visual Hybrid)
- [x] **Audio CD-DA & Red Book PCM Decoding Engine (`NawayasaCitramandala.Cd`)**
  - Penguraian file audio populer (`.mp3`, `.flac`, `.wav`, `.m4a`, `.aac`) secara otomatis ke format Red Book Audio standar: 44,100 Hz, 16-bit, stereo PCM, dengan perataan batas sektor 2,352-byte.
  - Pemanfaatan `NAudio` dan Windows Media Foundation reader & Sinc interpolator resampler bawaan.
- [x] **Live Dual-Gauge Capacity & Surface Radial Partition Calculator**
  - Perhitungan waktu putar audio total vs kapasitas maksimum CD standar 80 menit (79:58 / 359,849 sektor).
  - Perhitungan matematis batas radius permukaan piringan CD:
    $$r_{\text{audio\_end}} = \sqrt{r_{\text{inner}}^2 + \text{fraction}\cdot(r_{\text{outer}}^2 - r_{\text{inner}}^2)}$$
  - Menghitung sisa lebar radial piringan untuk cetak gambar visual artwork:
    $$\Delta r_{\text{art}} = 56.8 - r_{\text{audio\_end}} \text{ mm}$$
  - Pembaruan metrik *real-time* saat lagu ditambahkan, dihapus, atau diubah urutannya.
  - Proteksi batas kapasitas: Notifikasi peringatan jika audio melebihi 80 menit dengan penguncian tombol burn sampai lagu dikurangi.
- [x] **Interactive Audio + Visual Split Layout Canvas (`WizardView.xaml`)**
  - Kanvas preview piringan di Step 2 yang secara visual membagi piringan menjadi dua zona konsentris:
    1. **Inner Audio Zone (Cyan)**: Mewakili area data alur alunan lagu dari $r = 24.5$ mm hingga $r_{\text{audio\_end}}$ mm.
    2. **Outer Artwork Ring (Violet)**: Mewakili area cetak laser gambar visual dari $r_{\text{audio\_end}}$ mm hingga $56.8$ mm.
- [x] **Hybrid Master WAV & CUE Sheet Synthesizer (`CdAudioVisualHybridGenerator`)**
  - Menyintesis seluruh lagu menjadi satu stream audio master terpadu dengan file deskriptor `.cue` (Track 01 s.d. Track N Audio), dilanjutkan trek visual artwork pada sektor sisa di lingkar luar.
  - Opsi ekspor langsung master `.wav` + `.cue` siap pakai di Step 3 Wizard.
- [ ] **Hybrid Radial Zone Master Allocation (ISO 9660 / Joliet / UDF + Visual Artwork)**
  - Penggabungan berkas data file/installer pada partisi sistem berkas di lingkar dalam (*Inner Hub*, LBA rendah) yang dapat dibaca normal oleh sistem operasi (Windows Explorer, Linux, macOS).
