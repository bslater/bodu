---
title: Bodu.Security.Cryptography guides
---

# Bodu.Security.Cryptography guides

Recipe-style walk-throughs for using **Bodu.Security.Cryptography** in real applications — encrypting and decrypting data with the right mode, padding, and IV; and choosing the right hash for the job.

If you're looking for the generated API reference, see the [Bodu.Security.Cryptography namespace page](../../apidoc/Bodu.Security.Cryptography.md). For non-cryptographic checksums (CRC, Fletcher), see the [Bodu.IO.Hashing guides](../io-hashing/).

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
  <h3><a href="padding.html">Padding</a></h3>
  <p>PKCS7, Zeros, and None — how each one pads, when it round-trips cleanly, and when it silently loses bytes.</p>
</div>

<div class="bodu-card">
  <h3><a href="composing-primitives.html">Composing primitives</a></h3>
  <p>The two patterns side by side — manual <code>IBlockCipher</code> + <code>BlockCipherModeFactory</code> + <code>PaddingFactory</code>, and the equivalent through the <code>SymmetricAlgorithm</code> wrappers.</p>
</div>

</div>

## Symmetric ciphers

Each of the five block ciphers ships with its own page containing a complete encrypt-and-decrypt walk-through using its native sizes:

| Cipher | Block | Key | Extras | Guide |
|---|---|---|---|---|
| `Threefish256` | 256 bits (32 B) | 256 bits (32 B) | 128-bit tweak | [Using Threefish-256](threefish-256.md) |
| `Threefish512` | 512 bits (64 B) | 512 bits (64 B) | 128-bit tweak | [Using Threefish-512](threefish-512.md) |
| `Threefish1024` | 1024 bits (128 B) | 1024 bits (128 B) | 128-bit tweak | [Using Threefish-1024](threefish-1024.md) |
| `Skipjack` | 64 bits (8 B) | 80 bits (10 B) | — | [Using Skipjack](skipjack.md) |
| `Blowfish` | 64 bits (8 B) | 32–448 bits, 8-bit steps | — | [Using Blowfish](blowfish.md) |

All five share the same high-level shape: a `SymmetricAlgorithm` (or `TweakableSymmetricAlgorithm`) that you configure with `BlockMode`, `Padding`, `Key`, `IV` (and `Tweak` for Threefish), then drive with `CreateEncryptor()` / `CreateDecryptor()` or the `Encrypt` / `Decrypt` extension methods.

## Hashing

[Using hashes and checksums](hashing.md) — concrete recipes for the classic non-cryptographic families (Adler, FNV, CityHash), keyed short-input hashes (SipHash), cryptographic digests (Tiger), and streaming integrity with `MerkleTreeHash` / `ParallelMerkleTreeHash`.

For CRC and Fletcher checksums on `System.IO.Hashing.NonCryptographicHashAlgorithm`, see the [Bodu.IO.Hashing guides](../io-hashing/).

## Related concepts

- [Cipher modes diagram walkthrough](../../api/Bodu.Security.Cryptography.CipherBlockMode.html) — the pedagogical data-flow panels for each of the five classic modes.
- [AEAD modes overview](../../api/Bodu.Security.Cryptography.GcmModeTransform.html) — how GCM, CCM, OCB, EAX, SIV, and GCM-SIV relate.
- [Merkle tree construction](../../api/Bodu.Security.Cryptography.MerkleTreeHash.html) and the [parallel pipeline diagram](../../api/Bodu.Security.Cryptography.ParallelMerkleTreeHash.html).

## Authenticated encryption

For authenticated-encryption-with-associated-data (AEAD), pair the public <xref:Bodu.Security.Cryptography.AesBlockCipher> with one of the mode transforms — `GcmModeTransform`, `CcmModeTransform`, `OcbModeTransform`, `SivModeTransform`, or `GcmSivModeTransform` — and call through the one-shot <xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions> helpers. The [AEAD modes guide](aead-modes.md) walks through each mode end-to-end.
