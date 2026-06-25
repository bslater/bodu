---
title: Hashing & Cryptography — Guides
---

# Hashing & Cryptography — Guides

Recipe-style walk-throughs for the **Hashing & Cryptography** topic — `Bodu.IO.Hashing` (non-cryptographic fingerprints, checksums, and check digits) and `Bodu.Security.Cryptography` (ciphers, AEAD, MACs, digests, and KDFs with a formal adversary model).

The two libraries answer different questions, and picking the wrong one is the most common mistake in this topic. If you have not already, read the [topic overview](../../docs/topics/hashing-and-cryptography.md) for the decision rule — *can an attacker choose or tamper with the input, or does anything secret ride on the result?* — and the [topic concepts page](../../docs/topics/hashing-and-cryptography-concepts.md) for the full taxonomy.

## Bodu.IO.Hashing guides

Fast, portable, *non-adversarial* hashing on the BCL `NonCryptographicHashAlgorithm` contract.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../io-hashing/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.IO.Hashing</code> — namespace map across fingerprints, checksums, and check digits, and which guide covers each algorithm.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-hashing/crc.md">Using CRC</a></h3>
  <p>One engine, 113 named standards from the RevEng catalogue — <code>CrcStandard</code> selection, custom parameter sets, the shared lookup-table cache, and resumable hashing.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-hashing/fnv.md">Using FNV</a></h3>
  <p>FNV-1 and FNV-1a at 32 and 64 bits — the textbook constant-memory fingerprint and the default when in doubt.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-hashing/murmurhash3.md">Using MurmurHash3</a></h3>
  <p><code>MurmurHash3_32</code> and <code>MurmurHash3_128</code> — seeded, excellent avalanche, widely used in databases and distributed systems.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-hashing/check-digits.md">Check digits overview</a></h3>
  <p>Luhn, Damm, Verhoeff, EAN, GTIN, ISIN, IBAN, ISBN, SEDOL, CUSIP, ABA, LEI — compute-vs-validate surfaces for human-typed identifiers.</p>
</div>

<div class="bodu-card">
  <h3><a href="../io-hashing/fletcher.md">Using Fletcher</a></h3>
  <p>Twin-accumulator checksums in 16, 32, and 64 bits — catches the transpositions a simple sum or XOR misses.</p>
</div>

</div>

## Bodu.Security.Cryptography guides

Primitives with a formal adversary model, on the BCL `SymmetricAlgorithm` / `HashAlgorithm` contracts.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../cryptography/index.md">Overview</a></h3>
  <p>The full guide index for <code>Bodu.Security.Cryptography</code> — foundations, standard / tweakable / stream ciphers, AEAD, hashes, MACs, and the ASCON family.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/encryption-basics.md">Encryption basics</a></h3>
  <p>Key, IV, Tweak, BlockMode, Padding — the mental model every cipher in the library follows, plus key generation and safe disposal.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/aead-modes.md">AEAD modes</a></h3>
  <p>GCM, CCM, OCB, EAX, SIV, GCM-SIV — authenticated encryption with associated data using <code>AesBlockCipher</code> plus a mode transform.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/hashing.md">Hashing overview</a></h3>
  <p>Cross-cutting overview of keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru), and Merkle trees.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/siphash.md">Using SipHash</a></h3>
  <p>SipHash-64 and SipHash-128 — the keyed PRF for hash-flooding-resistant tables and short-message authentication.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/argon2.md">Using Argon2</a></h3>
  <p>Argon2id / Argon2i / Argon2d per RFC 9106 — memory-hard password hashing and key derivation, with the parameter guidance.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/asymmetric-overview.md">Asymmetric algorithms overview</a></h3>
  <p>The four families over <code>AsymmetricAlgorithm</code> — key agreement, signatures, and post-quantum KEM / signatures — with selection guidance and the shared key import / export shape.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/key-agreement-x25519.md">Key agreement with X25519</a></h3>
  <p><code>X25519</code> (RFC 7748) — Diffie-Hellman over Curve25519 for deriving a shared secret between two parties.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/signatures-ed25519.md">Signatures with Ed25519</a></h3>
  <p><code>Ed25519</code> (RFC 8032) — deterministic EdDSA signing and verification over Curve25519.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/ml-kem.md">ML-KEM post-quantum key encapsulation</a></h3>
  <p><code>MLKem512</code> / <code>MLKem768</code> / <code>MLKem1024</code> (FIPS 203) — lattice-based key encapsulation resistant to quantum attack.</p>
</div>

<div class="bodu-card">
  <h3><a href="../cryptography/ml-dsa.md">ML-DSA post-quantum signatures</a></h3>
  <p><code>MLDsa44</code> / <code>MLDsa65</code> / <code>MLDsa87</code> (FIPS 204) — lattice-based digital signatures resistant to quantum attack.</p>
</div>

</div>

## Start here

1. **[Topic overview](../../docs/topics/hashing-and-cryptography.md)** — the adversary-model split, the contrast table, and the "which library do I need?" scenarios.
2. **[Topic concepts](../../docs/topics/hashing-and-cryptography-concepts.md)** — fingerprint vs. checksum vs. check digit vs. digest vs. MAC vs. AEAD vs. XOF vs. KDF.
3. **For cryptography**, start with **[Encryption basics](../cryptography/encryption-basics.md)** before any cipher walk-through — the Key / IV / Tweak / mode / padding model carries across every algorithm. Then take [AEAD modes](../cryptography/aead-modes.md) for authenticated encryption or the [hashing overview](../cryptography/hashing.md) for digests and MACs.
4. **For non-cryptographic hashing**, start with the **[guide overview](../io-hashing/index.md)** to pick a subfamily, then the per-algorithm walk-through — [CRC](../io-hashing/crc.md) for channel checksums, [FNV](../io-hashing/fnv.md) or [MurmurHash3](../io-hashing/murmurhash3.md) for fingerprints, [check digits](../io-hashing/check-digits.md) for identifiers.

## Where to go next

- **[Hashing & Cryptography overview](../../docs/topics/hashing-and-cryptography.md)** — the topic landing page on the docs side.
- **[Hashing & Cryptography concepts](../../docs/topics/hashing-and-cryptography-concepts.md)** — the cross-package taxonomy and safety vocabulary.
- **Member introductions:** [Bodu.IO.Hashing](../../docs/io-hashing/index.md) · [Bodu.Security.Cryptography](../../docs/cryptography/index.md).
- **API reference:** [Bodu.IO.Hashing](xref:Bodu.IO.Hashing) · [Bodu.Security.Cryptography](xref:Bodu.Security.Cryptography).
