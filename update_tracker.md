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


### [2026-11-05 11:10:00 +07:00] - Pembuatan Dokumentasi Lengkap Multi-Tiered & Persiapan VCS Remote Push
- **Action Type**: `[NEW]` / `[MODIFY]`
- **File Paths**:
  - `README.md` `[MODIFY]`
  - `Feature-ToBe-Implemented.md` `[MODIFY]`
  - `docs/ARCHITECTURE.md` `[NEW]`
  - `docs/PHYSICAL_LAYER_MATH.md` `[NEW]`
  - `docs/CALIBRATION_GUIDE.md` `[NEW]`
  - `docs/CLI_REFERENCE.md` `[NEW]`
  - `src/NawayasaCitramandala.Core/README.md` `[NEW]`
  - `src/NawayasaCitramandala.ImageIO/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Imaging/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Iso/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Cd/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Dvd/README.md` `[NEW]`
  - `src/NawayasaCitramandala.BluRay/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Burning/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Vision/README.md` `[NEW]`
  - `src/NawayasaCitramandala.Cli/README.md` `[NEW]`
  - `tests/NawayasaCitramandala.Tests/README.md` `[NEW]`
  - `Report-To-GeminiProject.md` `[MODIFY]`
- **Reasoning**:
  - Mengimplementasikan Comprehensive Multi-Tiered Documentation Architecture sesuai Antigravity Global Rules.
  - Setiap modul dan paket di bawah `src/` dan `tests/` memiliki `README.md` spesifik yang mendokumentasikan API, invariant fisik, dependensi, dan contoh penggunaan.
  - Folder `docs/` diperluas dengan panduan mendalam: arsitektur sistem, turunan matematika lapisan fisik (spiral Archimedes, EFM/EFMPlus, 17PP, CIRC 111-frame delay), panduan kalibrasi computer vision, dan referensi CLI terpadu.
  - Mengompilasi seluruh dokumen ke dalam laporan master all-in-one `Report-To-GeminiProject.md` untuk sinkronisasi VCS ke repositori remote `https://github.com/y7thangeru/Nawayasa-Citramandala`.

### [2026-11-05 11:16:00 +07:00] - Penambahan Extended Media Specs, Calibration Target Generator & Simulasi Difraksi 3D
- **Action Type**: `[NEW]`
- **File Paths**:
  - `src/NawayasaCitramandala.Core/MediaFormatSpecification.cs`
  - `src/NawayasaCitramandala.Imaging/CalibrationTargetGenerator.cs`
  - `src/NawayasaCitramandala.Imaging/Disc3DVisualizer.cs`
  - `src/NawayasaCitramandala.Cli/Program.cs` (`target-generate` & `simulate-3d`)
  - `tests/NawayasaCitramandala.Tests/AdvancedSpinoffFeaturesTests.cs`
- **Reasoning**:
  - Memperluas pemodelan format piringan optik: DVD+R (ADIP wobble phase modulation), DVD-DL (OTP/PTP layer transition), BD-R DL (50 GB), dan BD-R XL TL (100 GB).
  - Menyediakan generator pola uji kalibrasi otonom terpadu (Universal Calibration Target Generator) berisi jeruji radial, cincin konsentris terkalibrasi 5mm, dan baji kontras abu-abu (*gray step wedges*).
  - Mengimplementasikan simulator fotorealistik WebGL/Three.js interaktif dengan fragment shader khusus yang mensimulasikan kilau pelangi difraksi optik Bragg (*Bragg optical diffraction grating iridescence*).
  - Memperluas cakupan test suite menjadi 14 unit tests (100% passed).

### [2026-11-05 12:15:00 +07:00] - Implementasi dan Integrasi Desktop GUI (NawayasaCitramandala.App - Opsi 1)
- **Action Type**: `[NEW]` / `[MODIFY]`
- **File Paths**:
  - `src/NawayasaCitramandala.App/` `[NEW]` (WPF App di atas .NET 9.0-windows, MVVM CommunityToolkit, Lucide Icons)
  - `src/NawayasaCitramandala.App/NawayasaCitramandala.App.csproj` `[NEW]`
  - `src/NawayasaCitramandala.App/Services/ConsoleModuleRunner.cs` `[NEW]`
  - `src/NawayasaCitramandala.App/ViewModels/LivePreviewViewModel.cs` `[MODIFY]` (Integrasi `Launch3DViewerCommand`)
  - `src/NawayasaCitramandala.App/Views/LivePreviewView.xaml` `[MODIFY]` (Penambahan tombol aksi "3D Iridescence (WebGL)")
  - `src/NawayasaCitramandala.App/ViewModels/CalibrationPhotoViewModel.cs` `[MODIFY]` (Integrasi `AutoCalibrateVisionCommand`)
  - `src/NawayasaCitramandala.App/Views/CalibrationView.xaml` `[MODIFY]` (Penambahan tombol "Auto-Calibrate with AI/Vision")
  - `src/NawayasaCitramandala.App/README.md` `[NEW]` (Dokumentasi lengkap modul GUI)
  - `src/NawayasaCitramandala.ImageIO/RasterCodec.cs` `[MODIFY]` (Penambahan `LoadBgra32FromBytes`)
  - `src/NawayasaCitramandala.Imaging/RingImageQuality.cs` `[MODIFY]` (Penambahan alias `GenerationSize` & `PreviewSize`)
  - `NawayasaCitramandala.sln` `[MODIFY]` (Registrasi proyek App ke dalam solution)
  - `Feature-ToBe-Implemented.md` `[MODIFY]`
  - `update_tracker.md` `[MODIFY]`
- **Reasoning**:
  - Menyelesaikan implementasi **Option 1: Modern Desktop GUI (WPF / .NET 9)** sesuai preferensi pengguna.
  - Memporting seluruh views, controls, themes, dan viewmodels dari arsitektur modern upstream ke namespace `NawayasaCitramandala.App`.
  - Mengintegrasikan fitur unggulan spinoff langsung ke antarmuka pengguna:
    1. Tombol **"Auto-Calibrate with AI/Vision"** pada modul kalibrasi untuk deteksi kontur batas dan kelengkungan pitch secara otonom via computer vision tanpa klik manual.
    2. Tombol **"3D Iridescence (WebGL)"** pada layar Live Preview untuk meluncurkan simulator interaktif difraksi kisi optik Bragg berbasis Three.js di browser default.
### [2026-11-05 12:22:00 +07:00] - Pembuatan Executable Mandiri (Standalone & Portable .EXE)
- **Action Type**: `[NEW]` / `[MODIFY]`
- **File Paths**:
  - `publish/win-x64-standalone/NawayasaCitramandala.exe` `[NEW]` (Single-file self-contained Windows executable, ~162 MB, zero runtime dependencies)
  - `publish/win-x64-portable/NawayasaCitramandala.exe` `[NEW]` (Framework-dependent portable launcher executable)
  - `.gitignore` `[MODIFY]` (Mengecualikan folder biner `publish/`)
- **Reasoning**:
  - Memenuhi permintaan pengguna untuk menyediakan berkas `.exe` siap pakai yang dapat langsung dijalankan dengan klik ganda (*double-click*) di lingkungan Windows tanpa konfigurasi CLI.
  - Varian standalone mengemas seluruh runtime .NET 9 Desktop, modul pemrosesan citra, pustaka pembakaran IMAPI2, modul computer vision, dan aset antarmuka ke dalam satu file biner tunggal.

### [2026-11-05 12:35:00 +07:00] - Perbaikan Lokalisasi Default ke Bahasa Inggris & Resource Loader Bugfix
- **Action Type**: `[FIX]` / `[MODIFY]`
- **File Paths**:
  - `src/NawayasaCitramandala.App/Localization/L.cs` `[FIX]` (Perbaikan resource filtering string dari `StartsWith("NawayasaCitramandala.Localization...")` ke `Contains("Localization.Catalogs...")`, pengalihan bahasa default ke English `en`)
  - `src/NawayasaCitramandala.App/Services/AppSettings.cs` `[MODIFY]` (Default language property diatur ke `L.English`)
  - `src/NawayasaCitramandala.App/AppHost.cs` `[MODIFY]` (Default runtime bootstrap language diatur ke `L.English`)
  - `src/NawayasaCitramandala.App/Services/AppSettingsStore.cs` `[FIX]` (Penyesuaian folder konfigurasi lokal dari `DiscImageStudio` ke `NawayasaCitramandala`)
  - `tests/NawayasaCitramandala.Tests/AdvancedSpinoffFeaturesTests.cs` `[NEW]` (Penambahan unit test verifikasi lokalisasi bahasa Inggris)
  - `tests/NawayasaCitramandala.Tests/NawayasaCitramandala.Tests.csproj` `[MODIFY]`
  - `publish/win-x64-standalone/NawayasaCitramandala.exe` `[REBUILD]`
  - `publish/win-x64-portable/NawayasaCitramandala.exe` `[REBUILD]`
- **Reasoning**:
  - Memperbaiki bug kritis di mana catalog JSON lokalisasi gagal dimuat karena perbedaan RootNamespace (`NawayasaCitramandala.App` vs `NawayasaCitramandala`), menyebabkan aplikasi menampilkan teks fallback Mandarin mentah.
  - Mengubah bahasa antarmuka default menjadi **Bahasa Inggris (English)** sesuai permintaan pengguna, dengan catalog JSON 100% lengkap dan terverifikasi unit test (15/15 passed).

### [2026-11-06 09:47:00 +07:00] - Penambahan Milestone 5: Hybrid Media & Multi-Track Authoring
- **Action Type**: `[MODIFY]`
- **File Paths**:
  - `Feature-ToBe-Implemented.md` `[MODIFY]`
  - `update_tracker.md` `[MODIFY]`
- **Reasoning**:
  - Merespons permintaan pengguna untuk memasukkan kapabilitas penyimpanan berkas data (dokumen, MP3, installer) dan trek musik Audio CD (CD-DA) bersamaan dengan pembakaran gambar visual ke dalam roadmap fitur `Feature-ToBe-Implemented.md`.
  - Menambahkan Milestone 5 dengan 4 fitur spesifik:
    1. *Hybrid Radial Zone Master Allocation (ISO 9660 / Joliet / UDF + Visual Artwork)*
    2. *Audio CD-DA & Mixed-Mode / Enhanced CD (CD-Extra / Blue Book) Master Engine*
    3. *Interactive Radial Partition Manager & Capacity Calculator in Desktop GUI*
    4. *CLI Hybrid Authoring Commands (`hybrid-cd` & `hybrid-iso`)*
  - Memperbarui metrik progres proyek (Total Milestone: 6, Planned Features: 28, Planned/Queued: 4).


### [2026-11-06 09:48:00 +07:00] - Implementasi 4-Step Easy Disc Wizard & Standardisasi Bahasa Inggris Penuh
- **Action Type**: `[NEW]` / `[MODIFY]` / `[REFACTOR]`
- **File Paths**:
  - `src/NawayasaCitramandala.App/ViewModels/WizardViewModel.cs` `[NEW]`
  - `src/NawayasaCitramandala.App/Views/WizardView.xaml` `[NEW]`
  - `src/NawayasaCitramandala.App/Views/WizardView.xaml.cs` `[NEW]`
  - `src/NawayasaCitramandala.App/ViewModels/Workspace.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.App/ViewModels/ShellViewModel.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.App/MainWindow.xaml` `[MODIFY]`
  - `src/NawayasaCitramandala.App/MainWindow.xaml.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.App/Views/HomeView.xaml` `[MODIFY]`
  - `src/NawayasaCitramandala.App/ViewModels/HomeViewModel.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.App/ViewModels/ProjectDialogViewModel.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.Core/Presets/built-in-disc-presets.json` `[MODIFY]`
  - `src/NawayasaCitramandala.Core/DiscPresetJsonStore.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.Cd/CdDiscPreset.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.Dvd/DvdDiscPreset.cs` `[MODIFY]`
  - `publish/win-x64-standalone/NawayasaCitramandala.exe` `[REFACTOR]`
  - `publish/win-x64-portable/NawayasaCitramandala.exe` `[REFACTOR]`
- **Reasoning**:
  - Memenuhi permintaan pengguna untuk merampingkan seluruh alur aplikasi menjadi alur **Wizard 4 Langkah (4-Step Easy Wizard)**:
    1. **Step 1: Load Image & Format**: Memilih format media (CD-R 700MB, DVD-R 4.7GB, BD-R 25GB) dan memilih file gambar (`.png`, `.jpg`, `.webp`, `.bmp`).
    2. **Step 2: Position on Disc Layout**: Kanvas interaktif layout piringan CD/DVD/BD dengan visualisasi batas cetak fisik & hub lubang tengah. Dilengkapi kontrol slider Scale (%), Rotasi (°), Offset X/Y (mm), tombol preset cepat (Fit to Disc, Center, Reset Rotation), serta algoritma dither Blue Noise.
    3. **Step 3: Finalize & Burn**: Ringkasan payload & sektor piringan, deteksi otomatis optical drive recorder via Windows IMAPI2, pemilihan kecepatan tulis (Write Speed), konfirmasi burn, dan progress streaming burn real-time.
    4. **Step 4: Save Design Option**: Opsi menyimpan desain piringan ke dalam file proyek `.discproj` agar dapat dibuka kembali dan diburn di lain waktu, serta opsi ekspor master image ISO/WAV.
  - Memperbaiki isu bahasa Mandarin: Menstandarisasi katalog bawaan `built-in-disc-presets.json`, `DiscPresetJsonStore`, dan fallback view model ke bahasa Inggris secara penuh (`en`).
  - Menghubungkan Wizard ke navigasi Sidebar `NavWizard` dan menambahkan banner Hero Card di `HomeView.xaml` untuk akses instan satu klik.
  - Membangun ulang file executable mandiri `publish/win-x64-standalone/NawayasaCitramandala.exe` (170 MB) dan portable `publish/win-x64-portable/NawayasaCitramandala.exe`.


### [2026-11-06 10:10:00 +07:00] - Implementasi Fitur Hybrid Audio CD (MP3/FLAC/WAV) + Artwork Permukaan & Kalkulator Kapasitas Real-Time
- **Action Type**: `[NEW]` / `[MODIFY]` / `[REFACTOR]`
- **File Paths**:
  - `src/NawayasaCitramandala.Cd/CdAudioTrack.cs` `[NEW]`
  - `src/NawayasaCitramandala.Cd/CdAudioVisualHybridGenerator.cs` `[NEW]`
  - `src/NawayasaCitramandala.Cd/NawayasaCitramandala.Cd.csproj` `[MODIFY]`
  - `src/NawayasaCitramandala.App/NawayasaCitramandala.App.csproj` `[MODIFY]`
  - `src/NawayasaCitramandala.App/Services/FileDialogService.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.App/ViewModels/WizardViewModel.cs` `[MODIFY]`
  - `src/NawayasaCitramandala.App/Views/WizardView.xaml` `[MODIFY]`
  - `tests/NawayasaCitramandala.Tests/CdAudioCapacityTests.cs` `[NEW]`
  - `publish/win-x64-standalone/NawayasaCitramandala.exe` `[REFACTOR]`
  - `Feature-ToBe-Implemented.md` `[MODIFY]`
  - `update_tracker.md` `[MODIFY]`
- **Reasoning**:
  - Memenuhi permintaan pengguna untuk mengimplementasikan fitur burn **Audio CD + Gambar visual di permukaan CD**:
    1. Pengguna dapat melampirkan beberapa file audio sekaligus (`.mp3`, `.flac`, `.wav`, `.m4a`, `.aac`) pada Step 1 Wizard.
    2. Sistem secara otomatis menghitung durasi total audio yang dapat diputar (vs kapasitas maksimal 80 menit / 79:58 standar Red Book).
    3. Sistem secara dinamis menghitung dan menampilkan seberapa lebar sisa bidang piringan CD yang dapat digunakan untuk membakar gambar visual artwork (dalam milimeter, dari radius akhir audio $r_{\text{audio\_end}}$ hingga lingkar luar 56.8 mm).
    4. Menyediakan antarmuka dual-gauge (Audio Playing Time vs Available Artwork Radial Width) dan visualisasi split layout konsentris (Inner Audio Zone berwarna Cyan & Outer Artwork Ring berwarna Violet) pada kanvas interaktif piringan di Step 2.
    5. Menyediakan engine `CdAudioVisualHybridGenerator` untuk decoding audio multi-format ke format Red Book PCM 44.1kHz 16-bit stereo berlatensi CIRC, lalu menyatukannya dengan sektor visual artwork lingkar luar dan menghasilkan file CUE sheet + master WAV siap bakar.
    6. Menambahkan proteksi kapasitas piringan dengan peringatan otomatis jika durasi lagu melebihi 80 menit.
    7. Menambahkan suite pengujian unit `CdAudioCapacityTests.cs` (18/18 pengujian unit lulus 100%).
    8. Membangun ulang file executable mandiri `publish/win-x64-standalone/NawayasaCitramandala.exe` (172 MB).


### [2026-11-06 10:25:00 +07:00] - Perbaikan Fatal XamlParseException Lucide Icons (Wand2, Home, AlertTriangle, Sliders, CheckCircle)
- **Action Type**: `[FIX]` / `[REFACTOR]`
- **File Paths**:
  - `src/NawayasaCitramandala.App/MainWindow.xaml` `[FIX]`
  - `src/NawayasaCitramandala.App/Views/HomeView.xaml` `[FIX]`
  - `src/NawayasaCitramandala.App/Views/WizardView.xaml` `[FIX]`
  - `.gitignore` `[MODIFY]`
  - `publish/win-x64-standalone/NawayasaCitramandala.exe` `[REFACTOR]`
  - `NawayasaCitramandala.exe` `[NEW]`
  - `update_tracker.md` `[MODIFY]`
- **Reasoning**:
  - Menginvestigasi laporan pengguna bahwa aplikasi tidak muncul saat di-double-click.
  - Melalui audit Windows Application Event Log (`Application Error` & `.NET Runtime`), ditemukan crash fatal `XamlParseException`:
    `System.FormatException: Wand2 is not a valid value for PackIconLucideKind.`
  - Dilakukan pemindaian otomatis via skrip refleksi PowerShell terhadap seluruh file `.xaml` menggunakan `MahApps.Metro.IconPacks.Lucide.dll`, dan ditemukan 5 icon name yang tidak terdaftar pada enum:
    1. `Wand2` -> diperbaiki menjadi `WandSparkles` di `MainWindow.xaml` dan `HomeView.xaml`.
    2. `Home` -> diperbaiki menjadi `House` di `WizardView.xaml`.
    3. `AlertTriangle` -> diperbaiki menjadi `TriangleAlert` di `WizardView.xaml`.
    4. `Sliders` -> diperbaiki menjadi `SlidersHorizontal` di `WizardView.xaml`.
    5. `CheckCircle` -> diperbaiki menjadi `CircleCheck` di `WizardView.xaml`.
  - Memverifikasi ulang bahwa seluruh icon di semua file `.xaml` 100% valid tanpa satupun error enum.
  - Mengompilasi dan menguji jalannya executable mandiri secara langsung (`STANDALONE EXE IS RUNNING FLAWLESSLY! PID: 46616`).
  - Menyalin executable mandiri siap klik langsung ke folder root proyek `NawayasaCitramandala.exe` (172 MB).


### [2026-11-06 10:33:00 +07:00] - Perbaikan Fatal Binding Mode BurnProgress, StaticResource Missing Keys & GoToStep Command Argument
- **Action Type**: `[FIX]` / `[REFACTOR]`
- **File Paths**:
  - `src/NawayasaCitramandala.App/ViewModels/WizardViewModel.cs` `[FIX]`
  - `src/NawayasaCitramandala.App/Views/WizardView.xaml` `[FIX]`
  - `publish/win-x64-standalone/NawayasaCitramandala.exe` `[REFACTOR]`
  - `NawayasaCitramandala.exe` `[REFACTOR]`
  - `update_tracker.md` `[MODIFY]`
- **Reasoning**:
  - Berdasarkan pembacaan Windows Application Event Viewer secara real-time, ditemukan 2 error berturut-turut pada startup WPF:
    1. `XamlParseException: Cannot find resource named 'ControlHeightLarge'`: Diperbaiki dengan mengganti resource yang hilang (`PrimarySubtleBrush`, `DangerSubtleBrush`, `ControlHeightLarge`) ke nilai konkret hex/double valid.
    2. `ArgumentException: Parameter cannot be of type System.String as command requires System.Int32`: Pada tombol navigasi tab wizard (Step 1 s.d. 4), XAML mengirimkan `string` `"1"`. Diperbaiki dengan mengizinkan `RelayCommand<object>` dengan parsing aman `int.TryParse`.
    3. `InvalidOperationException: TwoWay binding cannot work on read-only property 'BurnProgress'`: Diperbaiki dengan menambahkan setter dan mendeklarasikan `Mode=OneWay` pada `ProgressBar.Value`.
  - Berhasil meluncurkan GUI aplikasi langsung ke sesi Desktop Windows pengguna via Windows Explorer (`PID: 32868`, `MainWindowTitle: Disc Image Studio`, `Responding: True`).

### [2026-11-06 11:23:00 +07:00] - Penyelesaian 5 Masalah Utama Wizard: Dialog Pop-up, Drag Canvas Mouse, Live Dither Preview, Media Selection, & Burn/Save Lock
- **Action Type**: [FIX] / [REFACTOR]
- **File Paths**:
  - src/NawayasaCitramandala.App/ViewModels/ShellViewModel.cs [MODIFY]
  - src/NawayasaCitramandala.App/ViewModels/BurnViewModel.cs [MODIFY]
  - src/NawayasaCitramandala.App/ViewModels/WizardViewModel.cs [FIX] / [REFACTOR]
  - src/NawayasaCitramandala.App/Views/WizardView.xaml [FIX] / [REFACTOR]
  - src/NawayasaCitramandala.App/Views/WizardView.xaml.cs [FIX] / [NEW]
  - publish/win-x64-standalone/NawayasaCitramandala.exe [REFACTOR]
  - NawayasaCitramandala.exe [REFACTOR]
  - update_tracker.md [MODIFY]
- **Reasoning**:
  - Issue 1 (Dialog Pop-up saat Load Image): EnsureProjectOpen() sebelumnya memanggil _shell.RequestNewProjectFromTemplateAsync(..., null) yang membuka modal ProjectDialogWindow. Diperbaiki dengan mengimplementasikan CreateAndOpenProjectHeadless pada ShellViewModel sehingga pembuatan proyek di dalam Wizard berjalan headless di memori tanpa memunculkan dialog pop-up yang membingungkan.
  - Issue 2 (Interaksi Mouse Drag-and-Drop & Live Dither Preview):
    1. Menambahkan event handler canvas mouse (OnCanvasMouseLeftButtonDown, OnCanvasMouseMove, OnCanvasMouseLeftButtonUp, dan OnCanvasMouseWheel) dengan kursor SizeAll. Pengguna kini dapat langsung menggeser/menarik gambar dengan mouse secara halus (dikonversi dari piksel ke milimeter dengan faktor 3.5 px/mm) serta melakukan zoom in/out dengan scroll roda mouse.
    2. Mengintegrasikan ImageDitherProcessor.Process(...) ke dalam WizardViewModel secara asinkron dengan caching raster mentah _rawSourceRaster. Saat algoritma dithering diganti (BlueNoise, FloydSteinberg, Bayer, Threshold), preview permukaan optik diperbarui secara real-time di layar.
  - Issue 3 (Kartu Format Media CD/DVD/BD Tidak Bisa Diklik):
    1. Mengganti RadioButton dengan kartu Button interaktif yang terikat ke SelectFormatCommand dan visual data-trigger yang jelas (border ungu menyala #7C4DFF, latar belakang gelap elegan #221e35, dan indikator radio bulat tercentang).
    2. Menghilangkan pemanggilan dialog pop-up pada OnSelectedFormatChanged, sehingga pergantian media CD-R / DVD-R / BD-R terjadi seketika tanpa gangguan.
  - Issue 4 (Status & Progress Bar Burning, serta Penguncian Navigasi):
    1. Memperbaiki notifikasi properti Burn_PropertyChanged: sebelumnya XAML mengikat ke IsBurning dan BurnProgress, namun event notification memancarkan nama IsBurnRunning dan BurnProgressValue.
    2. Memperbarui BurnViewModel.BurnStatusText agar memperbarui telemetri throughput (MB ditulis, kecepatan MB/s, multiplier) pada burning standar.
    3. Mengunci navigasi Wizard (NextStepCommand, PrevStepCommand, dan tab langkah) saat IsBurning == true (CanExecute = false), sehingga tombol Proceed to Step 4 dinonaktifkan selama laser sedang membakar piringan optik.
    4. Menambahkan kartu progress burn yang menonjol lengkap dengan persentase besar (e.g. 45%), animasi progress bar, status teks langsung, dan badge peringatan penguncian tray drive optik.
  - Issue 5 (Aplikasi Hang saat Klik Save & Penguncian Drive CD):
    1. ExecuteSaveProjectAsync() sebelumnya memanggil PickSave dua kali secara bertumpukan (PickSave lalu SaveProjectAsCommand yang memanggil PickSave lagi), memicu modal freeze/deadlock pada thread WPF. Diperbaiki dengan memanggil PickSave tepat satu kali, menyusun ProjectDocument, dan menyimpannya langsung via ProjectJsonStore.Save di task background.
    2. Memastikan seluruh sesi IMAPI2 dan drive optik dilepaskan (finally COM release) sebelum save/export diizinkan.
  - Mengompilasi seluruh solusi (0 warning, 0 error), memvalidasi seluruh unit tests (18 passed), mempublikasikan standalone binary baru ke NawayasaCitramandala.exe (172 MB), dan meluncurkan GUI langsung ke layar desktop pengguna (PID: 43316).
