---
title: Bodu.Security.Cryptography guides
---

# Bodu.Security.Cryptography guides

Recipe-style walk-throughs for **Bodu.Security.Cryptography**, organised by the type hierarchy of the library: foundations → standard ciphers → tweakable ciphers → AEAD → cryptographic hashes → keyed hashes → ASCON.

If you're looking for the generated API reference, see the [Bodu.Security.Cryptography namespace page](../../apidoc/Bodu.Security.Cryptography.md). For non-cryptographic checksums (CRC, Fletcher), see the [Bodu.IO.Hashing guides](../io-hashing/). Not sure which primitive to use? See the [algorithm families overview](../algorithm-families.md).
If you have not yet installed the package or want the high-level shape of the library, start with the [Bodu.Security.Cryptography introduction](../../docs/cryptography/index.md) and the [getting-started page](../../docs/cryptography/getting-started.md). Not sure which primitive to use? See [Algorithm families](../../docs/algorithm-families.md).

For the auto-generated API reference, see the [Bodu.Security.Cryptography namespace page](../../apidoc/Bodu.Security.Cryptography.md). For non-cryptographic checksums and fingerprints, see the [Bodu.IO.Hashing guides](../io-hashing/index.md).

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
  <p>ECB, CBC, CFB, OFB, CTR — one worked round-trip per mode, with notes on when each is appropriate and when it is not.</p>
</div>

<div class="bodu-card">
  <h3><a href="padding.md">Padding</a></h3>
  <p>PKCS7, Zeros, None — how each one pads, when it round-trips cleanly, and when it silently loses bytes.</p>
</div>

<div class="bodu-card">
  <h3><a href="composing-primitives.md">Composing primitives</a></h3>
  <p>The two patterns side by side — manual <code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code>, and the equivalent through the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

</div>

## Symmetric ciphers — Standard

| Cipher | Block | Key | Guide |
|---|---|---|---|
| `Skipjack` | 64 bits (8 B) | 80 bits (10 B) | [Using Skipjack](skipjack.md) |
| `Blowfish` | 64 bits (8 B) | 32–448 bits, 8-bit steps | [Using Blowfish](blowfish.md) |
| `Camellia` | 128 bits (16 B) | 128 / 192 / 256 bits | (no dedicated guide — see API reference) |
| `Twofish` | 128 bits (16 B) | 128 / 192 / 256 bits | (no dedicated guide — see API reference) |
| `Serpent128` | 128 bits (16 B) | 128 / 192 / 256 bits | (no dedicated guide — see API reference) |

## Symmetric ciphers — Tweakable

| Cipher | Block | Key | Tweak | Guide |
|---|---|---|---|---|
| `Threefish256` | 256 bits (32 B) | 256 bits (32 B) | 128 bits (16 B) | [Using Threefish-256](threefish-256.md) |
| `Threefish512` | 512 bits (64 B) | 512 bits (64 B) | 128 bits (16 B) | [Using Threefish-512](threefish-512.md) |
| `Threefish1024` | 1024 bits (128 B) | 1024 bits (128 B) | 128 bits (16 B) | [Using Threefish-1024](threefish-1024.md) |
| `Serpent256` / `Serpent512` / `Serpent1024` | 256 / 512 / 1024 bits | matching key | 128 bits | (no dedicated guide — non-standard wide-block constructions; see API reference) |

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
  <p>128 / 160 / 192-bit cryptographic digest optimised for 64-bit platforms; two padding variants (Tiger / Tiger2).</p>
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

The library also exposes `Whirlpool`, `Blake2b`, `Blake2s`, `Blake3`, `Skein256` / `Skein512` / `Skein1024`, and `Shake` without dedicated walk-throughs yet — consult the [API reference](../../apidoc/Bodu.Security.Cryptography.md) directly.

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
  <p><code>AsconXof128</code> and <code>AsconCxof128</code> — squeeze any number of bytes; CXOF accepts a domain customisation string.</p>
</div>

<div class="bodu-card">
  <h3><a href="ascon-aead.md">ASCON authenticated encryption (AEAD)</a></h3>
  <p><code>AsconAead128</code> — sponge-based AEAD with no separate block cipher dependency.</p>
</div>

</div>

## Where to go next

- [Bodu.Security.Cryptography introduction](../../docs/cryptography/index.md) — namespaces, headline types, scenarios.
- [Bodu.Security.Cryptography getting started](../../docs/cryptography/getting-started.md) — install and minimal samples per subfamily.
- [Algorithm families](../../docs/algorithm-families.md) — cipher subtypes, hash structural shapes, cross-library map.
- [Bodu.IO.Hashing guides](../io-hashing/index.md) — non-cryptographic checksums and fingerprints.
- [Bodu.Security.Cryptography API reference](../../apidoc/Bodu.Security.Cryptography.md) — full type-by-type docs.
