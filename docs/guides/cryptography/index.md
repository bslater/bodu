---
title: Bodu.Security.Cryptography guides
---

# Bodu.Security.Cryptography guides

Recipe-style walk-throughs for **Bodu.Security.Cryptography**, organized by the type hierarchy of the library: foundations → standard ciphers → tweakable ciphers → stream ciphers → AEAD → cryptographic hashes → keyed hashes → ASCON.

Part of the **[Hashing & Cryptography](../topics/hashing-and-cryptography.md)** topic.

If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.Security.Cryptography introduction](../../docs/cryptography/index.md) and the [getting-started page](../../docs/cryptography/getting-started.md). Not sure which primitive to use? The introduction's *shape of the library* section maps the six subfamilies and explains how they differ.

For the auto-generated API reference, see the [Bodu.Security.Cryptography namespace page](xref:Bodu.Security.Cryptography). For non-cryptographic checksums and fingerprints, see the [Bodu.IO.Hashing guides](../io-hashing/index.md).

## Namespace map

| Namespace | What lives here | Guide section |
|---|---|---|
| `Bodu.Security.Cryptography` | All cryptographic primitives — block ciphers, mode transforms, padding strategies, hash algorithms, AEAD constructions, helpers. | All sections below |
| `Bodu.Security.Cryptography.Extensions` | Ergonomic one-shot, async, and verify helpers over `SymmetricAlgorithm`, `TweakableSymmetricAlgorithm`, `IBlockCipher` + AEAD transforms, `HashAlgorithm`, and `ICryptoTransform`. | (covered in the per-algorithm guides) |

## Foundations

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="encryption-basics.md">Encryption basics</a></h3>
  <p>The mental model: <code>BlockMode</code> vs .NET's <code>Mode</code>, how Key / IV / Tweak / Padding combine, generating random key material, and disposing safely.</p>
</div>

<div class="bodu-card">
  <h3><a href="cipher-modes.md">Cipher block modes</a></h3>
  <p>ECB, CBC, CFB, OFB, CTR, CTS, and XTS — one worked round-trip per mode, with the IV rules, the encrypt-vs-decrypt-primitive table, and when each is appropriate.</p>
</div>

<div class="bodu-card">
  <h3><a href="padding.md">Padding</a></h3>
  <p>PKCS7, ANSI X.923, ISO 10126, ISO/IEC 7816-4, Zeros, None — how each one pads, when it round-trips cleanly, and the padding-oracle caveat.</p>
</div>

<div class="bodu-card">
  <h3><a href="composing-primitives.md">Composing primitives</a></h3>
  <p>The two patterns side by side — manual <code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code>, and the equivalent through the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

<div class="bodu-card">
  <h3><a href="aes-family.md">AES-family block ciphers</a></h3>
  <p>AES, Twofish, Camellia, Serpent-128 — the four 128-bit-block ciphers compared, with selection guidance and BCL-vs-Bodu trade-offs.</p>
</div>

<div class="bodu-card">
  <h3><a href="hardware-acceleration.md">Hardware acceleration &amp; SIMD opt-out</a></h3>
  <p>Which primitives ship an AVX-512 fast path (BLAKE2/3, Threefish, CubeHash), when it engages, and the <code>Bodu.Security.Cryptography.DisableSimd</code> switch to force the scalar path.</p>
</div>

</div>

## Symmetric ciphers — Standard

| Cipher | Block | Key | Guide |
|---|---|---|---|
| `Skipjack` | 64 bits (8 B) | 80 bits (10 B) | [Using Skipjack](skipjack.md) |
| `Blowfish` | 64 bits (8 B) | 32–448 bits, 8-bit steps | [Using Blowfish](blowfish.md) |
| `Camellia` | 128 bits (16 B) | 128 / 192 / 256 bits | [AES-family block ciphers](aes-family.md) |
| `Twofish` | 128 bits (16 B) | 128 / 192 / 256 bits | [AES-family block ciphers](aes-family.md) |
| `Serpent128` | 128 bits (16 B) | 128 / 192 / 256 bits | [AES-family block ciphers](aes-family.md) |
| `AesBlockCipher` | 128 bits (16 B) | 128 / 192 / 256 bits | [AES-family block ciphers](aes-family.md) (raw `IBlockCipher` over the BCL `Aes`) |

## Symmetric ciphers — Tweakable

| Cipher | Block | Key | Tweak | Guide |
|---|---|---|---|---|
| `Threefish256` | 256 bits (32 B) | 256 bits (32 B) | 128 bits (16 B) | [Using Threefish-256](threefish-256.md) |
| `Threefish512` | 512 bits (64 B) | 512 bits (64 B) | 128 bits (16 B) | [Using Threefish-512](threefish-512.md) |
| `Threefish1024` | 1024 bits (128 B) | 1024 bits (128 B) | 128 bits (16 B) | [Using Threefish-1024](threefish-1024.md) |
| `Serpent256` / `Serpent512` / `Serpent1024` | 256 / 512 / 1024 bits | matching key | 128 bits | (no dedicated guide — non-standard wide-block constructions; see API reference) |

## Symmetric ciphers — Stream

Raw, confidentiality-only XOR keystream ciphers — **no authentication**; pair with a MAC or prefer AEAD. See [Using stream ciphers](stream-ciphers.md).

| Cipher | Key | Nonce / IV | Notes |
|---|---|---|---|
| `ChaCha20` | 256 bits (32 B) | 96 bits (12 B) | RFC 8439; the modern default. |
| `XChaCha20` | 256 bits (32 B) | 192 bits (24 B) | Extended nonce — safe to choose at random. |
| `Salsa20` | 128 / 256 bits | 64 bits (8 B) | eSTREAM; 64-bit nonce needs a counter. |
| `XSalsa20` | 256 bits (32 B) | 192 bits (24 B) | Extended-nonce Salsa20 (NaCl). |
| `Rabbit` | 128 bits (16 B) | 64 bits (8 B) | RFC 4503; evolving-state (no seekable counter). |
| `Hc128` | 128 bits (16 B) | 128 bits (16 B) | eSTREAM; expensive table-based setup. |

## Symmetric ciphers — AEAD

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="aead-modes.md">AEAD modes</a></h3>
  <p>GCM, CCM, OCB3, EAX, SIV, GCM-SIV — authenticated encryption with associated data using <code>AesBlockCipher</code> + the mode transforms, via the one-shot extension methods.</p>
</div>

</div>

## Cryptographic hashes

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="hashing.md">Hashing overview</a></h3>
  <p>Cross-cutting overview of keyed hashes (SipHash, Poly1305), cryptographic digests (Tiger, CubeHash, Snefru), and Merkle trees.</p>
</div>

<div class="bodu-card">
  <h3><a href="tiger.md">Using Tiger</a></h3>
  <p>128 / 160 / 192-bit cryptographic digest optimized for 64-bit platforms; two padding variants (Tiger / Tiger2).</p>
</div>

<div class="bodu-card">
  <h3><a href="cubehash.md">Using CubeHash</a></h3>
  <p>SHA-3 finalist with tunable rounds and block size.</p>
</div>

<div class="bodu-card">
  <h3><a href="snefru.md">Using Snefru</a></h3>
  <p>Snefru-128 / Snefru-256 — legacy cryptographic digest; interoperability use only.</p>
</div>

<div class="bodu-card">
  <h3><a href="merkle-trees.md">Using Merkle trees</a></h3>
  <p>Tree-structured streaming integrity over any inner <code>HashAlgorithm</code>.</p>
</div>

</div>

The library also exposes `Whirlpool`, `Blake2b`, `Blake2s`, `Blake3`, `Skein256` / `Skein512` / `Skein1024`, and `Shake` without dedicated walk-throughs yet — consult the [API reference](xref:Bodu.Security.Cryptography) directly.

## Keyed hashes (MAC)

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="siphash.md">Using SipHash</a></h3>
  <p>SipHash-64 and SipHash-128 — keyed PRF designed for hash-flooding-resistant hash tables.</p>
</div>

<div class="bodu-card">
  <h3><a href="poly1305.md">Using Poly1305</a></h3>
  <p>One-time authenticator (RFC 8439); pair with ChaCha20 or AES-CTR.</p>
</div>

</div>

## ASCON family

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="ascon.md">ASCON overview</a></h3>
  <p>All five NIST SP 800-232 types — Hash256, HashA256, XOF128, CXOF128, AEAD128 — with selection guidance.</p>
</div>

<div class="bodu-card">
  <h3><a href="ascon-hashing.md">ASCON hashing</a></h3>
  <p><code>AsconHash256</code> (12-round, max margin) and <code>AsconHashA256</code> (8-round, higher throughput).</p>
</div>

<div class="bodu-card">
  <h3><a href="ascon-xof.md">ASCON extendable output (XOF)</a></h3>
  <p><code>AsconXof128</code> and <code>AsconCxof128</code> — squeeze any number of bytes; CXOF accepts a domain customization string.</p>
</div>

<div class="bodu-card">
  <h3><a href="ascon-aead.md">ASCON authenticated encryption (AEAD)</a></h3>
  <p><code>AsconAead128</code> — sponge-based AEAD with no separate block cipher dependency.</p>
</div>

</div>

## Key derivation

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="hkdf.md">Using HKDF</a></h3>
  <p><code>Hkdf</code> (RFC 5869) — extract-and-expand key derivation for high-entropy inputs such as a shared secret or KEM output.</p>
</div>

<div class="bodu-card">
  <h3><a href="argon2.md">Using Argon2</a></h3>
  <p><code>Argon2id</code> / <code>Argon2i</code> / <code>Argon2d</code> (RFC 9106) — memory-hard password hashing and key derivation, with PHC encoded-hash <code>Hash</code> / <code>Verify</code>.</p>
</div>

<div class="bodu-card">
  <h3><a href="scrypt.md">Using scrypt</a></h3>
  <p><code>Scrypt</code> (RFC 7914) — the established memory-hard password KDF, with PHC encoded-hash <code>Hash</code> / <code>Verify</code>.</p>
</div>

</div>

## One-time passwords

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="one-time-passwords.md">Using HOTP and TOTP</a></h3>
  <p><code>Hotp</code> (RFC 4226) and <code>Totp</code> (RFC 6238) — the counter- and time-based one-time-password codes used for two-factor authentication, with constant-time verification and clock-drift windows.</p>
</div>

</div>

## Asymmetric algorithms

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="asymmetric-overview.md">Asymmetric algorithms overview</a></h3>
  <p>The four families over <code>AsymmetricAlgorithm</code> — key agreement, signatures, and post-quantum KEM / signatures — with selection guidance and the shared key import / export shape.</p>
</div>

<div class="bodu-card">
  <h3><a href="key-agreement-x25519.md">Key agreement with X25519</a></h3>
  <p><code>X25519</code> (RFC 7748) — Diffie-Hellman over Curve25519 for deriving a shared secret between two parties.</p>
</div>

<div class="bodu-card">
  <h3><a href="signatures-ed25519.md">Signatures with Ed25519</a></h3>
  <p><code>Ed25519</code> (RFC 8032) — deterministic EdDSA signing and verification over Curve25519.</p>
</div>

<div class="bodu-card">
  <h3><a href="ml-kem.md">ML-KEM post-quantum key encapsulation</a></h3>
  <p><code>MLKem512</code> / <code>MLKem768</code> / <code>MLKem1024</code> (FIPS 203) — lattice-based key encapsulation resistant to quantum attack.</p>
</div>

<div class="bodu-card">
  <h3><a href="ml-dsa.md">ML-DSA post-quantum signatures</a></h3>
  <p><code>MLDsa44</code> / <code>MLDsa65</code> / <code>MLDsa87</code> (FIPS 204) — lattice-based digital signatures resistant to quantum attack.</p>
</div>

<div class="bodu-card">
  <h3><a href="hpke.md">Hybrid public key encryption with HPKE</a></h3>
  <p><code>Hpke</code> (RFC 9180) — encrypt to a public key by composing the X25519 KEM, HKDF, and an AEAD; single-shot and session APIs across all four modes.</p>
</div>

</div>

## Where to go next

- [Bodu.Security.Cryptography introduction](../../docs/cryptography/index.md) — namespaces, headline types, scenarios.
- [Bodu.Security.Cryptography getting started](../../docs/cryptography/getting-started.md) — install and minimal samples per subfamily.
- [Bodu.IO.Hashing guides](../io-hashing/index.md) — non-cryptographic checksums and fingerprints.
- [Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography) — full type-by-type docs.
- **[Hashing & Cryptography guides](../topics/hashing-and-cryptography.md)** — every guide in this topic, across Bodu.IO.Hashing and Bodu.Security.Cryptography.
