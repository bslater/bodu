---
title: CRC catalogue
---

# CRC catalogue

The <xref:Bodu.IO.Hashing.Checksums.CrcStandard> type (in the **Bodu.IO.Hashing** package) exposes a broad catalogue of named CRC parameter sets that can be passed to <xref:Bodu.IO.Hashing.Checksums.Crc> for CRC computation. The catalogue is mechanically derived from the **CRC RevEng** project.

For walk-through usage of the CRC engine, see [Using CRC](crc.md).

## Attribution

The CRC parameter sets in this catalogue are sourced from **Greg Cook's CRC RevEng Catalogue of parameterized CRC algorithms** at [https://reveng.sourceforge.io/crc-catalogue/all.htm](https://reveng.sourceforge.io/crc-catalogue/all.htm).

The catalogue is distributed as part of the CRC RevEng project at <https://reveng.sourceforge.io/>. Please consult the upstream page for the authoritative parameter definitions, alias history, and license terms that apply to the underlying data.

- **Catalogue last fetched (UTC):** 2026-04-20T08:57:51Z
- **This page regenerated (UTC):** 2026-04-20T22:24:42Z
- **Entries in source:** 113

## Accessing standards

The catalogue is a **lazy-materialized data table**. Loading <xref:Bodu.IO.Hashing.Checksums.CrcStandard> allocates only the packed spec rows and the per-entry cache slots — individual <xref:Bodu.IO.Hashing.Checksums.CrcStandard> instances are constructed on first access and then memoized, so a process that uses only a handful of standards pays for only a handful of allocations.

Three entry points:

```csharp
// 1. Strongly-typed common standards — most convenient for the usual suspects.
var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
var crc = new Crc(CrcStandard.CRC32_ISCSI);          // iSCSI / Castagnoli
var crc = new Crc(CrcStandard.CRC16_MODBUS);

// 2. By enum — covers every canonical catalogue entry (112 in total).
var crc = new Crc(CrcStandard.Get(CrcStandards.CRC8_SAEJ1850));

// 3. By name — resolves canonical names AND published aliases.
var crc1 = new Crc(CrcStandard.FromName("CRC-32/ISO-HDLC"));
var crc2 = new Crc(CrcStandard.FromName("PKZIP"));   // same instance as crc1

// Iterate every catalogue standard
foreach (CrcStandard std in CrcStandard.All) { ... }
```

`FromName` is ordinal and case-sensitive. `TryFromName` returns `false` rather than throwing when a name is unknown.

## Support policy

`CrcStandard` represents all scalar parameters as <xref:System.UInt64>, so the library can materialize any CRC of width 1–64 bits. Entries whose width exceeds 64 bits are listed below for completeness but are **not** exposed by <xref:Bodu.IO.Hashing.Checksums.CrcStandards> and cannot be constructed through `CrcStandard`.

Aliases share a single catalogue instance with their canonical standard. `CrcStandard.FromName` resolves both canonical and alias names, so `FromName("CRC-32")` and `FromName("CRC-32/ISO-HDLC")` return the same instance.

## Common standards (strongly-typed)

These are exposed as `public static CrcStandard` properties on <xref:Bodu.IO.Hashing.Checksums.CrcStandard> for convenience — the underlying cache is still shared with the enum-based lookup.

| Name | Width | Property | Aliases |
|---|---:|---|---|
| CRC-8/MAXIM-DOW | 8 | `CrcStandard.CRC8_MAXIMDOW` | `CRC-8/MAXIM`, `DOW-CRC` |
| CRC-8/SMBUS | 8 | `CrcStandard.CRC8_SMBUS` | `CRC-8` |
| CRC-16/ARC | 16 | `CrcStandard.CRC16_ARC` | `ARC`, `CRC-16`, `CRC-16/LHA`, `CRC-IBM` |
| CRC-16/IBM-3740 | 16 | `CrcStandard.CRC16_IBM3740` | `CRC-16/AUTOSAR`, `CRC-16/CCITT-FALSE` |
| CRC-16/KERMIT | 16 | `CrcStandard.CRC16_KERMIT` | `CRC-16/BLUETOOTH`, `CRC-16/CCITT`, `CRC-16/CCITT-TRUE`, `CRC-16/V-41-LSB`, `CRC-CCITT`, `KERMIT` |
| CRC-16/MODBUS | 16 | `CrcStandard.CRC16_MODBUS` | `MODBUS` |
| CRC-16/XMODEM | 16 | `CrcStandard.CRC16_XMODEM` | `CRC-16/ACORN`, `CRC-16/LTE`, `CRC-16/V-41-MSB`, `XMODEM`, `ZMODEM` |
| CRC-32/BZIP2 | 32 | `CrcStandard.CRC32_BZIP2` | `CRC-32/AAL5`, `CRC-32/DECT-B`, `B-CRC-32` |
| CRC-32/ISCSI | 32 | `CrcStandard.CRC32_ISCSI` | `CRC-32/BASE91-C`, `CRC-32/CASTAGNOLI`, `CRC-32/INTERLAKEN`, `CRC-32C`, `CRC-32/NVME` |
| CRC-32/ISO-HDLC | 32 | `CrcStandard.CRC32_ISOHDLC` | `CRC-32`, `CRC-32/ADCCP`, `CRC-32/V-42`, `CRC-32/XZ`, `PKZIP` |
| CRC-64/ECMA-182 | 64 | `CrcStandard.CRC64_ECMA182` | `CRC-64` |
| CRC-64/XZ | 64 | `CrcStandard.CRC64_XZ` | `CRC-64/GO-ECMA` |

## Full catalogue

Access the following via `CrcStandard.Get(CrcStandards.X)` or `CrcStandard.FromName("name")`.

| Name | Width | Class | Enum | Aliases | RevEng |
|---|---:|---|---|---|---|
| CRC-3/GSM | 3 | academic | `CrcStandards.CRC3_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-3-gsm) |
| CRC-3/ROHC | 3 | academic | `CrcStandards.CRC3_ROHC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-3-rohc) |
| CRC-4/G-704 | 4 | academic | `CrcStandards.CRC4_G704` | `CRC-4/ITU` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-4-g-704) |
| CRC-4/INTERLAKEN | 4 | academic | `CrcStandards.CRC4_INTERLAKEN` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-4-interlaken) |
| CRC-5/EPC-C1G2 | 5 | attested | `CrcStandards.CRC5_EPCC1G2` | `CRC-5/EPC` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-5-epc-c1g2) |
| CRC-5/G-704 | 5 | academic | `CrcStandards.CRC5_G704` | `CRC-5/ITU` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-5-g-704) |
| CRC-5/USB | 5 | confirmed | `CrcStandards.CRC5_USB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-5-usb) |
| CRC-6/CDMA2000-A | 6 | attested | `CrcStandards.CRC6_CDMA2000A` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-cdma2000-a) |
| CRC-6/CDMA2000-B | 6 | academic | `CrcStandards.CRC6_CDMA2000B` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-cdma2000-b) |
| CRC-6/DARC | 6 | attested | `CrcStandards.CRC6_DARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-darc) |
| CRC-6/G-704 | 6 | academic | `CrcStandards.CRC6_G704` | `CRC-6/ITU` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-g-704) |
| CRC-6/GSM | 6 | academic | `CrcStandards.CRC6_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-6-gsm) |
| CRC-7/MMC | 7 | academic | `CrcStandards.CRC7_MMC` | `CRC-7` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-7-mmc) |
| CRC-7/ROHC | 7 | academic | `CrcStandards.CRC7_ROHC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-7-rohc) |
| CRC-7/UMTS | 7 | academic | `CrcStandards.CRC7_UMTS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-7-umts) |
| CRC-8/AUTOSAR | 8 | attested | `CrcStandards.CRC8_AUTOSAR` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-autosar) |
| CRC-8/CDMA2000 | 8 | academic | `CrcStandards.CRC8_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-cdma2000) |
| CRC-8/DARC | 8 | attested | `CrcStandards.CRC8_DARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-darc) |
| CRC-8/DVB-S2 | 8 | academic | `CrcStandards.CRC8_DVBS2` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-dvb-s2) |
| CRC-8/GSM-A | 8 | academic | `CrcStandards.CRC8_GSMA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-gsm-a) |
| CRC-8/GSM-B | 8 | academic | `CrcStandards.CRC8_GSMB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-gsm-b) |
| CRC-8/HITAG | 8 | attested | `CrcStandards.CRC8_HITAG` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-hitag) |
| CRC-8/I-432-1 | 8 | academic | `CrcStandards.CRC8_I4321` | `CRC-8/ITU` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-i-432-1) |
| CRC-8/I-CODE | 8 | attested | `CrcStandards.CRC8_ICODE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-i-code) |
| CRC-8/LTE | 8 | academic | `CrcStandards.CRC8_LTE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-lte) |
| **CRC-8/MAXIM-DOW** | 8 | attested | `CrcStandards.CRC8_MAXIMDOW` | `CRC-8/MAXIM`, `DOW-CRC` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-maxim-dow) |
| CRC-8/MIFARE-MAD | 8 | attested | `CrcStandards.CRC8_MIFAREMAD` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-mifare-mad) |
| CRC-8/NRSC-5 | 8 | attested | `CrcStandards.CRC8_NRSC5` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-nrsc-5) |
| CRC-8/OPENSAFETY | 8 | attested | `CrcStandards.CRC8_OPENSAFETY` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-opensafety) |
| CRC-8/ROHC | 8 | academic | `CrcStandards.CRC8_ROHC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-rohc) |
| CRC-8/SAE-J1850 | 8 | attested | `CrcStandards.CRC8_SAEJ1850` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-sae-j1850) |
| **CRC-8/SMBUS** | 8 | attested | `CrcStandards.CRC8_SMBUS` | `CRC-8` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-smbus) |
| CRC-8/TECH-3250 | 8 | attested | `CrcStandards.CRC8_TECH3250` | `CRC-8/AES`, `CRC-8/EBU` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-tech-3250) |
| CRC-8/WCDMA | 8 | third-party | `CrcStandards.CRC8_WCDMA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-wcdma) |
| CRC-10/ATM | 10 | attested | `CrcStandards.CRC10_ATM` | `CRC-10`, `CRC-10/I-610` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-10-atm) |
| CRC-10/CDMA2000 | 10 | academic | `CrcStandards.CRC10_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-10-cdma2000) |
| CRC-10/GSM | 10 | academic | `CrcStandards.CRC10_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-10-gsm) |
| CRC-11/FLEXRAY | 11 | attested | `CrcStandards.CRC11_FLEXRAY` | `CRC-11` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-11-flexray) |
| CRC-11/UMTS | 11 | academic | `CrcStandards.CRC11_UMTS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-11-umts) |
| CRC-12/CDMA2000 | 12 | academic | `CrcStandards.CRC12_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-cdma2000) |
| CRC-12/DECT | 12 | academic | `CrcStandards.CRC12_DECT` | `X-CRC-12` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-dect) |
| CRC-12/GSM | 12 | academic | `CrcStandards.CRC12_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-gsm) |
| CRC-12/UMTS | 12 | academic | `CrcStandards.CRC12_UMTS` | `CRC-12/3GPP` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-12-umts) |
| CRC-13/BBC | 13 | attested | `CrcStandards.CRC13_BBC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-13-bbc) |
| CRC-14/DARC | 14 | attested | `CrcStandards.CRC14_DARC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-14-darc) |
| CRC-14/GSM | 14 | academic | `CrcStandards.CRC14_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-14-gsm) |
| CRC-15/CAN | 15 | academic | `CrcStandards.CRC15_CAN` | `CRC-15` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-15-can) |
| CRC-15/MPT1327 | 15 | attested | `CrcStandards.CRC15_MPT1327` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-15-mpt1327) |
| **CRC-16/ARC** | 16 | attested | `CrcStandards.CRC16_ARC` | `ARC`, `CRC-16`, `CRC-16/LHA`, `CRC-IBM` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-arc) |
| CRC-16/CDMA2000 | 16 | academic | `CrcStandards.CRC16_CDMA2000` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-cdma2000) |
| CRC-16/CMS | 16 | third-party | `CrcStandards.CRC16_CMS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-cms) |
| CRC-16/DDS-110 | 16 | attested | `CrcStandards.CRC16_DDS110` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dds-110) |
| CRC-16/DECT-R | 16 | attested | `CrcStandards.CRC16_DECTR` | `R-CRC-16` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dect-r) |
| CRC-16/DECT-X | 16 | attested | `CrcStandards.CRC16_DECTX` | `X-CRC-16` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dect-x) |
| CRC-16/DNP | 16 | confirmed | `CrcStandards.CRC16_DNP` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dnp) |
| CRC-16/EN-13757 | 16 | confirmed | `CrcStandards.CRC16_EN13757` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-en-13757) |
| CRC-16/GENIBUS | 16 | attested | `CrcStandards.CRC16_GENIBUS` | `CRC-16/DARC`, `CRC-16/EPC`, `CRC-16/EPC-C1G2`, `CRC-16/I-CODE` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-genibus) |
| CRC-16/GSM | 16 | attested | `CrcStandards.CRC16_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-gsm) |
| **CRC-16/IBM-3740** | 16 | attested | `CrcStandards.CRC16_IBM3740` | `CRC-16/AUTOSAR`, `CRC-16/CCITT-FALSE` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-ibm-3740) |
| CRC-16/IBM-SDLC | 16 | attested | `CrcStandards.CRC16_IBMSDLC` | `CRC-16/ISO-HDLC`, `CRC-16/ISO-IEC-14443-3-B`, `CRC-16/X-25`, `CRC-B`, `X-25` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-ibm-sdlc) |
| CRC-16/ISO-IEC-14443-3-A | 16 | attested | `CrcStandards.CRC16_ISOIEC144433A` | `CRC-A` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-iso-iec-14443-3-a) |
| **CRC-16/KERMIT** | 16 | attested | `CrcStandards.CRC16_KERMIT` | `CRC-16/BLUETOOTH`, `CRC-16/CCITT`, `CRC-16/CCITT-TRUE`, `CRC-16/V-41-LSB`, `CRC-CCITT`, `KERMIT` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-kermit) |
| CRC-16/LJ1200 | 16 | third-party | `CrcStandards.CRC16_LJ1200` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-lj1200) |
| CRC-16/M17 | 16 | attested | `CrcStandards.CRC16_M17` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-m17) |
| CRC-16/MAXIM-DOW | 16 | attested | `CrcStandards.CRC16_MAXIMDOW` | `CRC-16/MAXIM` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-maxim-dow) |
| CRC-16/MCRF4XX | 16 | attested | `CrcStandards.CRC16_MCRF4XX` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-mcrf4xx) |
| **CRC-16/MODBUS** | 16 | attested | `CrcStandards.CRC16_MODBUS` | `MODBUS` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-modbus) |
| CRC-16/NRSC-5 | 16 | attested | `CrcStandards.CRC16_NRSC5` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-nrsc-5) |
| CRC-16/OPENSAFETY-A | 16 | attested | `CrcStandards.CRC16_OPENSAFETYA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-opensafety-a) |
| CRC-16/OPENSAFETY-B | 16 | attested | `CrcStandards.CRC16_OPENSAFETYB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-opensafety-b) |
| CRC-16/PROFIBUS | 16 | attested | `CrcStandards.CRC16_PROFIBUS` | `CRC-16/IEC-61158-2` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-profibus) |
| CRC-16/RIELLO | 16 | third-party | `CrcStandards.CRC16_RIELLO` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-riello) |
| CRC-16/SPI-FUJITSU | 16 | attested | `CrcStandards.CRC16_SPIFUJITSU` | `CRC-16/AUG-CCITT` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-spi-fujitsu) |
| CRC-16/T10-DIF | 16 | attested | `CrcStandards.CRC16_T10DIF` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-t10-dif) |
| CRC-16/TELEDISK | 16 | confirmed | `CrcStandards.CRC16_TELEDISK` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-teledisk) |
| CRC-16/TMS37157 | 16 | attested | `CrcStandards.CRC16_TMS37157` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-tms37157) |
| CRC-16/UMTS | 16 | attested | `CrcStandards.CRC16_UMTS` | `CRC-16/BUYPASS`, `CRC-16/VERIFONE` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-umts) |
| CRC-16/USB | 16 | confirmed | `CrcStandards.CRC16_USB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-usb) |
| **CRC-16/XMODEM** | 16 | attested | `CrcStandards.CRC16_XMODEM` | `CRC-16/ACORN`, `CRC-16/LTE`, `CRC-16/V-41-MSB`, `XMODEM`, `ZMODEM` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-xmodem) |
| CRC-17/CAN-FD | 17 | academic | `CrcStandards.CRC17_CANFD` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-17-can-fd) |
| CRC-21/CAN-FD | 21 | academic | `CrcStandards.CRC21_CANFD` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-21-can-fd) |
| CRC-24/BLE | 24 | attested | `CrcStandards.CRC24_BLE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-ble) |
| CRC-24/FLEXRAY-A | 24 | attested | `CrcStandards.CRC24_FLEXRAYA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-flexray-a) |
| CRC-24/FLEXRAY-B | 24 | attested | `CrcStandards.CRC24_FLEXRAYB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-flexray-b) |
| CRC-24/INTERLAKEN | 24 | academic | `CrcStandards.CRC24_INTERLAKEN` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-interlaken) |
| CRC-24/LTE-A | 24 | academic | `CrcStandards.CRC24_LTEA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-lte-a) |
| CRC-24/LTE-B | 24 | academic | `CrcStandards.CRC24_LTEB` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-lte-b) |
| CRC-24/OPENPGP | 24 | attested | `CrcStandards.CRC24_OPENPGP` | `CRC-24` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-openpgp) |
| CRC-24/OS-9 | 24 | attested | `CrcStandards.CRC24_OS9` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-24-os-9) |
| CRC-30/CDMA | 30 | academic | `CrcStandards.CRC30_CDMA` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-30-cdma) |
| CRC-31/PHILIPS | 31 | confirmed | `CrcStandards.CRC31_PHILIPS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-31-philips) |
| CRC-32/AIXM | 32 | attested | `CrcStandards.CRC32_AIXM` | `CRC-32Q` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-aixm) |
| CRC-32/AUTOSAR | 32 | attested | `CrcStandards.CRC32_AUTOSAR` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-autosar) |
| CRC-32/BASE91-D | 32 | confirmed | `CrcStandards.CRC32_BASE91D` | `CRC-32D` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-base91-d) |
| **CRC-32/BZIP2** | 32 | attested | `CrcStandards.CRC32_BZIP2` | `CRC-32/AAL5`, `CRC-32/DECT-B`, `B-CRC-32` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-bzip2) |
| CRC-32/CD-ROM-EDC | 32 | academic | `CrcStandards.CRC32_CDROMEDC` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-cd-rom-edc) |
| CRC-32/CKSUM | 32 | attested | `CrcStandards.CRC32_CKSUM` | `CKSUM`, `CRC-32/POSIX` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-cksum) |
| **CRC-32/ISCSI** | 32 | attested | `CrcStandards.CRC32_ISCSI` | `CRC-32/BASE91-C`, `CRC-32/CASTAGNOLI`, `CRC-32/INTERLAKEN`, `CRC-32C`, `CRC-32/NVME` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-iscsi) |
| **CRC-32/ISO-HDLC** | 32 | attested | `CrcStandards.CRC32_ISOHDLC` | `CRC-32`, `CRC-32/ADCCP`, `CRC-32/V-42`, `CRC-32/XZ`, `PKZIP` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-iso-hdlc) |
| CRC-32/JAMCRC | 32 | confirmed | `CrcStandards.CRC32_JAMCRC` | `JAMCRC` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-jamcrc) |
| CRC-32/MEF | 32 | attested | `CrcStandards.CRC32_MEF` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-mef) |
| CRC-32/MPEG-2 | 32 | attested | `CrcStandards.CRC32_MPEG2` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-mpeg-2) |
| CRC-32/XFER | 32 | confirmed | `CrcStandards.CRC32_XFER` | `XFER` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-xfer) |
| CRC-40/GSM | 40 | academic | `CrcStandards.CRC40_GSM` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-40-gsm) |
| **CRC-64/ECMA-182** | 64 | academic | `CrcStandards.CRC64_ECMA182` | `CRC-64` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-ecma-182) |
| CRC-64/GO-ISO | 64 | confirmed | `CrcStandards.CRC64_GOISO` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-go-iso) |
| CRC-64/JONES | 64 | confirmed | `CrcStandards.CRC64_JONES` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-jones) |
| CRC-64/MS | 64 | attested | `CrcStandards.CRC64_MS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-ms) |
| CRC-64/NVME | 64 | attested | `CrcStandards.CRC64_NVME` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-nvme) |
| CRC-64/REDIS | 64 | academic | `CrcStandards.CRC64_REDIS` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-redis) |
| CRC-64/WE | 64 | confirmed | `CrcStandards.CRC64_WE` | — | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-we) |
| **CRC-64/XZ** | 64 | attested | `CrcStandards.CRC64_XZ` | `CRC-64/GO-ECMA` | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-64-xz) |

## Not supported (width exceeds 64 bits)

The following standards are listed in the source catalogue but are **not** exposed by `CrcStandard` because their width exceeds the 64-bit scalar representation used by this library.

| Name | Width | Class | RevEng |
|---|---:|---|---|
| CRC-82/DARC | 82 | attested | [spec](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-82-darc) |

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
- **[Bodu.IO.Hashing guides overview](index.md)** — every guide in this library.
