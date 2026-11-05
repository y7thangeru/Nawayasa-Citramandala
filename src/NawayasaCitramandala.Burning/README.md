# NawayasaCitramandala.Burning

Hardware optical disc recording engine, COM IMAPI2 streaming bridge, and SCSI/MMC protocol writer.

## 1. Overview
The `NawayasaCitramandala.Burning` module provides direct hardware recording capabilities to write synthesized disc images onto physical CD-R, DVD-R, and BD-R discs using standard optical drives.

## 2. Key Components

### `WindowsImapiBurner`
Native Windows optical recording engine using Image Mastering API v2 (`IMAPI2`):
- Discovers optical drives (`IDiscMaster2`, `IDiscRecorder2`).
- Configures media write formats (`MsftDiscFormat2Data`, `MsftDiscFormat2TrackAtOnce`).
- Manages laser write speeds and enables hardware Buffer Underrun Protection (Burn-Proof / Seamless Link).
- Surfaces real-time write progress and buffer utilization metrics via `ImapiWriteProgressReporter`.

### `GeneratedContentComStream`
Implements the native COM `IStream` interface over a circular ring buffer:
- Total Ring Buffer Capacity: $64\,\text{MiB}$.
- Pre-fill Threshold: $32\,\text{MiB}$ before triggering laser start.
- Feeds synthesized sectors continuously from generation threads into the IMAPI2 driver without spooling gigantic multi-gigabyte temporary image files to disk.

### `BluRaySequentialWriter` & `BluRayMmcCommands`
Sends low-level SCSI / MMC (Multi-Media Commands) for Blu-ray streaming:
- Direct packet writing with 64 KiB (32-sector) cluster alignment.
- Handles TRACK RESERVATION, CLOSE TRACK/SESSION, and SYNCHRONIZE CACHE commands.

### `CdDataRawImageSession` & `CdRawSession`
Supports raw 2352-byte sector writing (DAO 96 raw / SAO) for bit-exact CD-R physical pit synthesis.

## 3. Supported Platforms
- Primary Engine: Windows IMAPI2 on Windows 10/11 x64/ARM64 (`net9.0-windows`).
- Cross-platform interface contracts (`IOpticalBurner`) prepared for upcoming Linux/macOS raw SCSI/MMC passthrough via `libburn` and `sg` devices.
