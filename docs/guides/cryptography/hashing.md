---
title: Using hashes and checksums
---

# Using hashes and checksums

**Bodu.Security.Cryptography** ships the library's keyed hashes (SipHash), one-time authenticators (Poly1305), cryptographic digests (Tiger, Snefru, CubeHash, ASCON-HASH256, ASCON-HASHA256), and Merkle-tree hashing. All of them plug into the standard <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> contract.

This page is the cross-cutting overview — how the families relate, which to choose for which job, and how to verify a digest safely. For the full per-algorithm walk-throughs, see:

- [Using SipHash](siphash.md) — keyed short-input PRF.
- [Using Poly1305](poly1305.md) — one-time authenticator.
- [Using Tiger](tiger.md) — 128 / 160 / 192-bit cryptographic digest.
- [Using CubeHash](cubehash.md) — SHA-3 submission with tunable rounds and block size.
- [Using Snefru](snefru.md) — legacy 128 / 256-bit digest (interop only).
- [Using ASCON-HASH256 and ASCON-HASHA256](ascon.md) — NIST SP 800-232 sponge digests; two variants trading margin for throughput.
- [Using Merkle trees](merkle-trees.md) — tree-structured streaming integrity.

> **Looking for CRC, Fletcher, Adler, FNV, CityHash, Pearson, Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, or Pjw32?** Those non-cryptographic families live in the companion <xref:Bodu.IO.Hashing> package, built on <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>. See the [Bodu.IO.Hashing guides](../io-hashing/).

## Pick the right tool

| If you need… | Use | Why |
|---|---|---|
| A fast non-cryptographic fingerprint for hash tables, bucketing, caches | <xref:Bodu.IO.Hashing.Adler32>, <xref:Bodu.IO.Hashing.Fnv1a32>, <xref:Bodu.IO.Hashing.Fnv1a64>, <xref:Bodu.IO.Hashing.CityHash64> | Cheap, well-spread, **not** cryptographic. |
| A hash-table key or short fingerprint, resistant to collision-DoS | <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.SipHash128> | Keyed, collision-resistant for short inputs. |
| A cryptographic digest for signatures, fingerprints, or content addressing | <xref:Bodu.Security.Cryptography.Tiger>, or <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> (BCL) | Collision-resistant against active attackers. |
| A NIST-standardised 256-bit digest with a small state footprint (SP 800-232) | <xref:Bodu.Security.Cryptography.AsconHash256>, <xref:Bodu.Security.Cryptography.AsconHashA256> | Two variants: max margin (`ASCON-HASH256`) or higher throughput (`ASCON-HASHA256`). |
| A rolling integrity check over a long stream with partial re-verification | <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Subtree recomputation without rehashing the whole input. |
| An on-the-wire CRC (zlib, PNG, Modbus, iSCSI, …) or a Fletcher checksum | <xref:Bodu.IO.Hashing.Crc>, <xref:Bodu.IO.Hashing.Fletcher32> | Non-cryptographic, `System.IO.Hashing` contract — see the [Bodu.IO.Hashing guides](../io-hashing/). |

## Pattern 1 — a classic non-cryptographic fingerprint

Adler, FNV, and CityHash all derive from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> (via **Bodu.IO.Hashing**), so they drop into any API that accepts a standard .NET non-cryptographic hash.

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

var fnv = new Fnv1a64();
fnv.Append(data);
byte[] digest = fnv.GetCurrentHash();
string hex    = Convert.ToHexString(digest);   // 8-byte value, 16 hex characters
```

The same shape works for `Adler32`, `Adler64`, `Fletcher16`, `Fletcher32`, `Fletcher64`, `Fnv1a32`, `Fnv1a64`, `CityHash32`, `CityHash64`, `Crc`, and the classic string hashes (`Bernstein`, `BKDR`, `SDBM`, `JSHash`, `Elf64`, `ApHash`, `Pjw32`, `Pearson`). They all derive from `NonCryptographicHashAlgorithm`, so any API that accepts that base accepts them.

**What they're not.** These are non-cryptographic — they are error-detection and distribution tools, not authentication tools. An attacker who can freely modify a message can trivially forge the digest. Pair them with a signature or a MAC if you need integrity against an adversary.

## Pattern 2 — a keyed hash (SipHash)

SipHash was designed to keep hash tables safe from collision-DoS attacks. It takes a secret key, and even an adversary who knows the algorithm cannot produce collisions efficiently without the key.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// 128-bit key — generated once, stored in your process / vault.
byte[] key = new byte[16];
RandomNumberGenerator.Fill(key);

using var sip = new SipHash64 { Key = key };
byte[] digest = sip.ComputeHash(keyBytes);
ulong slotHash = BitConverter.ToUInt64(digest);

// Use the output as a stable 64-bit hash for routing / bucketing.
```

`SipHash128` gives you a 128-bit output for use cases that care about longer collision resistance. Both types expose `CompressionRounds` and `FinalizationRounds` if you need to trade speed for margin (defaults are the standard SipHash-2-4 parameterisation).

## Pattern 3 — a cryptographic digest

<xref:Bodu.Security.Cryptography.Tiger> is a classic 192-bit cryptographic hash optimised for 64-bit platforms. Use it for interoperability with existing Tiger-based systems, or when you want a digest in the same family as SHA-2 without pulling in a second dependency.

```csharp
using Bodu.Security.Cryptography;

using var tiger = new Tiger();     // 192-bit default; set HashSize for 128/160
byte[] digest = tiger.ComputeHash(data);
```

For brand-new work, prefer the BCL's <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> or <xref:System.Security.Cryptography.SHA512?displayProperty=nameWithType> — they are hardware-accelerated on most modern CPUs.

## Pattern 4 — verifying a hash

Computing a hash is half the job; comparing it in constant time is the other half. The `HashAlgorithmExtensions` class (in `Bodu.Security.Cryptography.Extensions`) provides a `VerifyHash` helper that does the comparison with <xref:System.Security.Cryptography.CryptographicOperations.FixedTimeEquals*>:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

byte[] expected = LoadExpectedDigestForFile("report.pdf");

using var hash = new Tiger();
bool match = hash.VerifyHash(new ReadOnlySpan<byte>(fileBytes), expected);
```

A direct `SequenceEqual` comparison leaks timing information to an observer and is unsafe for authentication decisions.

## Pattern 5 — streaming hash over a file

For non-cryptographic streaming, `NonCryptographicHashAlgorithm.Append(Stream)` (or `AppendAsync`) consumes the stream without loading it into memory:

```csharp
using System.IO.Hashing;
using Bodu.IO.Hashing;

var hash = new Fnv1a64();
using (var stream = File.OpenRead("archive.bin"))
    hash.Append(stream);

byte[] fingerprint = hash.GetCurrentHash();
```

For a cryptographic digest the `HashAlgorithm` base exposes `ComputeHash(Stream)`:

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var tiger = new Tiger();
using var stream = File.OpenRead("archive.bin");
byte[] digest = tiger.ComputeHash(stream);
```

For a larger file where you want partial verifiability — "the first megabyte's digest is X; the second megabyte's digest is Y" — use a Merkle tree instead (next section).

## Pattern 6 — Merkle trees

<xref:Bodu.Security.Cryptography.MerkleTreeHash> lets you compute a single root digest over a stream by hashing it in fixed-size blocks and reducing the leaves level-by-level. The intermediate hashes can later prove integrity of an individual chunk without rehashing the whole stream.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

using var merkle = new MerkleTreeHash(
    algorithmFactory: () => SHA256.Create(),
    blockSize: 4096,
    fanOut: 2);

using var stream = File.OpenRead("archive.bin");
byte[] root = merkle.ComputeHash(stream);
```

Each leaf is a SHA-256 of a 4 KiB block; each internal node is a SHA-256 of two concatenated child hashes. The root changes if any byte of the input changes.

For large inputs where you want to overlap leaf hashing with tree reduction, use <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> — see the [class documentation](../../api/Bodu.Security.Cryptography.ParallelMerkleTreeHash.html) for the swim-lane diagram of how its dispatcher and level-workers interact.

## Where to go next

- [Encryption basics](encryption-basics.md) — symmetric encryption in this library.
- [Cipher block modes](cipher-modes.md) — ECB / CBC / CFB / OFB / CTR with worked examples.
- [Bodu.IO.Hashing guides](../io-hashing/) — CRC, Fletcher, Adler, FNV, CityHash, and the classic string hashes on `System.IO.Hashing.NonCryptographicHashAlgorithm`.
- [MerkleTreeHash class doc](../../api/Bodu.Security.Cryptography.MerkleTreeHash.html) · [ParallelMerkleTreeHash class doc](../../api/Bodu.Security.Cryptography.ParallelMerkleTreeHash.html).
