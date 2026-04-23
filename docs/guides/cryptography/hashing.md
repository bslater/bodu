---
title: Using hashes and checksums
---

# Using hashes and checksums

Non-cryptographic checksums and hash-table hashes live in **Bodu.IO.Hashing**, built on <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>. Cryptographic digests, keyed hashes, and Merkle trees live in **Bodu.Security.Cryptography**, built on <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>. This page sorts them by use-case and shows a minimal recipe for each.

## Pick the right tool

| If you need… | Use | Why |
|---|---|---|
| A fast error-detection checksum for network framing, file integrity, etc. | <xref:Bodu.IO.Hashing.Fletcher32>, <xref:Bodu.IO.Hashing.Adler32>, <xref:Bodu.IO.Hashing.Fnv1a32>, <xref:Bodu.IO.Hashing.Crc> | Cheap, well-spread, **not** cryptographic. |
| A hash-table key or a short fingerprint, resistant to collision DoS | <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.SipHash128> | Keyed, collision-resistant for short inputs. |
| A cryptographic digest for signatures, fingerprints, or content addressing | <xref:Bodu.Security.Cryptography.Tiger>, or <xref:System.Security.Cryptography.SHA256?displayProperty=nameWithType> (BCL) | Collision-resistant against active attackers. |
| A rolling integrity check over a long stream or file, with partial re-verification | <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Subtree recomputation without rehashing the whole input. |

**Bodu.IO.Hashing** also includes a number of classic non-cryptographic hashes (<xref:Bodu.IO.Hashing.Bernstein>, <xref:Bodu.IO.Hashing.BKDR>, <xref:Bodu.IO.Hashing.SDBM>, <xref:Bodu.IO.Hashing.JSHash>, <xref:Bodu.IO.Hashing.Elf64>, <xref:Bodu.IO.Hashing.ApHash>, <xref:Bodu.IO.Hashing.Pjw32>, <xref:Bodu.IO.Hashing.Pearson>, <xref:Bodu.IO.Hashing.CityHash32>, <xref:Bodu.IO.Hashing.CityHash64>) which follow the same `NonCryptographicHashAlgorithm` pattern shown below.

## Pattern 1 — a non-cryptographic checksum

```csharp
using System.IO.Hashing;
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

var hash = new Fletcher32();
hash.Append(data);
byte[] digest = hash.GetCurrentHash();
string hex    = Convert.ToHexString(digest);   // 4-byte value as 8 hex characters
```

The same shape works for `Adler32`, `Adler64`, `Fletcher16`, `Fletcher32`, `Fletcher64`, `Fnv1a32`, `Fnv1a64`, `CityHash64`, `Crc`, and the classic hashes listed above. They all derive from `NonCryptographicHashAlgorithm`, so any API that accepts a `NonCryptographicHashAlgorithm` accepts them.

**What they're not.** These are error-detection and distribution tools. An attacker who can freely modify a message can trivially forge the checksum. Pair them with a signature or a MAC if you need integrity against an adversary.

## Pattern 2 — a keyed hash (SipHash)

SipHash was designed to keep hash tables safe from collision-DoS attacks. It takes a secret key, and a knowledge-of-the-key adversary still cannot produce collisions efficiently.

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

`SipHash128` gives you a 128-bit output for use cases that care about longer collision resistance. Both types expose `CompressionRounds` and `FinalizationRounds` if you need to trade speed for margin (defaults are the standard 2-4 SipHash).

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

Every `HashAlgorithm` accepts a `Stream`, which lets you fingerprint a file without loading it into memory:

```csharp
using var hash = new Fnv1a64();
using var stream = File.OpenRead("archive.bin");
byte[] fingerprint = hash.ComputeHash(stream);
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
- [MerkleTreeHash class doc](../../api/Bodu.Security.Cryptography.MerkleTreeHash.html) · [ParallelMerkleTreeHash class doc](../../api/Bodu.Security.Cryptography.ParallelMerkleTreeHash.html).
