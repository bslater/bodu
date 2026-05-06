---
title: Using XxHash
---

# Using XxHash

XxHash is a non-cryptographic hash family designed by Yann Collet and distributed in the [xxHash reference repository](https://github.com/Cyan4973/xxHash). It is engineered for extremely high throughput — the algorithm processes data in multi-byte lanes that map naturally onto modern CPU registers — while maintaining excellent distribution and avalanche quality.

**Bodu.IO.Hashing** provides two variants:

| Type | Output | Notes |
|---|---|---|
| `XxHash32` | 32 bits | 32-bit output; works on both 32-bit and 64-bit platforms. |
| `XxHash64` | 64 bits | 64-bit output; optimised for 64-bit platforms. |

Both derive from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> via a shared `XxHash<T>` base. Both are one-shot algorithms that buffer their input internally.

> **Not cryptographic.** XxHash must not be used for password hashing, digital signatures, or any application that requires adversarial collision resistance. For adversary-facing use, reach for <xref:Bodu.Security.Cryptography.SipHash64>.

> **BCL note.** The .NET BCL ships `System.IO.Hashing.XxHash32` and `System.IO.Hashing.XxHash64` from .NET 6 onwards. The Bodu implementations are independently derived and expose the same `NonCryptographicHashAlgorithm` API shape, with the addition of a `Seed` property. Prefer the BCL types when seeding is not required and you want to minimise dependencies.

## Pattern 1 — compute a digest in one call

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var xxh = new XxHash64();
xxh.Append(data);
byte[] digest = xxh.GetCurrentHash();
string hex    = Convert.ToHexString(digest);   // 8 bytes, 16 hex characters
```

Substitute `XxHash32` when a 32-bit output is sufficient.

## Pattern 2 — a seeded fingerprint for independent hash functions

XxHash accepts an unsigned 64-bit seed at construction time (or via the `Seed` property, settable before the first `Append`). Different seeds produce independent hash functions over the same input.

```csharp
using Bodu.IO.Hashing;

// Two independent hash functions for a consistent-hash ring.
using var primary   = new XxHash64(seed: 0xA5A5A5A5A5A5A5A5UL);
using var secondary = new XxHash64(seed: 0x5A5A5A5A5A5A5A5AUL);

primary.Append(keyBytes);
secondary.Append(keyBytes);

ulong primarySlot   = BitConverter.ToUInt64(primary.GetCurrentHash())   % (ulong)ringSize;
ulong secondarySlot = BitConverter.ToUInt64(secondary.GetCurrentHash()) % (ulong)ringSize;
```

## Pattern 3 — `Append` / `GetCurrentHash` / `Reset` lifecycle

XxHash buffers all appended bytes internally and applies the final mixing pass in `GetCurrentHash`. The call is non-destructive — the buffer is preserved, so you can continue appending after a snapshot.

```csharp
using Bodu.IO.Hashing;

using var xxh = new XxHash64();

xxh.Append(headerBytes);
xxh.Append(bodyBytes);
byte[] partial = xxh.GetCurrentHash();   // snapshot — mixes all bytes appended so far
xxh.Append(trailerBytes);
byte[] full = xxh.GetCurrentHash();

xxh.Reset();                             // discards the buffer, keeps the seed
```

> **Memory note.** The internal buffer grows with each `Append`. For very large inputs where you want constant-memory processing, use a streaming algorithm — <xref:Bodu.IO.Hashing.Fnv1a64>, <xref:Bodu.IO.Hashing.Crc>, or <xref:Bodu.IO.Hashing.Fletcher32> all update state in place with no internal buffer.
> **Memory note.** The internal buffer grows with each `Append`. For very large inputs where you want constant-memory processing, use a streaming algorithm — <xref:Bodu.IO.Hashing.Fnv1a64>, <xref:Bodu.IO.Hashing.Checksums.Crc>, or <xref:Bodu.IO.Hashing.Checksums.Fletcher32> all update state in place with no internal buffer.

## Pattern 4 — streaming a file

For files that fit comfortably in memory, the standard streaming approach works fine:

```csharp
using Bodu.IO.Hashing;

using var xxh = new XxHash64();

using (var stream = File.OpenRead("asset.bin"))
    xxh.Append(stream);

byte[] fingerprint = xxh.GetCurrentHash();
```

For very large files where you do not want the whole buffer in memory, prefer a constant-memory algorithm or a Merkle tree — see the [cryptography hashing guide](../cryptography/hashing.md) for the `MerkleTreeHash` pattern.

## XxHash vs the other fingerprints

| Criterion | XxHash | MurmurHash3 | FNV-1a | CityHash |
|---|---|---|---|---|
| Output widths | 32 · 64-bit | 32 · 128-bit | 32 · 64-bit | 32 · 64 · 128-bit |
| Seed support | Yes | Yes | No | No |
| Streaming (constant memory) | No — buffers input | No — buffers input | Yes | No — buffers input |
| Throughput (large inputs) | Excellent | Good | Moderate | Excellent |
| Distribution quality | Excellent | Excellent | Good | Excellent |

Reach for **XxHash64** when you want seeded, high-throughput 64-bit fingerprints. Reach for **MurmurHash3_x64_128** when you need a 128-bit output. Reach for **FNV-1a** when constant-memory streaming is required. Reach for **CityHash** when you do not need a seed but want the absolute best throughput on large inputs.

## Where to go next

- [Using MurmurHash3](murmurhash3.md) — seeded fingerprint with 128-bit output.
- [Using CityHash](cityhash.md) — fastest throughput on large inputs.
- [Using FNV](fnv.md) — constant-memory streaming fingerprint.
- [Algorithm families](../algorithm-families.md) — fingerprints vs checksums vs keyed hashes.
- [Algorithm families](../../docs/algorithm-families.md) — fingerprints vs checksums vs keyed hashes.
- [Bodu.IO.Hashing namespace page](../../apidoc/Bodu.IO.Hashing.md) — key types and design notes.
