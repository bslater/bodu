---
uid: Bodu.IO.Hashing
---

![Bodu.IO.Hashing](~/images/hero-io.svg)

## Purpose

**Bodu.IO.Hashing** is a focused library of **non-cryptographic** hashes and checksums built on the BCL <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> contract. It covers two families in depth: the full **CRC RevEng catalogue** (widths 1–64 bits) and the **Fletcher** checksum family (16, 32, 64 bits).

Reach for this library when you need a fast, deterministic checksum for error detection, file integrity, framing, fingerprinting, or cache keying — and when you want the result to drop straight into any API that accepts <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>. If you need cryptographic integrity (against an active attacker, not just noise on the wire), see <xref:Bodu.Security.Cryptography> instead.

## Key types

**CRC family**

- <xref:Bodu.IO.Hashing.Crc> — the single CRC engine. Configured with a <xref:Bodu.IO.Hashing.CrcStandard>, it handles widths from 1 to 64 bits, honours polynomial, initial value, input / output reflection, and final XOR, and ships with a shared lookup-table cache.
- <xref:Bodu.IO.Hashing.CrcStandard> — an immutable parameter set: name, width, polynomial, initial value, reflect-in, reflect-out, XOR-out. Exposes common standards as named properties (`CRC32_ISOHDLC`, `CRC32_ISCSI`, `CRC16_MODBUS`, `CRC64_XZ`, …) and provides `FromName` / `TryFromName` over canonical names and published aliases.
- <xref:Bodu.IO.Hashing.CrcStandards> — an enum covering every canonical CRC RevEng entry (113 standards as of the last catalogue fetch).
- <xref:Bodu.IO.Hashing.CrcLookupTableCache> — thread-safe cache of 256-entry lookup tables, keyed by (width, polynomial, reflect-in), shared process-wide through <xref:Bodu.IO.Hashing.Crc.GlobalCache>.
- <xref:Bodu.IO.Hashing.CrcLookupTableBuilder> — builds a lookup table from parameters; used by the cache on first miss.

**Fletcher family**

- <xref:Bodu.IO.Hashing.Fletcher16> — 16-bit position-dependent checksum; 1-byte block.
- <xref:Bodu.IO.Hashing.Fletcher32> — 32-bit position-dependent checksum; 2-byte block.
- <xref:Bodu.IO.Hashing.Fletcher64> — 64-bit position-dependent checksum; 4-byte block.

**Building blocks**

- <xref:Bodu.IO.Hashing.BlockNonCryptographicHashAlgorithm`1> — an abstract CRTP base for block-oriented non-cryptographic hashes. Provides residual-buffering, finalisation, and clone semantics that the Fletcher family builds on.
- <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> — a contract for hashes that can reconstruct their state from a previously finalised digest and continue appending new data, implemented by <xref:Bodu.IO.Hashing.Crc>.

## Example

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

// CRC-32/ISO-HDLC — the canonical zlib / PNG / Ethernet CRC.
using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(data);
string crc32 = Convert.ToHexString(crc.GetCurrentHash());

// Fletcher-32 — position-dependent, drops into anything that takes a NonCryptographicHashAlgorithm.
using var fletcher = new Fletcher32();
fletcher.Append(data);
string fl32 = Convert.ToHexString(fletcher.GetCurrentHash());

// Resume a CRC from a previously stored digest and keep hashing.
byte[] previous = crc32.AsSpan().ToArray();           // already-finalised digest bytes
byte[] combined = crc.ComputeHashFrom(previous, Encoding.UTF8.GetBytes(" jumps over"));
```

## Notes

- **Not cryptographically secure.** Every algorithm here is designed for error detection and hash-table distribution, not authentication. An attacker who can choose the input can trivially forge the output. Pair with a MAC or signature if integrity against an adversary matters — see <xref:Bodu.Security.Cryptography.SipHash64> for a keyed short-input hash, or <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> for a full cryptographic digest.
- **Shared lookup tables.** <xref:Bodu.IO.Hashing.Crc> instances with identical (width, polynomial, reflect-in) triples share a single 256-entry lookup table through <xref:Bodu.IO.Hashing.Crc.GlobalCache>. Constructing a hundred `Crc(CrcStandard.CRC32_ISOHDLC)` instances allocates one table, not a hundred.
- **Non-destructive `GetCurrentHash`.** Calling <xref:System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash*> snapshots the accumulator and applies the final reflect / XOR / width-mask on the copy, so in-progress hashing is not disturbed. Call it as many times as you like.
- **Resumable.** <xref:Bodu.IO.Hashing.Crc> implements <xref:Bodu.IO.Hashing.IResumableHashAlgorithm> — reverse-finalise a stored digest, append further data, re-finalise. Handy for chunked streams where re-reading earlier bytes is expensive.
- **Determinism and portability.** All algorithms produce identical byte-for-byte output across platforms and architectures for the same input and configuration.
- **See also:** the [Using CRC](../guides/io-hashing/crc.md) and [Using Fletcher](../guides/io-hashing/fletcher.md) guides, and the [full CRC catalogue](../guides/io-hashing/crc-catalogue.md).
