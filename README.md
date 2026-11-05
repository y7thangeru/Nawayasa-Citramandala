# Nawayasa Citramandala (DiscImageStudio Spinoff)

> **Seni Visual Tingkat Lapisan Fisik pada Media Optik (CD, DVD, Blu-ray)**  
> *Spinoff & Evolusi Tingkat Lanjut dari DiscImageStudio (JiaFeiMiao-K-Cat/DiscImageStudio)*

---

## 🌟 Pengantar

**Nawayasa Citramandala** adalah studio perangkat lunak generasi baru untuk merancang, memodulasi, dan membakar karya seni visual langsung ke atas permukaan lapisan fisik piringan optik (CD-R, DVD-R, dan BD-R 25GB) menggunakan optical disc burner standar rumahan tanpa memerlukan drive khusus berlisensi (seperti Yamaha DiscT@2 atau Labelflash).

Proyek ini berbasis pada arsitektur luar biasa dari [JiaFeiMiao-K-Cat/DiscImageStudio](https://github.com/JiaFeiMiao-K-Cat/DiscImageStudio), yang memodulasi data biner, run-length coding (EFM, EFMPlus, 17PP), dan interleaving fisik (CIRC, RS-PC, LDC/BIS) agar kontras optik mikroskopis (*pit*, *land*, dan *transition density*) membentuk gambar yang tampak jelas oleh mata telanjang (*visible naked-eye artwork*).

---

## 🔬 Fondasi Ilmiah & Model Fisik

| Tipe Media | Standar Internasional | Skema Modulasi / Encoding | Prinsip Pembentukan Gambar Visual |
| :--- | :--- | :--- | :--- |
| **CD-R** | ECMA-130, ECMA-394, Red Book | EFM (8-to-14 Modulation), CIRC Interleaving (111-frame latency) | Modulasi nilai byte kontras (`0x0D` vs `0xAA`). Audio CD-DA (WAV/CUE) atau Mode 1 Data. |
| **DVD-R** | ECMA-267, ECMA-359 | EFMPlus (16-bit channel code, 4 States), RS-PC (PO 208×16, PI 182×10), LFSR Scrambler | Modulasi densitas transisi EFMPlus (*dispersion mode*): `0xA5` (5 transitions) vs `0x92` (2 transitions). |
| **BD-R** (25 GB) | ISO/IEC 30190 | 17PP (RLL 1,7 Parity Preserve), LDC+BIS ECC, DC bit insertion | Bias run-length 3T vs 6T: `0xAA` (85.4% 3T) vs `0x44` (62.7% 6T), UDF 2.01 filesystem. |

### Model Geometri Spiral & Presisi Pitch
- **Spiral Archimedes Presisi**: Pemetaan koordinat polar $(r, \theta)$ ke Linear Block Address (LBA) / Physical Sector Number (PSN).
- **Polinomial Pitch Orde-3**: $p(x) = p_0 (1 + c_1 x + c_2 x^2 + c_3 x^3)$ untuk mengeliminasi distorsi lengkung garis radial akibat variasi pitch manufaktur piringan.
- **Sistem Kalibrasi 3-Tahap**: Kalibrasi Kasar, Batas (Boundary), dan Halus (Fine) berbasis fotografi disk nyata.

---

## 🚀 Rencana Pengembangan Spinoff (Nawayasa Citramandala)

1. **Kompatibilitas Multi-SDK**: Mendukung .NET 9 LTS/STS dan .NET 10.
2. **Auto-Kalibrasi Computer Vision**: Otomatisasi deteksi $r_i, r_o$ dan koefisien lengkung pitch melalui pemrosesan citra (OpenCV/Skia) tanpa klik manual.
3. **Advanced Dithering & Tone Shading**: Penambahan algoritma *Blue Noise Dithering*, *Variable-Density Half-toning*, dan profiling respons kimia dye (AZO, Phthalocyanine, Cyanine).
4. **Cross-Platform UI & Decoupled Architecture**: Migrasi ke antarmuka modern cross-platform (Avalonia UI) yang mendukung Windows, Linux, dan macOS.
5. **Cross-Platform Streaming Burn Engine**: Mengintegrasikan abstraksi pembakaran multi-OS (Windows IMAPI2, Linux/macOS raw MMC/SCSI passthrough via libburn).
6. **Simulasi Fotorealistik 3D**: Rendering visualisasi 3D piringan optik interaktif dengan efek difraksi pelangi (*rainbow iridescence shader*).

---

## 📁 Struktur Repositori

```
NawayasaCitramandala_discImageStudio/
├── upstream_repo/          # Subtree/clone sumber asli DiscImageStudio v1.4.0
├── src/                    # Source code Nawayasa Citramandala
├── docs/                   # Dokumentasi teknis & riset model fisik
├── update_tracker.md       # Audit log pembaruan proyek
├── Feature-ToBe-Implemented.md # Pelacakan siklus hidup fitur
├── README.md               # Ringkasan utama proyek
└── Report-To-GeminiProject.md  # Laporan konsolidasi lengkap
```

---

## ⚖️ Lisensi & Penghargaan

- Proyek ini berbasis pada [DiscImageStudio](https://github.com/JiaFeiMiao-K-Cat/DiscImageStudio) oleh **JiaFeiMiao-K-Cat**, didistribusikan di bawah lisensi **GNU General Public License v3.0 or later (GPL-3.0-or-later)**.
- Penghargaan mendalam kepada JiaFeiMiao-K-Cat dan komunitas atas riset mendalam pada lapisan fisik optik CD/DVD/BD.
