---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Security.Cryptography`
under
[`samples/Security.Cryptography/`](https://github.com/bslater/bodu/tree/master/samples/Security.Cryptography).
All eight samples are **offline and deterministic** - they use fixed keys, nonces, IVs, and salts
(RFC/NIST test-vector material where applicable) and print lowercase hex, so output is
reproducible. Where post-quantum key generation or an HPKE ephemeral key draws randomness, the
scenario prints only deterministic facts: agreement and verification booleans, and byte sizes.
Every sample is a member of `bodu.slnx`, built and executed by CI, so the code it shows cannot
drift from the current API; the contract-test companion runs with the test suites. Each sample's
README documents every scenario individually: its intent, what the code does, the output to
expect, and the APIs demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Security.Cryptography/<SampleName>
```

## The samples

### Bodu.Security.Cryptography.Samples.HashingMacAndKdf

Digests and derivation: the cryptographic hashes (BLAKE2b, BLAKE2s, BLAKE3, Tiger, Skein-256/512/1024,
Whirlpool, Snefru, CubeHash, Ascon), keyed hashing and MAC (SipHash-64/128, keyed BLAKE2b, Poly1305),
extendable output (SHAKE128, Ascon-XOF128), incremental hashing with `AppendData` and `VerifyHash`,
the `IHashAlgorithmFactory<T>` seam and the comparable `HashValue`, key derivation (HKDF, Argon2id,
scrypt) against fixed salts, password hashing with PHC-encoded `Hash`/`Verify`, and the RFC 4226 /
RFC 6238 one-time-password generators (HOTP against the RFC test vectors, TOTP against an injected
fixed time). *Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.SymmetricAndAead

Symmetric encryption: single-block round-trips across the block ciphers (Threefish-256/512/1024,
Twofish, Camellia, Serpent-128, Skipjack, Blowfish) and the wide-block tweakable Serpent variants,
the CBC/PKCS7 and CTR cipher modes, the AEAD constructions (AsconAead128 and AES-GCM/EAX/OCB with
authenticated-tamper rejection, XChaCha20-Poly1305, and the XSalsa20-Poly1305 secretbox), and the
stream ciphers (ChaCha20, XChaCha20, Salsa20, XSalsa20, Rabbit, HC-128) - all with fixed keys and
nonces. *Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.CipherModesAndPadding

The layer beneath `SymmetricAlgorithm.Mode`: the seven `IPaddingStrategy` implementations with
their actual trailer bytes and which of them can recover the original length, the confidentiality
mode transforms driven directly over an `IBlockCipher` (with ECB shown leaking identical blocks),
and the specialist modes - ciphertext stealing, XTS with per-sector tweaks, and CCM/SIV/GCM-SIV
with a measurement of what nonce-misuse resistance buys, including the detached-tag
`EncryptDetached` / `DecryptDetached` pair. *Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.AsymmetricKeys

Public-key algorithms: X25519 key agreement against the RFC 7748 vectors, Ed25519 sign/verify
with a fixed seed and tamper rejection, ML-KEM-512/768/1024 encapsulation/decapsulation, and
ML-DSA-44/65/87 sign/verify. Because the post-quantum key generation and encapsulation draw
randomness, those scenarios print only deterministic facts - agreement and verification booleans
and fixed byte sizes - never secret or signature bytes.
*Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.HybridEncryption

RFC 9180 HPKE: the single-shot `Hpke.Seal` / `Open` pair, all four establishment modes (base, PSK,
auth, auth-PSK) with each mode's extra input shown to be authenticated and the modes shown not to
be interchangeable, the multi-message `HpkeSender` / `HpkeReceiver` contexts (sequence-bound,
replay- and reorder-detecting), and the `HpkeSuite` surface with secret export, including the
export-only AEAD. *Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.MerkleTrees

The RFC 6962 Merkle-tree family, reproducing the RFC's appendix D vectors byte for byte: roots and
audit paths with the leaf/node domain separation, the length-bound root (`BindRoot` /
`VerifyInclusionBound`) that closes the tree-size ambiguity, append-only consistency proofs, the
blocked, streaming, and async surface with <xref:Bodu.Security.Cryptography.MerkleBlockComputation>,
write-time accumulation through <xref:Bodu.Security.Cryptography.MerkleBlockAccumulator>, the
`fanOut` and `maxDegreeOfParallelism` knobs (including the `NotSupportedException` on proofs for a
non-binary tree), and the <xref:Bodu.Security.Cryptography.MerkleTreeDiagnostics> trace.
*Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.StreamingPipelines

The `Bodu.Security.Cryptography.Extensions` surface over streams, the way an application uses it:
stream-to-stream encryption with Twofish and XChaCha20 (the read buffer size shown to be invisible
in the ciphertext, and the stream-cipher instance refusing to reuse its nonce); `EncryptAsync` /
`DecryptAsync` with cancellation, and the same extensions over the BCL's own `Aes`, held byte for
byte to `Aes.EncryptCbc`; a download verified against a published manifest with `VerifyHashAsync`,
a multi-part digest with `AppendDataAsync`, and untrusted manifest entries through the
non-throwing `TryVerifyHash` / `TryVerifyHashAsync`; and an encrypt-then-MAC sealed file composed
from HKDF, `EncryptAsync`, keyed BLAKE2b, and `TryCreateDecryptor` / `TransformAsync`, which
verifies the whole tag before decrypting a byte. *Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.CustomHash (+ .Test)

A consumer-authored hash: `AdditiveDigest` subclasses the library's `BlockHashAlgorithm` base
(implementing the block/finalization hooks with a parameterless constructor and a `Variant`
enum) and then composes identically to the built-ins through the shared `HashAlgorithm` surface.
Its companion `Bodu.Security.Cryptography.Samples.CustomHash.Test` project derives the library's
own `BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>` contract base - supplying a
`HashAlgorithmSpecification` and known-answer rows - so the consumer type is proven against the
exact contract the built-in hashes pass. *Package: `Bodu.Security.Cryptography`.*

## Extension methods in the samples

The extension classes in `Bodu.Security.Cryptography.Extensions` hang off the BCL abstractions
(`HashAlgorithm`, `SymmetricAlgorithm`, `ICryptoTransform`) as well as the library's own, so they
serve the framework's algorithms too. Where each one is shown running:

| Extension class | Members shown | Sample (scenario) |
|---|---|---|
| <xref:Bodu.Security.Cryptography.Extensions.HashAlgorithmExtensions> | `AppendData`, `VerifyHash(byte[], string)` | HashingMacAndKdf (StreamingAndVerify); `AppendData` also in CustomHash (BesideTheBuiltIns) |
| | `AppendDataAsync`, `VerifyHashAsync`, `TryVerifyHash`, `TryVerifyHashAsync` | StreamingPipelines (VerifyingDownloads, SealedFilePipeline) |
| <xref:Bodu.Security.Cryptography.Extensions.SymmetricAlgorithmExtensions> | `Encrypt(byte[])`, `Decrypt(byte[])` | SymmetricAndAead (CipherModes) |
| | `Encrypt` / `Decrypt(Stream, Stream)`, `EncryptAsync`, `DecryptAsync`, `TryCreateEncryptor`, `TryCreateDecryptor` | StreamingPipelines (EncryptingStreams, AsyncAndBclInterop, SealedFilePipeline) |
| <xref:Bodu.Security.Cryptography.Extensions.SymmetricStreamAlgorithmExtensions> | `Encrypt(byte[])`, `Decrypt(byte[])` | SymmetricAndAead (StreamCiphers, MoreCiphers) |
| | `Encrypt` / `Decrypt(Stream, Stream)` | StreamingPipelines (EncryptingStreams) |
| <xref:Bodu.Security.Cryptography.Extensions.ICryptoTransformExtensions> | `Transform(ReadOnlySpan<byte>)`, `TransformAsync(Stream, Stream, int)` | StreamingPipelines (AsyncAndBclInterop, SealedFilePipeline) |
| <xref:Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions> | `Encrypt`, `Decrypt`, `EncryptDetached`, `DecryptDetached` | SymmetricAndAead (AeadModes), CipherModesAndPadding (SpecialistModes) |
| <xref:Bodu.Security.Cryptography.Extensions.AeadTransformExtensions> | `Encrypt`, `Decrypt` | SymmetricAndAead (MoreCiphers) |

<xref:Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions> adds the same
`TryCreateEncryptor` / `TryCreateDecryptor` shape for the tweakable ciphers, with a tweak argument;
[Streams and async](../guides/cryptography/streaming-and-async.md) lists every stream and `Task`
member with its buffer size and cancellation behaviour.

## Related

- [IO.Hashing samples](io-hashing.md) - the non-cryptographic checksum and check-digit side of
  the hashing story, including the same stream and verify extension shapes for checksums.
- [Streams and async](../guides/cryptography/streaming-and-async.md) - the guide to the stream,
  async, and verify extensions these samples run.
