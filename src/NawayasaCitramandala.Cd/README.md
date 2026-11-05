# NawayasaCitramandala.Cd

Compact Disc (CD-R) physical-layer visual synthesis engine, Red Book audio generator, and CIRC latency compensator.

## 1. Overview
The `NawayasaCitramandala.Cd` module synthesizes visible images on standard CD-R media. It operates in two main modes:
1. **Audio CD-DA Mode**: Encodes visual pixel data into 16-bit 44.1 kHz PCM audio samples (`.wav`) accompanied by a standard cue sheet (`.cue`).
2. **Mode 1 Data CD Mode**: Packs visual payloads into 2048-byte user data sectors with Layer 3 ECC and EDC checksums.

## 2. Key Components

### `CddaInterleaver` & `CddaInterleaveTable`
Implements the inverse Cross-Interleaved Reed-Solomon Code (CIRC) delay schedule:
- Standard Red Book CIRC introduces an asymmetric delay of up to 111 F2 frames across C1 and C2 decoders ($\approx 15.1\,\text{ms}$, or $\approx 18.1\,\text{mm}$ physical arc length).
- `CddaInterleaver` permutes PCM audio samples prior to writing, so that after the drive's hardware encoder applies forward CIRC interleaving, the physical pit/land bursts land precisely at the intended polar coordinates $(r, \theta)$.

### `CdTrackGenerator` & `CdBlockPipeline`
- Maps input 2D image pixels to spiral LBA positions using Archimedean polar coordinates.
- Selects byte contrast polarity:
  - Byte `0x0D` (EFM channel symbol `01000100100000`): Dark visual tone.
  - Byte `0xAA` (EFM channel symbol `10010000001000`): Bright visual tone.
- Compensates for byte endianness (`CdAudioByteOrder`: Little-Endian vs Big-Endian sample swap).

### `CdWaveFile`
Generates RIFF/WAVE header containers and writes PCM audio blocks with zero compression artifacts.

## 3. Specifications
- Audio Sampling Rate: $44,100\,\text{Hz}$, 16-bit stereo PCM.
- Sector Size: 2352 bytes (raw audio frame) / 2048 bytes (Mode 1 user data).
- Sector Rate: 75 sectors per second (1x speed standard).
