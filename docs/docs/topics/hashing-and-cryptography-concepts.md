---
title: Hashing & Cryptography — Concepts
---

# Hashing & Cryptography — Concepts

This page is the cross-package vocabulary for the Hashing & Cryptography topic — the full taxonomy of "things that summarize or protect bytes," and the guarantee each one does and does not make. Read the [topic overview](hashing-and-cryptography.md) first for the package split; the per-package concept pages linked at the bottom go deeper on each term.

## Adversary model

An **adversary model** is the formal statement of what an attacker is assumed to know, observe, and control. It is the single concept that organizes everything else on this page. A primitive *with* an adversary model remains secure even against an attacker who knows the algorithm, sees many input/output pairs, and chooses inputs adaptively. A primitive *without* one makes no claim at all against deliberate manipulation — its guarantees hold only for accidental errors on trusted input.

Everything in `Bodu.IO.Hashing` has **no** adversary model. Everything in `Bodu.Security.Cryptography` has one. There is no middle ground: "probably hard to fake" is not a security property.

## The taxonomy

| Kind | Keyed? | Reversible? | Guarantee made | Guarantee *not* made | Bodu examples |
|---|---|---|---|---|---|
| **Fingerprint** | No | No | Even distribution and good avalanche over the output range. | Nothing about error patterns or forgery — trivially collidable on purpose. | `Fnv1a64`, `CityHash64`, `MurmurHash3_128` |
| **Checksum** | No | No | Detects *characterized accidental* error patterns — burst errors, bit flips, transpositions. | Tamper evidence; even distribution (checksums make poor hash-table keys). | `Crc`, `Fletcher32`, `Adler32` |
| **Check digit** | No | No | Catches human transcription errors (substitutions, adjacent transpositions) in a *printed identifier*. | Unforgeability — anyone can compute a valid check digit for any payload. | `Luhn`, `Damm`, `Verhoeff`, `Iban`, `Isbn13` |
| **Cryptographic digest** | No | No | Pre-image, second-pre-image, and collision resistance. | Authenticity — a digest only proves integrity when it travels via an authenticated channel. | `Blake2b`, `Blake3`, `Tiger`, `Whirlpool`, `Skein512`, `AsconHash256` |
| **MAC (keyed hash)** | Yes | No | Integrity *and* authenticity: no one can forge a valid tag without the key. | Confidentiality — the message itself is not hidden. | `SipHash64` / `SipHash128` (reusable PRF), `Poly1305` (one-time key) |
| **AEAD** | Yes | Yes (decrypt) | Confidentiality, integrity, and authenticity in a single pass; optional associated data is authenticated but not encrypted. | Safety under nonce reuse — a repeated `(key, nonce)` pair voids the guarantees. | `AsconAead128`; `AesBlockCipher` + `GcmModeTransform` / `SivModeTransform` / … |
| **XOF** | No | No | A digest whose output length the *caller* chooses at squeeze time, with security up to the construction's capacity. | Anything a plain digest does not make; output length is a parameter, not extra strength. | `Shake`, `AsconXof128`, `AsconCxof128`, `Blake3` in XOF mode |
| **KDF** | Yes (secret input) | No | Stretches a low-entropy secret (a password) into key material at a *deliberately high* cost — memory-hard against GPU/ASIC attack. | Speed. A KDF that is fast is broken by definition for password storage. | `Argon2id` / `Argon2i` / `Argon2d`, scrypt |

Three placement rules fall out of the table:

- A **checksum** guards a binary payload only software sees; a **check digit** guards a printed identifier a human copies; a **fingerprint** just needs fast, even distribution. They are not interchangeable — CRC distributes poorly as a hash key, and FNV gives weaker burst-error guarantees than CRC.
- A **digest** alone never proves authenticity. If the attacker can replace both the file and its published hash, the digest verifies perfectly. Use a MAC or AEAD when the channel is untrusted.
- A **MAC** and a **cipher** both take keys but serve opposite purposes: the cipher hides a message without summarizing it; the MAC summarizes a message without hiding it. AEAD is the packaged combination.

## Distinctions inside the cryptographic side

**MAC vs. one-time authenticator.** Both are keyed hashes; the difference is how many messages one key can safely authenticate. A MAC in the PRF sense — `SipHash64` / `SipHash128` — authenticates many messages under one key. A one-time authenticator — `Poly1305` — is provably secure when its key is used *exactly once* and trivially broken on the second message, which is why it is normally paired with a stream cipher that derives a fresh per-message key from `(key, nonce)`.

**AEAD and nonce discipline.** AEAD's three-property guarantee is conditional on a single rule: the `(key, nonce)` pair must never repeat. The nonce is public and need not be unpredictable — a counter is fine — but reuse leaks plaintext relationships and enables forgery in most modes. The SIV and GCM-SIV transforms degrade more gracefully under nonce misuse, which is exactly why they exist.

**Digest vs. XOF.** A plain digest has a fixed output length baked into the algorithm; an XOF lets the caller squeeze any number of output bytes. Asking an XOF for more bytes does not add strength — security is bounded by the construction's internal capacity — but it removes the awkward truncate-or-concatenate dance when a protocol needs a non-standard output length, and it makes one primitive serve as digest, KDF-style expander, and stream of derived values.

**KDF vs. digest for passwords.** A password has so little entropy that any fast hash — cryptographic or not — falls to offline guessing. A KDF inverts the usual performance goal: it is *engineered to be expensive*, and memory-hard designs (Argon2, scrypt) tie the cost to RAM, the resource GPUs and ASICs scale worst. Cost parameters (memory, iterations, parallelism) are part of the stored output so they can be raised over time. Never store passwords behind a plain digest, however modern.

## Distinctions inside the non-cryptographic side

**Avalanche vs. error coverage.** A fingerprint is judged on *avalanche* — flipping one input bit flips roughly half the output bits, so keys spread evenly across hash-table buckets. A checksum is judged on *characterized error coverage* — which burst lengths, bit-flip counts, and transpositions it is guaranteed to catch on a channel. The two goals conflict: a well-chosen CRC polynomial catches every burst of its width or shorter but leaves output regions sparse for typical inputs, and a fast fingerprint makes no promise about any specific error pattern. This is why the subfamilies live in separate namespaces and the [Bodu.IO.Hashing introduction](../io-hashing/index.md) selects by job, not by speed.

**Streaming and snapshots.** Everything on the non-cryptographic side shares the `Append` / `GetCurrentHash` / `Reset` lifecycle, and `GetCurrentHash` is a *non-destructive snapshot* — you can read a rolling tag mid-stream and keep appending. Only `Crc` goes further, implementing `IResumableHashAlgorithm` to reverse-finalize a stored digest and continue.

## Collision and pre-image resistance

A cryptographic digest makes three related promises, in increasing order of difficulty for the attacker: given a digest, it is infeasible to find *any* input that produces it (**pre-image resistance**); given an input, it is infeasible to find a *different* input with the same digest (**second-pre-image resistance**); and it is infeasible to find *any two* inputs that collide (**collision resistance**). Collision resistance is the weakest link — birthday attacks halve the effective bit strength, which is why a 256-bit digest offers roughly 128-bit collision resistance, and why broken-but-shipped legacy digests (Snefru in Bodu's catalogue) are flagged as interop-only.

## Constant-time comparison

Comparing a computed tag or digest with `==` or `SequenceEqual` leaks timing: the comparison exits at the first mismatched byte, and an attacker who can measure response times can recover a valid tag byte by byte. Always compare MAC tags, AEAD tags, and security-relevant digests with `CryptographicOperations.FixedTimeEquals` (or the library's `VerifyHash` helpers, which use it internally). Bodu's AEAD transforms verify internally and reject mismatches with a `CryptographicException` — do not catch and ignore it, and do not re-implement the comparison around them.

None of this applies to `Bodu.IO.Hashing`: a CRC or fingerprint comparison protects no secret, so an early-exit `SequenceEqual` is fine there. The moment a comparison gates a security decision, the primitive — and the comparison — must both come from the cryptographic side.

## Member concept pages

| Member | Concepts coverage |
|---|---|
| Bodu.IO.Hashing | [Bodu.IO.Hashing — Core concepts](../io-hashing/concepts.md) — the adversary-model table, fingerprint / checksum / check digit boundaries, the `NonCryptographicHashAlgorithm` contract, avalanche, CRC parameters and the RevEng catalogue, twin-sum checksums, check-digit subfamilies, and endianness. |
| Bodu.Security.Cryptography | [Bodu.Security.Cryptography — Core concepts](../cryptography/concepts.md) — confidentiality / integrity / authenticity, block vs. stream ciphers, modes of operation, padding, IV vs. nonce, AEAD and tags, tweakable ciphers, digest output shapes, MAC vs. one-time authenticator, sponges, Merkle trees, and the BCL surface. |

For the package map, contrast table, and decision rule, return to the [Hashing & Cryptography overview](hashing-and-cryptography.md); for hands-on walk-throughs, see the [Hashing & Cryptography guides](../../guides/topics/hashing-and-cryptography.md).
