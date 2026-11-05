# NawayasaCitramandala.Iso

ISO9660, Joliet, and UDF 2.01 optical filesystem generators with radial partition placement.

## 1. Overview
The `NawayasaCitramandala.Iso` module generates valid optical disc filesystem images that can simultaneously store user data files and embed visual pattern sectors. It supports:
- **ISO9660 (ECMA-119)** Level 1 and Level 2 directory hierarchies.
- **Joliet** Unicode extensions (UCS-2 BE filenames up to 64 characters).
- **UDF 2.01 (ECMA-167)** filesystem structures for DVD and Blu-ray compatibility.
- **Hybrid Radial Zone Partitioning**: Distributing data files into non-rendered radial zones (inner hub or outer rim) while allocating user-specified sectors to visual artwork.

## 2. Key Components

### `Iso9660ImageWriter`
Generates byte-perfect ISO9660 / Joliet filesystem structures:
- Primary Volume Descriptor (PVD).
- Supplementary Volume Descriptor (SVD) for Joliet.
- Path Table records (Type L and Type M).
- Directory Record trees with interleaved extents.
- Volume Descriptor Set Terminator.

### `UdfImageWriter`
Implements UDF 2.01 specification:
- Anchor Volume Descriptor Pointer (AVDP) at sector 256.
- Main Volume Descriptor Sequence (VDS) and Reserve VDS.
- Logical Volume Descriptor (LVD) and Partition Descriptor (PD).
- File Set Descriptor (FSD) and File Entry records (ICB tag 261).
- Allocation Descriptors (short and long ad) mapping file data extents.

## 3. Radial Zone Allocation
When burning visual discs, real data files can be positioned at:
- `InnerHub`: Files written to LBAs corresponding to radii below the visual artwork area.
- `OuterRim`: Files written to LBAs beyond the visual artwork outer boundary.
- `Interspersed`: Transparently padding visual sector blocks between file extents.
