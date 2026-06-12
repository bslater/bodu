---
title: CRC catalogue
---

# CRC catalogue

The <xref:Bodu.IO.Hashing.Checksums.CrcStandard> type exposes a broad catalogue of named CRC parameter sets that can be passed to <xref:Bodu.IO.Hashing.Checksums.Crc> for CRC computation. The catalogue is mechanically derived from the **CRC RevEng** project.

## Attribution

The CRC parameter sets in this catalogue are sourced from **Greg Cook's CRC RevEng Catalogue of parametrised CRC algorithms** at [https://reveng.sourceforge.io/crc-catalogue/all.htm](https://reveng.sourceforge.io/crc-catalogue/all.htm).

The catalogue is distributed as part of the CRC RevEng project at <https://reveng.sourceforge.io/>. Please consult the upstream page for the authoritative parameter definitions, alias history, and licence terms that apply to the underlying data.

- **Catalogue last fetched (UTC):** 06/09/2026 06:48:50
- **This page regenerated (UTC):** 2026-06-09T06:49:30Z
- **Entries in source:** 113

## Accessing standards

The catalogue is a **lazy-materialised data table**. Loading <xref:Bodu.IO.Hashing.Checksums.CrcStandard> allocates only the packed spec rows and the per-entry cache slots — individual <xref:Bodu.IO.Hashing.Checksums.CrcStandard> instances are constructed on first access and then memoised, so a process that uses only a handful of standards pays for only a handful of allocations.

Three entry points:

```csharp
// 1. Strongly-typed common standards — most convenient for the usual suspects.
using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
using var crc = new Crc(CrcStandard.CRC32_ISCSI);          // iSCSI / Castagnoli
using var crc = new Crc(CrcStandard.CRC16_MODBUS);

// 2. By enum — covers every canonical catalogue entry (112 in total).
using var crc = new Crc(CrcStandard.Get(CrcStandards.CRC8_SAEJ1850));

// 3. By name — resolves canonical names AND published aliases.
using var crc1 = new Crc(CrcStandard.FromName("CRC-32/ISO-HDLC"));
using var crc2 = new Crc(CrcStandard.FromName("PKZIP"));   // same instance as crc1

// Iterate every catalogue standard
foreach (CrcStandard std in CrcStandard.All) { ... }
```

`FromName` is ordinal and case-sensitive. `TryFromName` returns `false` rather than throwing when a name is unknown.

## Support policy

`CrcStandard` represents all scalar parameters as <xref:System.UInt64>, so the library can materialise any CRC of width 1–64 bits. Entries whose width exceeds 64 bits are listed below for completeness but are **not** exposed by <xref:Bodu.IO.Hashing.Checksums.CrcStandards> and cannot be constructed through `CrcStandard`.

Aliases share a single catalogue instance with their canonical standard. `CrcStandard.FromName` resolves both canonical and alias names, so `FromName("CRC-32")` and `FromName("CRC-32/ISO-HDLC")` return the same instance.

## Common standards (strongly-typed)

These are exposed as `public static CrcStandard` properties on <xref:Bodu.IO.Hashing.Checksums.CrcStandard> for convenience — the underlying cache is still shared with the enum-based lookup.

| Name | Width | Property | Aliases |
|---|---:|---|---|
| CRC-8/MAXIM-DOW | 8 | `CrcStandard.CRC8_MAXIMDOW` | — |
| CRC-8/SMBUS | 8 | `CrcStandard.CRC8_SMBUS` | — |
| CRC-16/ARC | 16 | `CrcStandard.CRC16_ARC` | — |
| CRC-16/IBM-3740 | 16 | `CrcStandard.CRC16_IBM3740` | — |
| CRC-16/KERMIT | 16 | `CrcStandard.CRC16_KERMIT` | — |
| CRC-16/MODBUS | 16 | `CrcStandard.CRC16_MODBUS` | — |
| CRC-16/XMODEM | 16 | `CrcStandard.CRC16_XMODEM` | — |
| CRC-32/BZIP2 | 32 | `CrcStandard.CRC32_BZIP2` | — |
| CRC-32/ISCSI | 32 | `CrcStandard.CRC32_ISCSI` | — |
| CRC-32/ISO-HDLC | 32 | `CrcStandard.CRC32_ISOHDLC` | — |
| CRC-64/ECMA-182 | 64 | `CrcStandard.CRC64_ECMA182` | — |
| CRC-64/XZ | 64 | `CrcStandard.CRC64_XZ` | — |

## Full catalogue

Access the following via `CrcStandard.Get(CrcStandards.X)` or `CrcStandard.FromName("name")`.

| Name | Width | Class | Enum | Aliases | RevEng |
|---|---:|---|---|---|---|
| CRC-3/GSM | 3 |  | `CrcStandards.CRC3_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-3-gsm) |
| CRC-3/ROHC | 3 |  | `CrcStandards.CRC3_ROHC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-3-rohc) |
| CRC-4/G-704 | 4 |  | `CrcStandards.CRC4_G704` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-4-g-704) |
| CRC-4/INTERLAKEN | 4 |  | `CrcStandards.CRC4_INTERLAKEN` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-4-interlaken) |
| CRC-5/EPC-C1G2 | 5 |  | `CrcStandards.CRC5_EPCC1G2` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-5-epc-c1g2) |
| CRC-5/G-704 | 5 |  | `CrcStandards.CRC5_G704` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-5-g-704) |
| CRC-5/USB | 5 |  | `CrcStandards.CRC5_USB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-5-usb) |
| CRC-6/CDMA2000-A | 6 |  | `CrcStandards.CRC6_CDMA2000A` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-cdma2000-a) |
| CRC-6/CDMA2000-B | 6 |  | `CrcStandards.CRC6_CDMA2000B` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-cdma2000-b) |
| CRC-6/DARC | 6 |  | `CrcStandards.CRC6_DARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-darc) |
| CRC-6/G-704 | 6 |  | `CrcStandards.CRC6_G704` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-g-704) |
| CRC-6/GSM | 6 |  | `CrcStandards.CRC6_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-gsm) |
| CRC-7/MMC | 7 |  | `CrcStandards.CRC7_MMC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-7-mmc) |
| CRC-7/ROHC | 7 |  | `CrcStandards.CRC7_ROHC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-7-rohc) |
| CRC-7/UMTS | 7 |  | `CrcStandards.CRC7_UMTS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-7-umts) |
| CRC-8/AUTOSAR | 8 |  | `CrcStandards.CRC8_AUTOSAR` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-autosar) |
| CRC-8/BLUETOOTH | 8 |  | `CrcStandards.CRC8_BLUETOOTH` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-bluetooth) |
| CRC-8/CDMA2000 | 8 |  | `CrcStandards.CRC8_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-cdma2000) |
| CRC-8/DARC | 8 |  | `CrcStandards.CRC8_DARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-darc) |
| CRC-8/DVB-S2 | 8 |  | `CrcStandards.CRC8_DVBS2` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-dvb-s2) |
| CRC-8/GSM-A | 8 |  | `CrcStandards.CRC8_GSMA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-gsm-a) |
| CRC-8/GSM-B | 8 |  | `CrcStandards.CRC8_GSMB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-gsm-b) |
| CRC-8/HITAG | 8 |  | `CrcStandards.CRC8_HITAG` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-hitag) |
| CRC-8/I-432-1 | 8 |  | `CrcStandards.CRC8_I4321` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-i-432-1) |
| CRC-8/I-CODE | 8 |  | `CrcStandards.CRC8_ICODE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-i-code) |
| CRC-8/LTE | 8 |  | `CrcStandards.CRC8_LTE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-lte) |
| **CRC-8/MAXIM-DOW** | 8 |  | `CrcStandards.CRC8_MAXIMDOW` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-maxim-dow) |
| CRC-8/MIFARE-MAD | 8 |  | `CrcStandards.CRC8_MIFAREMAD` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-mifare-mad) |
| CRC-8/NRSC-5 | 8 |  | `CrcStandards.CRC8_NRSC5` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-nrsc-5) |
| CRC-8/OPENSAFETY | 8 |  | `CrcStandards.CRC8_OPENSAFETY` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-opensafety) |
| CRC-8/ROHC | 8 |  | `CrcStandards.CRC8_ROHC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-rohc) |
| CRC-8/SAE-J1850 | 8 |  | `CrcStandards.CRC8_SAEJ1850` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-sae-j1850) |
| **CRC-8/SMBUS** | 8 |  | `CrcStandards.CRC8_SMBUS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-smbus) |
| CRC-8/TECH-3250 | 8 |  | `CrcStandards.CRC8_TECH3250` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-tech-3250) |
| CRC-8/WCDMA | 8 |  | `CrcStandards.CRC8_WCDMA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-wcdma) |
| CRC-10/ATM | 10 |  | `CrcStandards.CRC10_ATM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-10-atm) |
| CRC-10/CDMA2000 | 10 |  | `CrcStandards.CRC10_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-10-cdma2000) |
| CRC-10/GSM | 10 |  | `CrcStandards.CRC10_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-10-gsm) |
| CRC-11/FLEXRAY | 11 |  | `CrcStandards.CRC11_FLEXRAY` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-11-flexray) |
| CRC-11/UMTS | 11 |  | `CrcStandards.CRC11_UMTS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-11-umts) |
| CRC-12/CDMA2000 | 12 |  | `CrcStandards.CRC12_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-cdma2000) |
| CRC-12/DECT | 12 |  | `CrcStandards.CRC12_DECT` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-dect) |
| CRC-12/GSM | 12 |  | `CrcStandards.CRC12_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-gsm) |
| CRC-12/UMTS | 12 |  | `CrcStandards.CRC12_UMTS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-umts) |
| CRC-13/BBC | 13 |  | `CrcStandards.CRC13_BBC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-13-bbc) |
| CRC-14/DARC | 14 |  | `CrcStandards.CRC14_DARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-14-darc) |
| CRC-14/GSM | 14 |  | `CrcStandards.CRC14_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-14-gsm) |
| CRC-15/CAN | 15 |  | `CrcStandards.CRC15_CAN` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-15-can) |
| CRC-15/MPT1327 | 15 |  | `CrcStandards.CRC15_MPT1327` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-15-mpt1327) |
| **CRC-16/ARC** | 16 |  | `CrcStandards.CRC16_ARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-arc) |
| CRC-16/CDMA2000 | 16 |  | `CrcStandards.CRC16_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-cdma2000) |
| CRC-16/CMS | 16 |  | `CrcStandards.CRC16_CMS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-cms) |
| CRC-16/DDS-110 | 16 |  | `CrcStandards.CRC16_DDS110` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dds-110) |
| CRC-16/DECT-R | 16 |  | `CrcStandards.CRC16_DECTR` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dect-r) |
| CRC-16/DECT-X | 16 |  | `CrcStandards.CRC16_DECTX` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dect-x) |
| CRC-16/DNP | 16 |  | `CrcStandards.CRC16_DNP` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dnp) |
| CRC-16/EN-13757 | 16 |  | `CrcStandards.CRC16_EN13757` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-en-13757) |
| CRC-16/GENIBUS | 16 |  | `CrcStandards.CRC16_GENIBUS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-genibus) |
| CRC-16/GSM | 16 |  | `CrcStandards.CRC16_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-gsm) |
| **CRC-16/IBM-3740** | 16 |  | `CrcStandards.CRC16_IBM3740` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-ibm-3740) |
| CRC-16/IBM-SDLC | 16 |  | `CrcStandards.CRC16_IBMSDLC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-ibm-sdlc) |
| CRC-16/ISO-IEC-14443-3-A | 16 |  | `CrcStandards.CRC16_ISOIEC144433A` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-iso-iec-14443-3-a) |
| **CRC-16/KERMIT** | 16 |  | `CrcStandards.CRC16_KERMIT` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-kermit) |
| CRC-16/LJ1200 | 16 |  | `CrcStandards.CRC16_LJ1200` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-lj1200) |
| CRC-16/M17 | 16 |  | `CrcStandards.CRC16_M17` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-m17) |
| CRC-16/MAXIM-DOW | 16 |  | `CrcStandards.CRC16_MAXIMDOW` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-maxim-dow) |
| CRC-16/MCRF4XX | 16 |  | `CrcStandards.CRC16_MCRF4XX` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-mcrf4xx) |
| **CRC-16/MODBUS** | 16 |  | `CrcStandards.CRC16_MODBUS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-modbus) |
| CRC-16/NRSC-5 | 16 |  | `CrcStandards.CRC16_NRSC5` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-nrsc-5) |
| CRC-16/OPENSAFETY-A | 16 |  | `CrcStandards.CRC16_OPENSAFETYA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-opensafety-a) |
| CRC-16/OPENSAFETY-B | 16 |  | `CrcStandards.CRC16_OPENSAFETYB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-opensafety-b) |
| CRC-16/PROFIBUS | 16 |  | `CrcStandards.CRC16_PROFIBUS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-profibus) |
| CRC-16/RIELLO | 16 |  | `CrcStandards.CRC16_RIELLO` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-riello) |
| CRC-16/SPI-FUJITSU | 16 |  | `CrcStandards.CRC16_SPIFUJITSU` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-spi-fujitsu) |
| CRC-16/T10-DIF | 16 |  | `CrcStandards.CRC16_T10DIF` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-t10-dif) |
| CRC-16/TELEDISK | 16 |  | `CrcStandards.CRC16_TELEDISK` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-teledisk) |
| CRC-16/TMS37157 | 16 |  | `CrcStandards.CRC16_TMS37157` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-tms37157) |
| CRC-16/UMTS | 16 |  | `CrcStandards.CRC16_UMTS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-umts) |
| CRC-16/USB | 16 |  | `CrcStandards.CRC16_USB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-usb) |
| **CRC-16/XMODEM** | 16 |  | `CrcStandards.CRC16_XMODEM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-xmodem) |
| CRC-17/CAN-FD | 17 |  | `CrcStandards.CRC17_CANFD` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-17-can-fd) |
| CRC-21/CAN-FD | 21 |  | `CrcStandards.CRC21_CANFD` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-21-can-fd) |
| CRC-24/BLE | 24 |  | `CrcStandards.CRC24_BLE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-ble) |
| CRC-24/FLEXRAY-A | 24 |  | `CrcStandards.CRC24_FLEXRAYA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-flexray-a) |
| CRC-24/FLEXRAY-B | 24 |  | `CrcStandards.CRC24_FLEXRAYB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-flexray-b) |
| CRC-24/INTERLAKEN | 24 |  | `CrcStandards.CRC24_INTERLAKEN` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-interlaken) |
| CRC-24/LTE-A | 24 |  | `CrcStandards.CRC24_LTEA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-lte-a) |
| CRC-24/LTE-B | 24 |  | `CrcStandards.CRC24_LTEB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-lte-b) |
| CRC-24/OPENPGP | 24 |  | `CrcStandards.CRC24_OPENPGP` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-openpgp) |
| CRC-24/OS-9 | 24 |  | `CrcStandards.CRC24_OS9` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-os-9) |
| CRC-30/CDMA | 30 |  | `CrcStandards.CRC30_CDMA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-30-cdma) |
| CRC-31/PHILIPS | 31 |  | `CrcStandards.CRC31_PHILIPS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-31-philips) |
| CRC-32/AIXM | 32 |  | `CrcStandards.CRC32_AIXM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-aixm) |
| CRC-32/AUTOSAR | 32 |  | `CrcStandards.CRC32_AUTOSAR` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-autosar) |
| CRC-32/BASE91-D | 32 |  | `CrcStandards.CRC32_BASE91D` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-base91-d) |
| **CRC-32/BZIP2** | 32 |  | `CrcStandards.CRC32_BZIP2` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-bzip2) |
| CRC-32/CD-ROM-EDC | 32 |  | `CrcStandards.CRC32_CDROMEDC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-cd-rom-edc) |
| CRC-32/CKSUM | 32 |  | `CrcStandards.CRC32_CKSUM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-cksum) |
| **CRC-32/ISCSI** | 32 |  | `CrcStandards.CRC32_ISCSI` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-iscsi) |
| **CRC-32/ISO-HDLC** | 32 |  | `CrcStandards.CRC32_ISOHDLC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-iso-hdlc) |
| CRC-32/JAMCRC | 32 |  | `CrcStandards.CRC32_JAMCRC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-jamcrc) |
| CRC-32/MEF | 32 |  | `CrcStandards.CRC32_MEF` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-mef) |
| CRC-32/MPEG-2 | 32 |  | `CrcStandards.CRC32_MPEG2` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-mpeg-2) |
| CRC-32/XFER | 32 |  | `CrcStandards.CRC32_XFER` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-xfer) |
| CRC-40/GSM | 40 |  | `CrcStandards.CRC40_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-40-gsm) |
| **CRC-64/ECMA-182** | 64 |  | `CrcStandards.CRC64_ECMA182` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-ecma-182) |
| CRC-64/GO-ISO | 64 |  | `CrcStandards.CRC64_GOISO` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-go-iso) |
| CRC-64/MS | 64 |  | `CrcStandards.CRC64_MS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-ms) |
| CRC-64/NVME | 64 |  | `CrcStandards.CRC64_NVME` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-nvme) |
| CRC-64/REDIS | 64 |  | `CrcStandards.CRC64_REDIS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-redis) |
| CRC-64/WE | 64 |  | `CrcStandards.CRC64_WE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-we) |
| **CRC-64/XZ** | 64 |  | `CrcStandards.CRC64_XZ` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-xz) |

## Not supported (width exceeds 64 bits)

The following standards are listed in the source catalogue but are **not** exposed by `CrcStandard` because their width exceeds the 64-bit scalar representation used by this library.

| Name | Width | Class | RevEng |
|---|---:|---|---|
| CRC-82/DARC | 82 |  | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-82-darc) |

## Regeneration

This page and the generated C# sources are produced by the scripts in `tools/`. To refresh the data from upstream:

```pwsh
pwsh ./tools/Fetch-CrcSpecs.ps1
pwsh ./tools/Generate-CrcCatalog.ps1        # regenerates CrcStandards.cs and CrcStandard.Catalog.cs
pwsh ./tools/Generate-CrcCatalogTests.ps1   # regenerates CrcTests.Catalog.cs
pwsh ./tools/Generate-CrcDocs.ps1           # regenerates this page
```


## See also

- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
- **[Bodu.Security.Cryptography guides overview](index.md)** — every guide in this library.
