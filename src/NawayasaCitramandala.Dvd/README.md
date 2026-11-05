# NawayasaCitramandala.Dvd

DVD-R physical sector generator, RS-PC Reed-Solomon Product Code engine, and EFMPlus dispersion modulator.

## 1. Overview
The `NawayasaCitramandala.Dvd` module is the heart of DVD-R optical image synthesis. It implements the complete ECMA-267 and ECMA-359 DVD physical channel pipeline in reverse:
1. Polar spatial image rasterization.
2. EFMPlus 4-state transition density selection.
3. LFSR scrambler inversion ($P = Q \oplus \text{mask}$).
4. RS-PC Product Code parity calculation over Galois Field $GF(2^8)$.
5. Output format generation (ISO image or streaming buffer).

## 2. Key Components

### `DvdSolverEngine`
The central reverse-constraint solver:
- Evaluates the 4-state EFMPlus automaton to enforce maximum visual contrast.
- In **Dispersion Mode**:
  - Modulated Byte $Q = 0xA5$: 5 transitions per 16-bit channel word $\rightarrow$ High optical diffusion (Dark).
  - Modulated Byte $Q = 0x92$: 2 transitions per 16-bit channel word $\rightarrow$ Low optical diffusion (Bright).
- Reverses the 15-bit LFSR pseudo-random scrambler to compute the exact unmodulated user sector data $P_k = Q_k \oplus S_k$.

### `DvdReedSolomon` & `DvdEccEncoder`
Computes the 2D Product Code parity bytes across each 16-sector ECC block ($32,768$ user bytes):
- **Parity Outer (PO)**: $RS(208, 192, 17)$ along columns.
- **Parity Inner (PI)**: $RS(182, 172, 11)$ along rows.
- Uses optimized $GF(2^8)$ log/antilog table multiplication for microsecond throughput.

### `DvdStreamingGenerator`
Streams generated sectors sequentially on-the-fly, avoiding the need to write multi-gigabyte ISO files to disk when burning directly to hardware.

## 3. Specifications
- Sector Size: 2,048 user data bytes.
- ECC Block: 16 sectors ($37,856$ total physical bytes including sync, PO, and PI).
- Track Pitch: $\approx 0.74\,\mu\text{m}$.
- Target Media: DVD-R Single Layer (4.7 GB).
