# Mathematical Foundations & Physical-Layer Modulation

This document details the exact mathematical, geometric, and physical-layer modulation models implemented in **Nawayasa Citramandala**.

---

## 1. Archimedean Spiral & 3rd-Order Pitch Polynomial

Optical discs record information along a continuous outward spiral track starting from an inner radius $r_i$ to an outer radius $r_o$.

### 1.1 Differential Spiral Equation
For an ideal Archimedean spiral with constant track pitch $p$, the radius as a function of total angle $\theta$ is:
$$r(\theta) = r_i + \frac{p}{2\pi} \theta \iff \frac{dr}{d\theta} = \frac{p}{2\pi}$$

In physical reality, stamper replication and thermal expansion introduce non-uniform pitch variation. We model the track pitch as a 3rd-order polynomial:
$$p(x) = p_0 \left(1 + c_1 x + c_2 x^2 + c_3 x^3\right)$$
where:
- $x = \frac{r - r_i}{r_o - r_i} \in [0, 1]$ is the normalized radial position.
- $p_0$ is the fundamental base pitch (typically $\approx 0.74\,\mu\text{m}$ for DVD, $1.6\,\mu\text{m}$ for CD, $0.32\,\mu\text{m}$ for BD).
- $c_1, c_2, c_3$ are dimensionless polynomial coefficients obtained through calibration.

### 1.2 Angle Integration & Sector Coordinate Mapping
The cumulative angular displacement $\theta(r)$ is given by the integral:
$$\theta(r) = 2\pi \int_{r_i}^r \frac{dr'}{p(r')}$$

The total linear track length $L(r)$ traversed from $r_i$ to radius $r$ is:
$$dL = \sqrt{dr^2 + (r \, d\theta)^2} = \sqrt{1 + \left(\frac{2\pi r}{p(r)}\right)^2} dr \approx \frac{2\pi r}{p(r)} dr$$
since $\frac{2\pi r}{p(r)} \gg 1$ for all optical disc radii ($r \ge 22\,\text{mm}, p \le 1.6\,\mu\text{m}$).

Given scanning linear velocity $v$ or channel bit length $T_b$, the physical sector index (PSN / LBA) corresponding to radius $r$ is:
$$\text{LBA}(r) = \left\lfloor \frac{L(r)}{S_{\text{sector}}} \right\rfloor$$
where $S_{\text{sector}}$ is the physical arc length of a single sector.

---

## 2. Compact Disc (CD) — EFM & CIRC Latency

### 2.1 Eight-to-Fourteen Modulation (EFM)
- Standard: ECMA-130 / Red Book.
- Each 8-bit user byte is converted into a 14-bit channel symbol satisfying the $(2, 10)$ run-length limited (RLL) constraint (minimum run length of 3 channel bits, maximum of 11 channel bits).
- 3 merging bits are inserted between successive 14-bit symbols to maintain DSV (Digital Sum Value) near zero.

### 2.2 Byte Reflection Polarity
Experimental spectroscopy reveals that pit and land duty cycles differ based on the modulated byte:
- `0x0D` (channel symbol `01000100100000`): Produces high pit-to-land ratio, reflecting less light $\rightarrow$ **Dark Region**.
- `0xAA` (channel symbol `10010000001000`): Produces low transition density and long lands, reflecting more light $\rightarrow$ **Bright Region**.

### 2.3 CIRC Interleaving Delay Model
CIRC (Cross-Interleaved Reed-Solomon Code) applies staggered delays across 28-byte F1 frames and 32-byte F2 frames:
$$\Delta(i) = i \cdot D, \quad D = 4 \text{ frames}, \quad i \in [0, 27]$$
The maximum latency across both C1 and C2 stages is:
$$\text{Latency}_{\max} = 111 \text{ frames}$$
At 7,350 frames/sec (audio sample rate $44.1\,\text{kHz} \times 16\,\text{bit} \times 2\,\text{channels} = 588$ channel samples/frame):
$$t_{\text{latency}} = \frac{111}{7350} \approx 15.102\,\text{ms}$$
At linear velocity $v = 1.2\,\text{m/s}$, this produces a physical arc shift along the spiral:
$$\Delta s = 1.2 \times 0.015102 \approx 18.122\,\text{mm}$$
Nawayasa Citramandala applies an inverse latency permutation to audio samples before CIRC encoding, ensuring that physical pit positions align strictly with target coordinates $(r, \theta)$.

---

## 3. DVD-R — EFMPlus & RS-PC Product Code

### 3.1 16-Sector ECC Block Structure
- Standard: ECMA-267.
- A physical ECC block contains 16 sectors ($16 \times 2048 = 32,768$ user bytes).
- 2D Reed-Solomon Product Code (RS-PC):
  - **PO (Parity Outer)**: $RS(208, 192, 17)$ along columns.
  - **PI (Parity Inner)**: $RS(182, 172, 11)$ along rows.

```
       172 bytes (Data)          10 bytes (PI)
   +---------------------------+------------+
192|                           |            |
row|        Data Block         |  PO on PI  |
 s |                           |            |
   +---------------------------+------------+
 16|       PO Parity           |  PO on PI  |
row|                           |            |
 s +---------------------------+------------+
```

### 3.2 LFSR Scrambler & Inversion
To prevent repetitive DC patterns, data bytes $P_k$ are scrambled with an 8-bit pseudo-random sequence generated from a 15-bit shift register:
$$Q_k = P_k \oplus S_k$$
where $S_k$ is the LFSR mask determined by the sector's lower 4 bits of ID:
$$\text{ID} \pmod{16}$$
In Nawayasa Citramandala, to force the physical disc to contain desired modulation symbol $Q_k$, the host sector payload is inverted prior to writing:
$$P_k = Q_k \oplus S_k$$

### 3.3 EFMPlus Dispersion Modulation
EFMPlus maps 8-bit bytes to 16-bit channel words via a 4-state finite state machine (FSM).
We select target modulated symbols $Q$ that maximize visual contrast:
- **$Q = 0xA5$**: Produces an average of 5 NRZI transitions per 16-bit symbol. Creates dense laser burns that disperse light heavily $\rightarrow$ **Dark tone**.
- **$Q = 0x92$**: Produces only 2 NRZI transitions per 16-bit symbol. Creates long unbroken pit/land structures that reflect coherent light $\rightarrow$ **Bright tone**.

---

## 4. Blu-ray (BD-R 25 GB) — 17PP Modulation & Run-Length Bias

### 4.1 17PP Modulation Rules
- Standard: ISO/IEC 30190:2021.
- RLL constraint: $(1, 7)$ — minimum run length is $2T$, maximum is $8T$.
- Parity-preserving property: The number of '1's in the channel code has the same parity as the data word, maintaining predictable DSV.

### 4.2 Greedy DC Bit Insertion
Every 45 channel bits, a DC control bit is dynamically chosen ($0$ or $1$) to minimize the running Digital Sum Value:
$$\text{DSV}(k) = \sum_{j=1}^k w_j, \quad w_j \in \{-1, +1\}$$
This guarantees carrier stability in standard BD-R drives without causing tracking errors.

### 4.3 3T vs 6T Run-Length Distribution
By carefully crafting the user payload $P$:
- Byte value `0xAA`: After 17PP mapping, yields **85.4% 3T run-lengths** (microscopic short pits/lands).
- Byte value `0x44`: Yields **62.7% 6T run-lengths** (extended macroscopic pits/lands).
The stark difference in diffraction efficiency between 3T and 6T structures produces sharp, high-contrast naked-eye imagery.

---

## 5. Tone Shading & Spatial Dithering

### 5.1 Void-and-Cluster Blue Noise
Unlike Floyd-Steinberg error diffusion which produces correlated "worm-like" artifacts along diagonal spirals, our Void-and-Cluster blue-noise matrix ($64 \times 64$ toroidal tile) exhibits pure high-frequency spatial noise:
$$S(f) \propto f^\alpha \quad (\alpha > 0)$$
No low-frequency clustering occurs, producing a smooth velvet texture on reflective disc surfaces.

### 5.2 Dye Absorption Modeling
Different chemical recording layers react differently to laser power:
$$\text{Reflectance}(D) = R_{\min} + (R_{\max} - R_{\min}) \cdot \left(\frac{D}{255}\right)^\gamma$$
- **Azo (Dark Blue)**: $\gamma \approx 1.22$
- **Phthalocyanine (Silver-Gold)**: $\gamma \approx 0.95$
- **Cyanine (Light Blue/Green)**: $\gamma \approx 1.08$
- **BD-R HTL (Inorganic Cu/Si)**: $\gamma \approx 1.40$
- **BD-R LTH (Organic)**: $\gamma \approx 1.15$
The `DyeProfile` engine automatically pre-compensates pixel intensities based on the selected media dye profile.
