# NawayasaCitramandala.Tests

Automated test suite and regression verification for **Nawayasa Citramandala**.

## 1. Overview
The test suite validates the mathematical foundations, physical layer constraints, image dithering algorithms, and computer vision auto-calibration pipelines.

## 2. Test Suites & Coverage

| Test Class | Target Module | Scope & Invariants Tested |
| :--- | :--- | :--- |
| `CdPhysicsTests` | `NawayasaCitramandala.Cd` | CIRC latency schedule (111-frame delay), PCM audio sample byte swaps, contrast byte polarity (`0x0D` vs `0xAA`). |
| `DvdPhysicsTests` | `NawayasaCitramandala.Dvd` | Galois Field $GF(2^8)$ multiplication tables, RS-PC PO/PI matrix generation, LFSR scrambler inversion ($P = Q \oplus \text{mask}$), EFMPlus transition density jump counts ($0xA5$ dense vs $0x92$ sparse). |
| `BluRayPhysicsTests` | `NawayasaCitramandala.BluRay` | 17PP RLL(1,7) constraint validation (no adjacent 1s, max run 8T), greedy DC control bit selection, 3T vs 6T run-length distribution ratios. |
| `VisionAndDitherTests` | `Vision` & `Imaging` | Synthetic disc Hough circle detection, spindle center tolerance ($\le 1.0\,\text{px}$), radius recovery, and Blue Noise Void-and-Cluster matrix energy dispersion. |

## 3. Running Tests

Execute all unit tests using the standard .NET CLI:
```bash
dotnet test
```

To run with verbose test output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

## 4. Current Test Status
- Total Tests: 11
- Passed: 11
- Failed: 0
- Skipped: 0
- Pass Rate: 100%
