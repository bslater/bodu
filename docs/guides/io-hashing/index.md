---
title: Bodu.IO.Hashing guides
---

# Bodu.IO.Hashing guides

Recipe-style walk-throughs for the **Bodu.IO.Hashing** package — the full CRC RevEng catalogue and the Fletcher family, built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract.

If you're looking for the generated API reference, see the [Bodu.IO.Hashing namespace page](../../apidoc/Bodu.IO.Hashing.md).

## Start here

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="crc.html">Using CRC</a></h3>
  <p>Named standards (<code>CRC32_ISOHDLC</code>, <code>CRC16_MODBUS</code>, <code>CRC64_XZ</code>, …), the <code>CrcStandards</code> enum, <code>FromName</code>, custom parameter sets, shared lookup-table caches, resumable hashing, and streaming over a <code>Stream</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="fletcher.html">Using Fletcher</a></h3>
  <p>Choosing between Fletcher-16 / 32 / 64, the <code>Append</code> / <code>GetCurrentHash</code> / <code>Reset</code> lifecycle, the twin-accumulator structure, and when to prefer CRC instead.</p>
</div>

<div class="bodu-card">
  <h3><a href="crc-catalogue.html">CRC catalogue</a></h3>
  <p>The full table of named CRC standards, mechanically derived from the <a href="https://reveng.sourceforge.io/crc-catalogue/all.htm">CRC RevEng catalogue</a> — name, width, class, enum value, and every published alias.</p>
</div>

</div>

## Picking the right checksum

| If you need… | Reach for | Why |
|---|---|---|
| A standard on-the-wire checksum (zlib / PNG / Ethernet / PKZIP) | <xref:Bodu.IO.Hashing.Crc> + <xref:Bodu.IO.Hashing.CrcStandard.CRC32_ISOHDLC> | Canonical CRC-32; the result is interoperable with every tool that speaks one of the listed formats. |
| A modern hardware-friendly 32-bit CRC (iSCSI, Btrfs, NVMe, SCTP) | <xref:Bodu.IO.Hashing.Crc> + <xref:Bodu.IO.Hashing.CrcStandard.CRC32_ISCSI> | Castagnoli polynomial; better error-detection properties than ISO-HDLC and the choice for modern protocols. |
| A short, cheap checksum that still catches transpositions | <xref:Bodu.IO.Hashing.Fletcher16> / <xref:Bodu.IO.Hashing.Fletcher32> / <xref:Bodu.IO.Hashing.Fletcher64> | Twin-accumulator design detects swaps that a simple sum or XOR misses. |
| An obscure standard from a serial protocol or silicon datasheet | <xref:Bodu.IO.Hashing.Crc> + one of the 113 entries in [the catalogue](crc-catalogue.md) | Every canonical RevEng entry is reachable through <xref:Bodu.IO.Hashing.CrcStandards>, including aliases. |
| A hash that's safe against an adversary (not just noise) | Not here — see <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.Tiger>, or <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> | Everything in this library is **non-cryptographic**. Attackers can forge collisions trivially. |

## Cross-references

- [Bodu.Security.Cryptography hashing guide](../cryptography/hashing.md) — keyed hashes (SipHash), cryptographic digests (Tiger), Merkle trees, verifying hashes in constant time.
- [Bodu.IO.Hashing API reference](../../apidoc/Bodu.IO.Hashing.md) — namespace overview with key types.
