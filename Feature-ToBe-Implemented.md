# Feature-ToBe-Implemented: Nawayasa Citramandala (DiscImageStudio Spinoff)

Daftar pelacakan siklus hidup fitur (Feature Lifecycle Tracking) untuk evolusi dan pengembangan spinoff **Nawayasa Citramandala DiscImageStudio**.

## Progress Metrics
- **Total Milestone**: 6
- **Planned Features**: 28
- **Completed**: 18
- **In Progress**: 0
- **Postponed / Omitted**: 6
- **Planned / Queued**: 4
- **Overall Completion**: ~64% (Core Engine & Desktop GUI 100% Complete)

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
  - 14 unit test lulus 100% tanpa error, memvalidasi matematika Galois Field, ECC DVD, latensi CIRC CD, batasan run-length 17PP Blu-ray, auto-kalibrasi Vision, Target Generator, dan Simulasi 3D.
- [x] **Export to Physical Visualization 3D Simulation**
  - Simulator fotorealistik 3D interaktif WebGL/Three.js mandiri (`Disc3DVisualizer` / `simulate-3d`) yang mensimulasikan kilau pelangi difraksi optik kisi Bragg (*Bragg optical diffraction iridescence shader*) dengan interaksi orbit 3D dan kontrol pencahayaan dinamis.

---

## Milestone 5: Hybrid Media & Multi-Track Authoring (Data Files & Audio CD Integration)
- [ ] **Hybrid Radial Zone Master Allocation (ISO 9660 / Joliet / UDF + Visual Artwork)**
  - Penggabungan berkas data nyata (dokumen, MP3, file installer, arsip) pada partisi sistem berkas di lingkar dalam (*Inner Hub*, LBA rendah) yang dapat dibaca normal oleh sistem operasi (Windows Explorer, Linux, macOS), dengan sektor visual artwork mengisi sisa radius luar (*Outer Rim*) sebagai *padding* sektor terpadu.
- [ ] **Audio CD-DA & Mixed-Mode / Enhanced CD (CD-Extra / Blue Book) Master Engine**
  - Pembuatan master audio CD kompatibel: Trek audio PCM 44.1 kHz 16-bit asli pada trek 1 s.d. N yang dapat diputar secara normal dan jernih pada pemutar CD mobil / pemutar stereo konvensional, diikuti trek visual terisolasi pada trek penutup atau sesi kedua (*Session 2*) guna mencegah timbulnya *white noise* saat CD diputar dari awal.
- [ ] **Interactive Radial Partition Manager & Capacity Calculator in Desktop GUI**
  - Kontrol antarmuka visual interaktif (slider / radial ring picker) pada Desktop GUI (`NawayasaCitramandala.App`) untuk menentukan batas radius inner data ($r_{\text{start}}$ s.d. $r_{\text{art}}$) dan visual artwork ($r_{\text{art}}$ s.d. $r_{\text{end}}$), dilengkapi kalkulator kapasitas otomatis (Megabytes file vs menit trek audio vs resolusi kanvas visual) secara *real-time*.
- [ ] **CLI Hybrid Authoring Commands (`hybrid-cd` & `hybrid-iso`)**
  - Sub-perintah CLI terpadu untuk menyusun master disc hibrida dari direktori berkas lokal / daftar trek audio `.wav` + berkas gambar input, menghasilkan file image `.iso`, `.udf`, atau `.cue`/`.bin` siap bakar.
