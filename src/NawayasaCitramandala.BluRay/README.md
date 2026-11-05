# NawayasaCitramandala.BluRay

Blu-ray (BD-R 25 GB) 17PP RLL(1,7) parity-preserve modulation, DC control bit engine, and UDF 2.01 generator.

## 1. Overview
The `NawayasaCitramandala.BluRay` module synthesizes visible images on single-layer BD-R (25.0 GB) optical media adhering to ISO/IEC 30190:2021. It manages:
- 17PP $(1,7)$ Run-Length Limited modulation.
- Greedy DC control bit insertion to prevent low-frequency drift.
- 3T vs 6T pit/land length distribution bias.
- Recording Unit Block (RUB) 496-frame clustering.
- LDC (Long Distance Code) and BIS (Burst Indicator Subcode) layout.

## 2. Key Components

### `BluRay17PpEncoder`
Implements the 17PP modulation rules:
- Min run length: $2T$ (at least one '0' between adjacent '1's).
- Max run length: $8T$ (at most seven '0's between adjacent '1's).
- Parity Preserve: Maintains equal bit parity between source word and channel word.

### `BluRayGreedyDcControlEncoder`
Dynamically inserts a DC control bit every 45 channel bits:
- Evaluates both bit choices ($0$ and $1$) using lookahead simulation.
- Selects the bit that brings the cumulative Digital Sum Value (DSV) closest to zero.
- Ensures carrier tracking stability across all standard BD-RE/BD-R optical drives.

### `BluRayImageMapping` & `BluRayRunLengthMapping`
Creates visual contrast by biasing the run-length distribution:
- **3T Dominant (Byte `0xAA`)**: Generates $85.4\%$ short 3T channel marks. Creates high light scattering.
- **6T Dominant (Byte `0x44`)**: Generates $62.7\%$ long 6T channel marks. Creates specular mirror-like reflections.

### `BluRayIsoGenerator` & `BluRayStreamingGenerator`
Packages physical clusters into valid UDF 2.01 optical images with optional direct streaming support.

## 3. Specifications
- Track Pitch: $0.32\,\mu\text{m}$.
- Laser Wavelength: $405\,\text{nm}$ (Blue-violet).
- Sector Size: 2,048 bytes user data.
- RUB Cluster Size: 32 sectors ($64\,\text{KiB}$ user payload, 496 physical recording frames).
