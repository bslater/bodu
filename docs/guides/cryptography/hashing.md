---
title: Using hashes and checksums
---

# Using hashes and checksums

**Bodu.Security.Cryptography** ships the library's keyed hashes (SipHash), one-time authenticators (Poly1305), cryptographic digests (Tiger, Snefru, CubeHash, ASCON-HASH256, ASCON-HASHA256), and Merkle-tree hashing. All of them plug into the standard <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> contract.

This page is the cross-cutting overview — what guarantee a cryptographic hash actually makes, how the structural shapes (digest / XOF / tree) differ, which to choose for which job, and how to verify a digest safely. For the full per-algorithm walk-throughs, see:

- [Using SipHash](siphash.md) — keyed short-input PRF.
- [Using Poly1305](poly1305.md) — one-time authenticator.
- [Using Tiger](tiger.md) — 128 / 160 / 192-bit cryptographic digest.
- [Using CubeHash](cubehash.md) — SHA-3 submission with tunable rounds and block size.
- [Using Snefru](snefru.md) — legacy 128 / 256-bit digest (interop only).
- [Using ASCON-HASH256 and ASCON-HASHA256](ascon.md) — NIST SP 800-232 sponge digests; two variants trading margin for throughput.
- [Using Merkle trees](merkle-trees.md) — tree-structured streaming integrity.

> **Looking for CRC, Fletcher, Adler, FNV, CityHash, Pearson, Bernstein, BKDR, SDBM, JSHash, Elf64, ApHash, or Pjw32?** Those non-cryptographic families live in the companion <xref:Bodu.IO.Hashing> package, built on <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>. See the [Bodu.IO.Hashing guides](../io-hashing/index.md).

## What a cryptographic hash guarantees

A cryptographic hash is a **one-way** function: cheap to compute forwards, computationally infeasible to invert. Three named resistance properties define the contract, and the security level of each is bounded by the digest width:

- **Pre-image resistance** — given a digest `h`, no efficient way to find any input `m` with `hash(m) = h`. Costs ≈ 2ⁿ for an n-bit digest.
- **Second-pre-image resistance** — given an input `m₁`, no efficient way to find a *different* `m₂` with the same digest. Costs ≈ 2ⁿ.
- **Collision resistance** — no efficient way to find *any* pair `m₁ ≠ m₂` with the same digest. Costs ≈ 2^(n/2) by the birthday bound — so a 256-bit digest gives only 128-bit collision resistance, and truncating a digest halves the collision exponent with it.

This is the line between this package and the non-cryptographic <xref:Bodu.IO.Hashing> fingerprints: those carry **no adversary model** and fail every property above against an attacker who can choose the input. A cryptographic digest provides **integrity only when the digest itself reaches the verifier over an authenticated channel** — on its own it is not authentication. For integrity *and* authenticity against an active attacker, reach for a keyed hash / MAC (below) or an AEAD mode.

## The three structural shapes

The cryptographic hashes here share the <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType> base but differ in what they produce:

| Shape | Output | Types | Use |
|---|---|---|---|
| **Plain digest** | Fixed width chosen at construction | <xref:Bodu.Security.Cryptography.Tiger>, <xref:Bodu.Security.Cryptography.Whirlpool>, <xref:Bodu.Security.Cryptography.CubeHash>, <xref:Bodu.Security.Cryptography.Snefru128>, <xref:Bodu.Security.Cryptography.Blake2b>, <xref:Bodu.Security.Cryptography.Skein512>, <xref:Bodu.Security.Cryptography.AsconHash256> | Content addressing, signature inputs, fingerprints. |
| **Extendable output (XOF)** | Any requested length | <xref:Bodu.Security.Cryptography.Shake>, <xref:Bodu.Security.Cryptography.AsconXof128> | Squeeze arbitrary-length key material or deterministic randomness from a seed. |
| **Tree** | Root digest over parallel leaves | <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Verifiable per-chunk inclusion proofs; partial re-verification. |

Two families on this page take a **secret key** in addition to the message and produce an authentication tag — and they split on a critical axis:

| Subtype | Type | Key reuse |
|---|---|---|
| **Reusable PRF** | <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.SipHash128> | One key authenticates **many** messages. |
| **One-time authenticator** | <xref:Bodu.Security.Cryptography.Poly1305> | The key authenticates **exactly one** message — reuse is a complete break. |

## The HashAlgorithm lifecycle

Every cryptographic hash here is a <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, so the BCL contract applies unchanged:

- **One-shot** — `ComputeHash(byte[])`, `ComputeHash(ReadOnlySpan<byte>)`, or `ComputeHash(Stream)`. The instance is auto-reinitialised afterwards, so the same instance can hash the next message.
- **Streaming** — `TransformBlock(...)` repeatedly, then `TransformFinalBlock(...)`; read the digest from the `Hash` property. Call `Initialize()` to reset between messages.
- `CanReuseTransform` reports whether the instance survives a finalisation — `true` for the digests and for SipHash, **`false` for `Poly1305`** (its way of refusing key reuse at the API level).

The <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> helpers add `AppendData`, `VerifyHash`, and `TryVerifyHash` (and their async overloads) on top of that base.

## Pick the right tool

| If you need… | Use | Why |
|---|---|---|
| A fast non-cryptographic fingerprint for hash tables, bucketing, caches | <xref:Bodu.IO.Hashing.Checksums.Adler32>, <xref:Bodu.IO.Hashing.Fnv1a32>, <xref:Bodu.IO.Hashing.Fnv1a64>, <xref:Bodu.IO.Hashing.CityHash64> | Cheap, well-spread, **not** cryptographic. |
| A hash-table key or short fingerprint, resistant to collision-DoS | <xref:Bodu.Security.Cryptography.SipHash64>, <xref:Bodu.Security.Cryptography.SipHash128> | Keyed, collision-resistant for short inputs. |
| A cryptographic digest for signatures, fingerprints, or content addressing | <xref:Bodu.Security.Cryptography.Tiger>, or `System.Security.Cryptography.SHA256` (BCL) | Collision-resistant against active attackers. |
| A NIST-standardized 256-bit digest with a small state footprint (SP 800-232) | <xref:Bodu.Security.Cryptography.AsconHash256>, <xref:Bodu.Security.Cryptography.AsconHashA256> | Two variants: max margin (`ASCON-HASH256`) or higher throughput (`ASCON-HASHA256`). |
| A rolling integrity check over a long stream with partial re-verification | <xref:Bodu.Security.Cryptography.MerkleTreeHash>, <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> | Subtree recomputation without rehashing the whole input. |
| An on-the-wire CRC (zlib, PNG, Modbus, iSCSI, …) or a Fletcher checksum | <xref:Bodu.IO.Hashing.Checksums.Crc>, <xref:Bodu.IO.Hashing.Checksums.Fletcher32> | Non-cryptographic, `System.IO.Hashing` contract — see the [Bodu.IO.Hashing guides](../io-hashing/index.md). |

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

SipHash was designed to keep hash tables safe from collision-DoS attacks. It is a **reusable PRF** — one secret key authenticates many messages, and even an adversary who knows the algorithm cannot produce collisions efficiently without the key.

```csharp
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

// 128-bit key (SipHash64.KeySize == 128) — generated once, stored in your process / vault.
byte[] key = new byte[SipHash64.KeySize / 8];   // 16 bytes
RandomNumberGenerator.Fill(key);

byte[] message = "the quick brown fox"u8.ToArray();

using var sip = new SipHash64 { Key = key };
byte[] digest = sip.ComputeHash(message);
ulong slotHash = BitConverter.ToUInt64(digest);

// Use the output as a stable 64-bit hash for routing / bucketing.
```

The key is exactly 16 bytes; a shorter or longer key is rejected at configuration time. `SipHash128` gives you a 128-bit output for use cases that care about longer collision resistance. Both types expose `CompressionRounds` (default 2) and `FinalizationRounds` (default 4) if you need to trade speed for margin — the defaults are the standard **SipHash-2-4** parameterization. See [Using SipHash](siphash.md).

> **PRF vs one-time authenticator.** SipHash is safe to reuse under one key. <xref:Bodu.Security.Cryptography.Poly1305> is *not* — its forgery bound holds only while the 32-byte key is used exactly once, which is why its `CanReuseTransform` is `false`. See [Using Poly1305](poly1305.md).

## Pattern 3 — a cryptographic digest

<xref:Bodu.Security.Cryptography.Tiger> is a classic 192-bit cryptographic hash optimized for 64-bit platforms. Use it for interoperability with existing Tiger-based systems, or when you want a digest in the same family as SHA-2 without pulling in a second dependency.

```csharp
using Bodu.Security.Cryptography;

using var tiger = new Tiger();     // 192-bit default; set HashSize for 128/160
byte[] digest = tiger.ComputeHash(data);
```

For brand-new work, prefer the BCL's `System.Security.Cryptography.SHA256` or `System.Security.Cryptography.SHA512` — they are hardware-accelerated on most modern CPUs. The package also ships <xref:Bodu.Security.Cryptography.Whirlpool> (512-bit, ISO/IEC 10118-3), <xref:Bodu.Security.Cryptography.CubeHash> (tunable), <xref:Bodu.Security.Cryptography.Skein512> (Threefish-based, with a built-in MAC mode), and <xref:Bodu.Security.Cryptography.Blake2b> / <xref:Bodu.Security.Cryptography.Blake3>.

> [!WARNING]
> <xref:Bodu.Security.Cryptography.Snefru128> / <xref:Bodu.Security.Cryptography.Snefru256> are **cryptographically broken** — practical collisions are known. They ship for interoperability and research only; never use them to protect real data. See [Using Snefru](snefru.md).

## Pattern 3b — an extendable-output function (XOF)

When you need *more* output than a fixed digest supplies — arbitrary-length key material, deterministic randomness from a seed — reach for a XOF instead of a plain digest. <xref:Bodu.Security.Cryptography.Shake> squeezes any positive multiple of 8 bits at a chosen 128- or 256-bit security level:

```csharp
using Bodu.Security.Cryptography;

// SHAKE256, squeezing 1024 bits (128 bytes) from the seed.
using var shake = new Shake(outputBits: 1024, securityLevel: 256);
byte[] output = shake.ComputeHash(seed);   // 128 bytes
```

Unlike a Merkle–Damgård digest, a sponge XOF is immune to length extension and truncating its output is safe down to the security target. See [Using SHAKE](shake.md).

## Pattern 4 — verifying a hash

Computing a hash is half the job; comparing it in constant time is the other half. The `HashAlgorithmExtensions` class (in `Bodu.Security.Cryptography.Extensions`) provides a `VerifyHash` helper that does the comparison with `CryptographicOperations.FixedTimeEquals`:

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

For large inputs where you want to overlap leaf hashing with tree reduction, use <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash> — see the class documentation for the swim-lane diagram of how its dispatcher and level-workers interact.

## Where to go next

- [Encryption basics](encryption-basics.md) — symmetric encryption in this library.
- [Cipher block modes](cipher-modes.md) — ECB / CBC / CFB / OFB / CTR with worked examples.
- [Bodu.IO.Hashing guides](../io-hashing/index.md) — CRC, Fletcher, Adler, FNV, CityHash, and the classic string hashes on `System.IO.Hashing.NonCryptographicHashAlgorithm`.
- <xref:Bodu.Security.Cryptography.MerkleTreeHash> · <xref:Bodu.Security.Cryptography.ParallelMerkleTreeHash>.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
