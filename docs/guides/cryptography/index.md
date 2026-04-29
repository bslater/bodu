---
title: Bodu.Security.Cryptography guides
---

# Bodu.Security.Cryptography guides

Recipe-style walk-throughs for using **Bodu.Security.Cryptography** in real applications — encrypting and decrypting data with the right mode, padding, and IV; and choosing the right hash for the job.

If you're looking for the generated API reference, see the [Bodu.Security.Cryptography namespace page](../../apidoc/Bodu.Security.Cryptography.md). For non-cryptographic checksums (CRC, Fletcher), see the [Bodu.IO.Hashing guides](../io-hashing/). Not sure which primitive to use? See the [algorithm families overview](../algorithm-families.md).

## Start here

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="encryption-basics.html">Encryption basics</a></h3>
  <p>The mental model: <code>BlockMode</code> vs .NET's <code>Mode</code>, how Key / IV / Tweak / Padding combine, how to generate random key material, and how to dispose of it safely.</p>
</div>

<div class="bodu-card">
  <h3><a href="cipher-modes.html">Cipher block modes</a></h3>
  <p>ECB, CBC, CFB, OFB, CTR — one worked round-trip per mode, with notes on when each is appropriate and when it is not.</p>
</div>

<div class="bodu-card">
  <h3><a href="aead-modes.html">AEAD modes</a></h3>
  <p>GCM, CCM, OCB3, SIV, GCM-SIV — authenticated encryption with AES, using <code>AesBlockCipher</code> plus the one-shot extension methods.</p>
</div>

<div class="bodu-card">
  <h3><a href="ascon.html">ASCON family</a></h3>
  <p>All five NIST SP 800-232 algorithms — <code>AsconHash256</code>, <code>AsconHashA256</code>, <code>AsconXof128</code>, <code>AsconCxof128</code>, and <code>AsconAead128</code> — with algorithm selection guidance and quick-start examples.</p>
</div>

<div class="bodu-card">
  <h3><a href="padding.html">Padding</a></h3>
  <p>PKCS7, Zeros, and None — how each one pads, when it round-trips cleanly, and when it silently loses bytes.</p>
</div>

<div class="bodu-card">
  <h3><a href="composing-primitives.html">Composing primitives</a></h3>
  <p>The two patterns side by side — manual <code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code>, and the equivalent through the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

</div>

## Symmetric ciphers

The five ciphers below ship with a dedicated walk-through page covering an encrypt-and-decrypt round-trip in their native sizes:

| Cipher | Block | Key | Extras | Guide |
|---|---|---|---|---|
| `Threefish256` | 256 bits (32 B) | 256 bits (32 B) | 128-bit tweak | [Using Threefish-256](threefish-256.md) |
| `Threefish512` | 512 bits (64 B) | 512 bits (64 B) | 128-bit tweak | [Using Threefish-512](threefish-512.md) |
| `Threefish1024` | 1024 bits (128 B) | 1024 bits (128 B) | 128-bit tweak | [Using Threefish-1024](threefish-1024.md) |
| `Skipjack` | 64 bits (8 B) | 80 bits (10 B) | — | [Using Skipjack](skipjack.md) |
| `Blowfish` | 64 bits (8 B) | 32–448 bits, 8-bit steps | — | [Using Blowfish](blowfish.md) |

The library also exposes `Camellia`, `Twofish`, and `Serpent128` — three additional 128-bit-block standard ciphers each accepting 128-/192-/256-bit keys — and the wide-block tweakable variants `Serpent256` / `Serpent512` / `Serpent1024`. None of those have a per-cipher walk-through page yet; consult the [API reference](../../apidoc/Bodu.Security.Cryptography.md) directly. AES sits one level lower in the public surface, exposed as `AesBlockCipher` over the BCL `Aes` engine so it pairs naturally with the AEAD mode transforms — see the [AEAD modes guide](aead-modes.md).

All eleven share the same high-level shape: a `SymmetricAlgorithm` (or `TweakableSymmetricAlgorithm`) that you configure with `BlockMode`, `Padding`, `Key`, `IV` (and `Tweak` for the tweakable variants), then drive with `CreateEncryptor()` / `CreateDecryptor()` or the `Encrypt` / `Decrypt` extension methods. `AesBlockCipher` is the exception — it implements `IBlockCipher` directly rather than wrapping a `SymmetricAlgorithm` lifecycle.

## Hashing

Start with [Using hashes and checksums](hashing.md) for the cross-cutting picture — how the keyed, cryptographic, and Merkle-tree primitives relate, how to compute and verify digests, and when each one is the right tool. Then pick the per-algorithm page you need:

| Type | Shape | Guide |
|---|---|---|
| `SipHash64` / `SipHash128` | Keyed PRF — hash-flooding defence | [Using SipHash](siphash.md) |
| `Poly1305` | One-time authenticator | [Using Poly1305](poly1305.md) |
| `Tiger` | 128 / 160 / 192-bit cryptographic digest | [Using Tiger](tiger.md) |
| `CubeHash` | SHA-3 submission, tunable rounds and block size | [Using CubeHash](cubehash.md) |
| `Snefru128` / `Snefru256` | Legacy cryptographic digest — interop only | [Using Snefru](snefru.md) |
| `AsconHash256` / `AsconHashA256` | NIST SP 800-232 sponge digest — 256-bit output, two margin/throughput variants | [ASCON hashing](ascon-hashing.md) |
| `AsconXof128` / `AsconCxof128` | NIST SP 800-232 extendable output — squeeze any number of bytes; CXOF128 adds a customisation string | [ASCON XOF](ascon-xof.md) |
| `MerkleTreeHash` / `ParallelMerkleTreeHash` | Tree-structured streaming integrity | [Using Merkle trees](merkle-trees.md) |

For non-cryptographic checksums and fingerprints (CRC, Fletcher, Adler, FNV, CityHash, Pearson, and the classic string hashes) on `System.IO.Hashing.NonCryptographicHashAlgorithm`, see the [Bodu.IO.Hashing guides](../io-hashing/).

## Related concepts

- [Cipher modes diagram walkthrough](../../api/Bodu.Security.Cryptography.CipherBlockMode.html) — the pedagogical data-flow panels for each of the five classic modes.
- [AEAD modes overview](../../api/Bodu.Security.Cryptography.GcmModeTransform.html) — how GCM, CCM, OCB, EAX, SIV, and GCM-SIV relate.
- [Merkle tree construction](../../api/Bodu.Security.Cryptography.MerkleTreeHash.html) and the [parallel pipeline diagram](../../api/Bodu.Security.Cryptography.ParallelMerkleTreeHash.html).

## Authenticated encryption

For authenticated-encryption-with-associated-data (AEAD), you have two families of algorithms:

**AES-based AEAD** — pair <xref:Bodu.Security.Cryptography.AesBlockCipher> with one of the
five mode transforms (`GcmModeTransform`, `CcmModeTransform`, `OcbModeTransform`,
`SivModeTransform`, or `GcmSivModeTransform`) and call through the one-shot
<xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions> helpers.
The [AEAD modes guide](aead-modes.md) walks through each mode end-to-end.

**ASCON-AEAD128** — <xref:Bodu.Security.Cryptography.AsconAead128> is a sponge-based AEAD
(NIST SP 800-232) that requires no separate block cipher. Use it when you need a
standards-backed AEAD with a compact software footprint or when targeting hardware without
AES-NI. See the [ASCON AEAD guide](ascon-aead.md).
