# Nawayasa Citramandala App (`NawayasaCitramandala.App`)

Aplikasi Desktop GUI modern untuk **Nawayasa Citramandala DiscImageStudio**, dibangun menggunakan Windows Presentation Foundation (WPF) di atas **.NET 9.0 (C# 13)** dengan pola arsitektur **MVVM (CommunityToolkit.Mvvm)** dan ikonografi modern **Lucide Icons** (`MahApps.Metro.IconPacks.Lucide`).

---

## 1. Fitur Utama & Alur Kerja

Aplikasi mengorganisasikan alur kerja pembuatan karya seni visual piringan optik fisik ke dalam 6 ruang kerja (*workspaces*):

1. **Home (`HomeView`)**:
   - Kartu proyek terbaru (*recent project cards*), inisialisasi proyek baru (CD, DVD, Blu-ray), dan navigasi cepat.
2. **Design Studio (`DesignView`)**:
   - Komposisi multi-layer visual (vektor, raster citra, teks sirkular).
   - Pengaturan tone piringan: Dithering strength, brightness offset, sRGB linearization gamma, 2/4-level gray quantizer (CD), dan 3T/6T inversion (Blu-ray).
   - Pratinjau medallion real-time terproyeksi sesuai geometri fisik piringan.
3. **Live Readback Preview (`LivePreviewView`)**:
   - Pratinjau simulasi pembacaan optik (*optical readback projection*) berdasarkan berkas citra ISO / CD track yang telah di-generate.
   - **Inovasi Spinoff: 3D Iridescence (WebGL)**: Tombol aksi langsung di header untuk meluncurkan simulator interaktif 3D difraksi kisi optik Bragg (*Three.js WebGL shader*) di web browser secara lokal.
4. **Export Master (`ExportView`)**:
   - Ekspor citra raster resolusi tinggi (hingga 8192×8192 px master), berkas binari ISO/UDF, atau CD-DA audio track WAV/CUE.
5. **Streaming Burner (`BurnView`)**:
   - Pembakaran streaming berkecepatan tinggi ke perangkat optik fisik (CD/DVD/BD Writer) menggunakan Windows IMAPI2 dengan ring buffer 64 MB untuk mencegah buffer underrun.
6. **Calibration Studio (`CalibrationView`)**:
   - Alur kalibrasi akurasi geometri piringan fisik (3-langkah):
     1. Pembuatan target uji kalibrasi (*Universal Calibration Target*).
     2. Rektifikasi foto piringan optik hasil bakar.
     3. Estimasi kelengkungan pitch polinomial 3-derajat $p(x) = p_0 (1 + c_1 x + c_2 x^2 + c_3 x^3)$.
   - **Inovasi Spinoff: Auto-Calibrate with AI/Vision**: Tombol deteksi otonom berbasis computer vision (`DiscVisionCalibrator`) yang mengekstrak koordinat pusat spindle, radius dalam/luar, dan koefisien kelengkungan pitch tanpa klik manual.

---

## 2. Struktur Proyek

```
src/NawayasaCitramandala.App/
├── Assets/              # Ikon aplikasi, logo piringan, font HarmonyOS Sans
├── Controls/            # Kontrol visual custom (DiscDesignCanvas, TonePanel, StepCard, MonitorCard, dsb.)
├── Localization/        # Mesin lokalisasi multibahasa (English & Chinese JSON catalogs)
├── Properties/          # Assembly & manifest properties
├── Services/            # Layanan background, AppSettings, UnifiedCommandRunner, ConsoleModuleRunner
├── Themes/              # Token warna, light/dark theme dictionaries, control templates
├── ViewModels/          # ViewModel MVVM (ShellViewModel, DesignViewModel, ToneViewModel, CalibrationViewModel, dsb.)
├── Views/               # Tampilan XAML (HomeView, DesignView, LivePreviewView, ExportView, BurnView, CalibrationView)
├── AppHost.cs           # Bootstrap WPF application runtime
├── MainWindow.xaml      # Jendela utama dengan sidebar navigasi responsif
├── Program.cs           # Main entry point (GUI + CLI dual invocation mode)
└── NawayasaCitramandala.App.csproj
```

---

## 3. Cara Menjalankan

### Melalui Visual Studio / Rider:
Pilih proyek `NawayasaCitramandala.App` sebagai Startup Project dan tekan **F5** atau Run.

### Melalui .NET CLI:
```bash
dotnet run --project src/NawayasaCitramandala.App
```

Aplikasi juga mendukung eksekusi headless / CLI snapshot via argumen baris perintah:
```bash
dotnet run --project src/NawayasaCitramandala.App -- help
dotnet run --project src/NawayasaCitramandala.App -- burn-devices
```
