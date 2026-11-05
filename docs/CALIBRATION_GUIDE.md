# Calibration Guide — Disc Photography & Computer Vision

Calibration is critical to aligning synthesized radial patterns with the physical spiral tracks of an optical disc. Without calibration, manufactured stamper variations will cause radial lines to curve or warp.

---

## 1. Why Calibration is Required

Even discs adhering to the exact same standard (e.g. ECMA-267 DVD-R) exhibit slight variations:
1. **Inner Recorded Radius ($r_i$)**: May vary between $22.0\,\text{mm}$ and $24.5\,\text{mm}$ depending on drive lead-in positioning.
2. **Outer Recorded Radius ($r_o$)**: Ranges from $57.5\,\text{mm}$ to $58.2\,\text{mm}$.
3. **Non-linear Track Pitch ($p(x)$)**: Physical injection molding of polycarbonate substrates creates subtle thermal expansion gradients from center to outer rim.

A linear model causes visible tangential distortion at outer radii:

```
  Expected Radial Line           Distorted (Uncalibrated)
        \                                  )
         \                                /
          \                              /
           O (Center)                   O (Center)
```

By calibrating the 3rd-order polynomial $p(x) = p_0(1 + c_1 x + c_2 x^2 + c_3 x^3)$, radial lines stay laser-straight across the entire disc.

---

## 2. Calibration Workflows

Nawayasa Citramandala supports two calibration pathways:
1. **Automated Computer Vision Calibration** (Recommended).
2. **Manual Visual Anchor Calibration**.

---

## 3. Workflow A: Automated Computer Vision Calibration

The `NawayasaCitramandala.Vision` engine uses Hough Circle transformations and radial variance gradients to compute all geometric parameters automatically from a single photo.

### Step 1: Burn a Calibration Pattern
Burn a calibration target using the CLI:
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- cd-generate --preset CD_700MB --image assets/calibration_grid.png --out test_calibration.iso
```

### Step 2: Photograph the Disc
1. Place the burned disc on a flat, matte surface (white or black background).
2. Ensure even, diffuse lighting (avoid harsh directional glare).
3. Position your smartphone or scanner directly perpendicular to the disc face.
4. Ensure the center spindle hole and entire outer edge are clearly visible in the frame.
5. Save the photo as PNG or JPEG (e.g., `calibration_photo.png`).

### Step 3: Run Auto-Calibration
Execute the `autocalibrate` command:
```bash
dotnet run --project src/NawayasaCitramandala.Cli -- autocalibrate --photo calibration_photo.png --preset DVD_SL_4700MB
```

### Output Interpretation
The tool will output:
```text
=== Auto-Calibration Results ===
Image Size: 2400 x 2400 px
Spindle Center (x0, y0): (1200.4, 1198.8) px
Inner Track Radius (ri): 23.45 mm (Pixel Radius: 469 px)
Outer Track Radius (ro): 57.82 mm (Pixel Radius: 1156 px)
Pitch Coefficients:
  c1 = -0.0142
  c2 = +0.0031
  c3 = -0.0008
Calibration Quality Score: 96.4% (Excellent)
Preset updated and saved to: presets/custom_calibrated_dvd.json
```

---

## 4. Workflow B: Manual Visual Verification

If a photo has suboptimal lighting or uneven contrast, you can fine-tune coefficients manually using the JSON preset file:

1. Open `presets/custom_calibrated_dvd.json`:
   ```json
   {
     "Name": "Custom DVD-R Calibrated",
     "MediaType": "DVD_R",
     "InnerRadiusMm": 23.45,
     "OuterRadiusMm": 57.82,
     "PitchPolynomial": {
       "C1": -0.0142,
       "C2": 0.0031,
       "C3": -0.0008
     }
   }
   ```
2. If radial lines bend clockwise towards the outer edge, increase $c_1$ slightly (e.g. from $-0.0142$ to $-0.0100$).
3. If radial lines bend counter-clockwise, decrease $c_1$ (e.g. to $-0.0180$).
4. Re-burn a small 5-minute test sector to confirm straightness.

---

## 5. Best Practices for High Fidelity
- Always use the same brand/batch of blank media when calibrating. Different factories (e.g. CMC Magnetics, Ritek, Mitsubishi Kagaku Media) use different stamper molds.
- Keep burning speed moderate ($4\times$ or $8\times$ for DVD, $16\times$ for CD, $4\times$ for BD) to prevent spindle motor wobble and track wobble jitter.
