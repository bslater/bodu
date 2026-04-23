---
title: Bodu.IO.Hashing guides
---

# Bodu.IO.Hashing guides

Recipe-style walk-throughs for the **Bodu.IO.Hashing** package — non-cryptographic checksums and fingerprints built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract.

If you're looking for the generated API reference, see the [Bodu.IO.Hashing namespace page](../../apidoc/Bodu.IO.Hashing.md). For keyed or cryptographic hashes (SipHash, Poly1305, Tiger, CubeHash, Merkle trees), see the [Bodu.Security.Cryptography hashing guides](../cryptography/hashing.md).

## Start here

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="crc.html">Using CRC</a></h3>
  <p>Named standards (<code>CRC32_ISOHDLC</code>, <code>CRC16_MODBUS</code>, <code>CRC64_XZ</code>, …), the <code>CrcStandards</code> enum, <code>FromName</code>, custom parameter sets, shared lookup-table caches, resumable hashing, and streaming over a <code>Stream</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="crc-catalogue.html">CRC catalogue</a></h3>
  <p>The full table of named CRC standards, mechanically derived from the <a href="https://reveng.sourceforge.io/crc-catalogue/all.htm">CRC RevEng catalogue</a> — name, width, class, enum value, and every published alias.</p>
</div>

<div class="bodu-card">
  <h3><a href="fletcher.html">Using Fletcher</a></h3>
  <p>Choosing between Fletcher-16 / 32 / 64, the <code>Append</code> / <code>GetCurrentHash</code> / <code>Reset</code> lifecycle, the twin-accumulator structure, and when to prefer CRC instead.</p>
</div>

<div class="bodu-card">
  <h3><a href="adler.html">Using Adler</a></h3>
  <p>Adler-32 for zlib-compatible checksums, Adler-32C for SIMD-friendly throughput, and Adler-64 for very long streams — twin-accumulator checksums with a prime modulus.</p>
</div>

<div class="bodu-card">
  <h3><a href="fnv.html">Using FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32-bit and 64-bit widths — the simple, fast, textbook fingerprint for in-memory hash tables and short keys.</p>
</div>

<div class="bodu-card">
  <h3><a href="cityhash.html">Using CityHash</a></h3>
  <p>32-, 64-, and 128-bit CityHash — Google's SIMD-friendly fingerprint for long inputs. Faster and better-distributed than FNV on large buffers, at the cost of in-memory buffering.</p>
</div>

<div class="bodu-card">
  <h3><a href="pearson.html">Using Pearson</a></h3>
  <p>Pearson's table-driven hash with output widths from 8 bits to 2048 bits, four built-in permutation tables, and a user-defined table option.</p>
</div>

<div class="bodu-card">
  <h3><a href="string-hashes.html">Classic string hashes</a></h3>
  <p>Bernstein (djb2), BKDR, SDBM, JSHash, Elf64, ApHash, PJW — the one-liner hash functions from compilers, textbooks, and early web servers.</p>
</div>

</div>

## Picking the right hash

| If you need… | Reach for | Why |
|---|---|---|
| A standard on-the-wire checksum (zlib / PNG / Ethernet / PKZIP) | <xref:Bodu.IO.Hashing.Crc> + <xref:Bodu.IO.Hashing.CrcStandard.CRC32_ISOHDLC>, or <xref:Bodu.IO.Hashing.Adler32> | Canonical wire formats; the result is interoperable with every tool that speaks the format. |
| A modern hardware-friendly 32-bit CRC (iSCSI, Btrfs, NVMe, SCTP) | <xref:Bodu.IO.Hashing.Crc> + <xref:Bodu.IO.Hashing.CrcStandard.CRC32_ISCSI> | Castagnoli polynomial; better error-detection properties than ISO-HDLC and the choice for modern protocols. |
| A short, cheap checksum that still catches transpositions | <xref:Bodu.IO.Hashing.Fletcher16> / <xref:Bodu.IO.Hashing.Fletcher32> / <xref:Bodu.IO.Hashing.Fletcher64> | Twin-accumulator design detects swaps that a simple sum or XOR misses. |
| A fast, streaming-friendly fingerprint for short keys | <xref:Bodu.IO.Hashing.Fnv1a32> / <xref:Bodu.IO.Hashing.Fnv1a64> | Constant-memory, portable, same result everywhere. |
| The fastest in-memory fingerprint you can get | <xref:Bodu.IO.Hashing.CityHash64> / <xref:Bodu.IO.Hashing.CityHash128> | SIMD-friendly design with excellent distribution; buffers the input in memory. |
| An output width that isn't a native integer size | <xref:Bodu.IO.Hashing.Pearson> | 8–2048 bits in 8-bit steps, parallel accumulators per byte. |
| An obscure standard from a serial protocol or silicon datasheet | <xref:Bodu.IO.Hashing.Crc> + one of the 113 entries in [the catalogue](crc-catalogue.md) | Every canonical RevEng entry is reachable through <xref:Bodu.IO.Hashing.CrcStandards>, including aliases. |
| A hash that's safe against an adversary (not just noise) | Not here — see <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.Tiger>, or <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> | Everything in this library is **non-cryptographic**. Attackers can forge collisions trivially. |

## One shape, many algorithms

Everything in this package derives from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>, so the lifecycle is identical regardless of which algorithm you pick:

```csharp
using Bodu.IO.Hashing;

using var hash = new Crc();      // or Fletcher32, Adler32, Fnv1a64, CityHash64, …

hash.Append(chunk1);
hash.Append(chunk2);
byte[] partial = hash.GetCurrentHash();   // snapshot, non-destructive
hash.Append(chunk3);
byte[] full = hash.GetCurrentHash();

hash.Reset();                              // back to the initial state
```

Only CRC currently implements <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> (reverse-finalise an earlier digest, append new bytes, finalise again) — see the [CRC guide](crc.md#pattern-6--resume-from-a-stored-digest).

## Cross-references

- [Bodu.Security.Cryptography hashing guide](../cryptography/hashing.md) — keyed hashes (SipHash), cryptographic digests (Tiger, CubeHash, Snefru), one-time authenticators (Poly1305), and Merkle trees.
- [Bodu.IO.Hashing API reference](../../apidoc/Bodu.IO.Hashing.md) — namespace overview with key types.
