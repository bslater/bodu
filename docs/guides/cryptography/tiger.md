---
title: Using Tiger
---

# Using Tiger

<xref:Bodu.Security.Cryptography.Tiger> is a 1995 cryptographic hash by Anderson and Biham, optimized for 64-bit platforms and widely deployed in file-transfer and content-addressing systems (most famously Direct Connect's Tiger Tree Hash). It processes 64-byte blocks and produces a **192-bit digest by default**, with 160-bit and 128-bit truncations available.

Tiger derives from <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, so any API that accepts a standard .NET hash accepts Tiger.

> For new work where interoperability isn't a constraint, prefer the BCL's hardware-accelerated `System.Security.Cryptography.SHA256` or `System.Security.Cryptography.SHA512`. Use Tiger when you need to match an existing Tiger-based system, or as part of a Tiger Tree Hash (see the Merkle tree guide).

## Fixed sizes at a glance

| Parameter | Size | Notes |
|---|---|---|
| Block size | 64 bytes (512 bits) | Fixed. |
| Output | 192 bits (default), 160 bits, or 128 bits | Configurable via `HashSize`. |
| Variant | `TigerHashingVariant.Tiger` (padding byte `0x01`) or `.Tiger2` (padding byte `0x80`) | Default is `Tiger`. |

## Pattern 1 — a default 192-bit digest

```csharp
using System.Text;
using Bodu.Security.Cryptography;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var tiger = new Tiger();                  // 192-bit, Tiger variant
byte[] digest   = tiger.ComputeHash(data);      // 24 bytes
string hex      = Convert.ToHexString(digest);
```

This matches the digest that a reference Tiger implementation would produce for the same input — 24 bytes, in the standard output layout.

## Pattern 2 — choose a truncation

Tiger accepts three output widths. 192 is the "real" Tiger; 160 and 128 are truncations of the same 192-bit internal state, useful when an external protocol fixes the digest size.

```csharp
using Bodu.Security.Cryptography;

using var tiger128 = new Tiger(hashSize: 128);  // 16-byte digest
using var tiger160 = new Tiger(hashSize: 160);  // 20-byte digest
using var tiger192 = new Tiger(hashSize: 192);  // 24-byte digest (the default)
```

`HashSize` is also settable on an existing instance, but only **before** the first block is processed:

```csharp
using var tiger = new Tiger { HashSize = 160 };
byte[] digest = tiger.ComputeHash(data);
```

Attempting to change `HashSize` after `ComputeHash` / `TransformBlock` has begun throws — the schedule is fixed at the first call.

## Pattern 3 — Tiger vs Tiger2

Tiger's original specification used a padding byte of `0x01`. A later clarification (Tiger2) changed the padding byte to `0x80` to match the convention used by SHA-2 and other Merkle-Damgård hashes. The two variants produce **different digests** for the same input.

```csharp
using Bodu.Security.Cryptography;

using var tiger  = new Tiger  { Variant = TigerHashingVariant.Tiger  };   // default
using var tiger2 = new Tiger  { Variant = TigerHashingVariant.Tiger2 };

byte[] d1 = tiger .ComputeHash(data);
byte[] d2 = tiger2.ComputeHash(data);
// d1 != d2 — different padding byte → different digest.
```

Match the variant to whatever the interoperating system speaks. `AlgorithmName` reflects the configured hash size (it follows the `Tiger/{bits}` convention regardless of variant; include the variant alongside it when logging):

```csharp
using var tiger = new Tiger { Variant = TigerHashingVariant.Tiger2, HashSize = 160 };
Console.WriteLine($"{tiger.AlgorithmName} ({tiger.Variant})");   // "Tiger/160 (Tiger2)"
```

## Pattern 4 — streaming a file

Tiger plugs into any BCL API that takes a `HashAlgorithm`:

```csharp
using Bodu.Security.Cryptography;

using var tiger = new Tiger();

using var stream = File.OpenRead("archive.bin");
byte[] digest = tiger.ComputeHash(stream);
```

For larger files where you want to verify ranges of the file without rehashing the whole thing, pair Tiger with <xref:Bodu.Security.Cryptography.MerkleTreeHash> to build a Tiger Tree Hash — see the [Merkle trees guide](merkle-trees.md).

## Pattern 5 — verifying a digest

Compare digests in constant time. The `VerifyHash` helper in `Bodu.Security.Cryptography.Extensions` wraps `CryptographicOperations.FixedTimeEquals`:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] expected = LoadExpectedDigest("report.pdf");

using var tiger = new Tiger();
bool ok = tiger.VerifyHash(fileBytes, expected);
```

A plain `SequenceEqual` leaks timing information and is unsafe when the result drives an authentication decision.

## When to use Tiger

- **Interoperability** with systems that already use Tiger — Direct Connect, the `tth:` URN scheme, older archive formats.
- **Tiger Tree Hash** (Merkle tree of Tiger leaves) for content-addressable stores — pair with <xref:Bodu.Security.Cryptography.MerkleTreeHash>.
- **Educational** or research settings where the round structure and S-boxes are the object of study.

For new work without an interoperability constraint, prefer SHA-2 or SHA-3 from the BCL — both are hardware-accelerated on modern CPUs and have broader analysis behind them.

## Where to go next

- [Hashing overview](hashing.md) — where Tiger sits alongside SipHash, Snefru, CubeHash, and the non-cryptographic families.
- [Merkle trees guide](merkle-trees.md) — build a Tiger Tree Hash over a stream.
- [Using SipHash](siphash.md) — keyed short-input hash, for hash-table DoS resistance.
- [Using CubeHash](cubehash.md), [Using Snefru](snefru.md) — other cryptographic digests in this package.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
