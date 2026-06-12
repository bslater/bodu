---
title: Hashing & Cryptography — Overview
---

# Hashing & Cryptography

Two Bodu packages turn bytes into fixed-size summaries or protected ciphertext, and the line between them is a single question: **is there an adversary?**

**Bodu.IO.Hashing** is the non-cryptographic library — fast fingerprints for hash-table keys and caches, error-detecting checksums (CRC, Fletcher, Adler) for transmission and storage channels, and check digits (Luhn, Damm, IBAN, ISBN, …) for human-typed identifiers. Nothing in it carries a security guarantee: every algorithm is trivially forgeable by anyone who controls the bytes, and that is by design — it buys speed, portability, and characterized error coverage on *trusted* input.

**Bodu.Security.Cryptography** is the adversarial-setting library — block, tweakable, and stream ciphers, AEAD constructions, keyed hashes (MACs), cryptographic digests and XOFs, Merkle trees, and memory-hard key-derivation functions (Argon2, scrypt). Every algorithm is designed against a formal **adversary model**: it must be computationally infeasible for an attacker — even one who knows the algorithm, observes many inputs and outputs, and chooses inputs adaptively — to forge, invert, or find collisions.

![Algorithm taxonomy across both libraries](../../images/diagrams/algorithm-taxonomy.svg)

## Members of this topic

| Package | Status | What it provides | Docs |
|---|---|---|---|
| **Bodu.IO.Hashing** | Stable | Fingerprints (FNV, CityHash, MurmurHash3, Pearson, the classic string hashes), checksums (`Crc` with 113 named standards, Fletcher, Adler), and check digits (Luhn, Damm, Verhoeff, EAN, GTIN, ISIN, IBAN, ISBN, SEDOL, CUSIP, ABA, LEI, ISO 7064) — all on the BCL `NonCryptographicHashAlgorithm` contract. | [Introduction](../io-hashing/index.md) · [Concepts](../io-hashing/concepts.md) · [Getting started](../io-hashing/getting-started.md) |
| **Bodu.Security.Cryptography** | Stable | Block ciphers (Threefish, Serpent, Camellia, Twofish, Blowfish, Skipjack), stream ciphers (ChaCha20, Salsa20 families, Rabbit, HC-128), AES paired with six AEAD mode transforms (GCM, CCM, OCB, EAX, SIV, GCM-SIV), MACs (SipHash, Poly1305), cryptographic digests and XOFs (Tiger, Whirlpool, BLAKE2/3, Skein, Shake, ASCON), Merkle trees, and KDFs (Argon2, scrypt) — on the BCL `SymmetricAlgorithm` / `HashAlgorithm` contracts. | [Introduction](../cryptography/index.md) · [Concepts](../cryptography/concepts.md) · [Getting started](../cryptography/getting-started.md) |

## Two libraries, one decision

The split is structural, not cosmetic — the packages differ in lineage, posture, and contract:

| Dimension | Bodu.IO.Hashing | Bodu.Security.Cryptography |
|---|---|---|
| **Base-class lineage** | <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType> — `Append` / `GetCurrentHash` / `Reset`, with non-destructive snapshots. | <xref:System.Security.Cryptography.HashAlgorithm?displayProperty=nameWithType>, <xref:System.Security.Cryptography.KeyedHashAlgorithm?displayProperty=nameWithType>, and <xref:System.Security.Cryptography.SymmetricAlgorithm?displayProperty=nameWithType> — plus Bodu's `IBlockCipher` and `TweakableSymmetricAlgorithm`. |
| **Performance posture** | Tuned for throughput, even distribution, and constant-memory streaming; lookup tables shared across instances. | Pays for security margin — rounds, key schedules, sponge permutations; KDFs are *deliberately* expensive (memory-hard). |
| **Guarantee** | Detects *accidental* errors with characterized coverage (burst errors, transpositions, mistypes); distributes evenly across buckets. | Computationally infeasible to forge a tag, invert a digest, find a collision, or recover plaintext without the key. |
| **Attacker assumption** | None. Input is trusted; anyone who controls the bytes can forge any output. | Attacker knows the algorithm, observes many inputs/outputs, and chooses inputs adaptively. |

### The decision rule

> **Can an attacker choose or tamper with the input, or does anything secret ride on the result?** If yes — keys, authentication, tamper evidence, passwords, attacker-reachable hash tables — use `Bodu.Security.Cryptography`. Otherwise — error detection, distribution, fingerprinting, identifier validation on trusted input — use `Bodu.IO.Hashing`.

Cross the line the moment the trust assumption changes. A CRC that guards a download against line noise is correct; the same CRC "guarding" a file against tampering is a vulnerability, because anyone who can modify the file can recompute the CRC.

> [!IMPORTANT]
> When you do reach for the cryptographic side, the safety rules from the [Bodu.Security.Cryptography introduction](../cryptography/index.md) apply in full:
>
> - **Never reuse a nonce or IV under the same key** — stream ciphers and most AEAD modes lose all confidentiality on nonce reuse.
> - **Always verify the AEAD authentication tag** before trusting decrypted plaintext; the transforms reject mismatches with `CryptographicException`.
> - **Compare tags and digests in constant time** — `CryptographicOperations.FixedTimeEquals`, never `==` or `SequenceEqual`.
> - **Prefer AEAD over encrypt-then-MAC-by-hand**, and **prefer the BCL** (`Aes`, `AesGcm`, SHA-2/3) where it already covers your case.

## Subfamilies at a glance

**Bodu.IO.Hashing** partitions into three subfamilies, each in its own namespace:

- **Fingerprints** (`Bodu.IO.Hashing`) — even distribution and speed for hash-table keys, cache bucketing, and deduplication. Judged on avalanche and streaming behavior, not error coverage.
- **Checksums** (`Bodu.IO.Hashing.Checksums`) — short tags engineered to catch the error patterns of a channel: single-bit flips, burst errors, adjacent transpositions. Polynomial-remainder (`Crc`) and twin-accumulator (Fletcher, Adler) shapes.
- **Check digits** (`Bodu.IO.Hashing.CheckDigits`, with the multi-character schemes in `Checksums`) — one or two characters appended to a *printed identifier* so a later reader can confirm it was not mis-typed.

**Bodu.Security.Cryptography** spans the cryptographic roles:

- **Symmetric ciphers** — standard block ciphers, tweakable block ciphers (Threefish, with a public tweak for domain separation), and raw-keystream stream ciphers (confidentiality only — pair with a MAC or prefer AEAD).
- **AEAD** — authenticated encryption with associated data: ciphertext plus an authentication tag in a single pass, via `AsconAead128` or AES paired with the GCM / CCM / OCB / EAX / SIV / GCM-SIV mode transforms.
- **Cryptographic hashes** — plain digests, extendable-output functions (XOFs), and tree hashes (BLAKE3, Merkle trees).
- **Keyed hashes / MACs** — the reusable PRF (SipHash) and the one-time authenticator (Poly1305).
- **KDFs** — memory-hard password hashing and key derivation (Argon2id / Argon2i / Argon2d, scrypt).

The lifecycle shape differs accordingly. Non-cryptographic hashes snapshot non-destructively; cryptographic algorithms finalize and dispose:

```csharp
// Bodu.IO.Hashing — streaming with non-destructive snapshots.
using var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
crc.Append(chunk1);
crc.Append(chunk2);
byte[] checksum = crc.GetCurrentHash();   // snapshot; appending may continue

// Bodu.Security.Cryptography — keyed hash over the BCL HashAlgorithm contract.
using var mac = new SipHash64 { Key = key };
byte[] tag = mac.ComputeHash(message);    // compare with FixedTimeEquals, never ==
```

## Which library do I need?

| Scenario | Reach for | Notes |
|---|---|---|
| Hash-table key inside a trust boundary | `Fnv1a64`, `MurmurHash3_32` (IO.Hashing) | The default fingerprints; constant-memory streaming. |
| Hash-table keys exposed to attacker-chosen input | `SipHash64` / `SipHash128` (Cryptography) | Hash-flooding defense — a keyed PRF, not a fingerprint. |
| File integrity against *accidental* corruption | `Crc`, `Fletcher32`, `Adler32` (IO.Hashing) | Characterized error coverage; pick the published `CrcStandard` your channel expects. |
| File integrity against *tampering* | `Blake2b`, `Blake3`, `AsconHash256` digest — or a MAC / AEAD (Cryptography) | A digest detects tampering only if the digest itself travels over an authenticated channel. |
| Validate an ISBN, IBAN, credit-card number, or barcode | `Isbn10` / `Isbn13`, `Iban`, `Luhn`, `Ean13` (IO.Hashing) | Transcription guards for human-typed identifiers — not unforgeability. |
| Password storage / password-based key derivation | `Argon2id` (Cryptography) | Memory-hard KDF per RFC 9106; never a plain digest, never anything from IO.Hashing. |
| Encrypt-then-authenticate a message | `AesBlockCipher` + `GcmModeTransform`, or `AsconAead128` (Cryptography) | AEAD bundles confidentiality, integrity, and authenticity in one pass. |
| MAC over API tokens or cookies | `SipHash64` / `SipHash128` (Cryptography) | One key authenticates many messages; compare tags in constant time. |
| Per-record / per-sector encryption without re-keying | `Threefish256/512/1024` with `Tweak`, `XtsModeTransform` (Cryptography) | Tweakable ciphers give public domain separation. |
| Stream encryption of arbitrary-length data (no padding) | `ChaCha20`, `XChaCha20` (Cryptography) | Confidentiality only — pair with `Poly1305` or prefer AEAD. |
| Deduplication / cache bucketing inside a trust boundary | `CityHash64` / `CityHash128` (IO.Hashing) | SIMD-friendly; fastest on long inputs. If dedup keys cross a trust boundary, switch to a cryptographic digest. |
| Verifiable inclusion proofs over many leaves | `MerkleTreeHash`, `ParallelMerkleTreeHash` (Cryptography) | Root digest plus logarithmic sibling paths. |

Two BCL notes that both introductions make: for xxHash, use `System.IO.Hashing.XxHash32/64/3/128` directly — Bodu does not duplicate them; and prefer the BCL's hardware-accelerated `Aes`, `AesGcm`, and SHA-2/3 where they cover your case — reach for Bodu when you need an algorithm the BCL does not ship.

### Using both together

Real systems routinely use both packages, each on its own side of the trust boundary. A storage pipeline might CRC each block on the way to disk (accidental-corruption detection, `Bodu.IO.Hashing`), encrypt and authenticate the payload with an AEAD before it leaves the process (`Bodu.Security.Cryptography`), and key its in-memory block cache with FNV-1a (trusted input, `Bodu.IO.Hashing` again). The packages do not overlap, so installing both adds no ambiguity — every algorithm's namespace tells you which guarantee you are getting.

## Install

```bash
dotnet add package Bodu.IO.Hashing
dotnet add package Bodu.Security.Cryptography
```

Both target `net8.0` and depend only on `Bodu.Core` (and, for `Bodu.IO.Hashing`, the BCL's `System.IO.Hashing` contract). Install only the one you need — they are independent packages.

## Where to go next

- **[Hashing & Cryptography — Concepts](hashing-and-cryptography-concepts.md)** — the full taxonomy: fingerprint vs. checksum vs. check digit vs. digest vs. MAC vs. AEAD vs. XOF vs. KDF, and the guarantee each does and does not make.
- **[Bodu.IO.Hashing introduction](../io-hashing/index.md)** — subfamily map and the algorithm-selection tables.
- **[Bodu.Security.Cryptography introduction](../cryptography/index.md)** — the six subfamilies, the choosing-a-primitive table, and the safety rules.
- **Getting started:** [Bodu.IO.Hashing](../io-hashing/getting-started.md) · [Bodu.Security.Cryptography](../cryptography/getting-started.md).
- **[Hashing & Cryptography guides](../../guides/topics/hashing-and-cryptography.md)** — the recipe-style walk-throughs for this topic.
- **API reference:** [Bodu.IO.Hashing](xref:Bodu.IO.Hashing) · [Bodu.Security.Cryptography](xref:Bodu.Security.Cryptography).
