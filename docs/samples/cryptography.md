---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Security.Cryptography`
under
[`samples/Security.Cryptography/`](https://github.com/bslater/bodu/tree/master/samples/Security.Cryptography).
All four samples are **offline and deterministic** — they use fixed keys, nonces, IVs, and salts
(RFC/NIST test-vector material where applicable) and print lowercase hex, so output is
reproducible — and are members of `bodu.slnx`, built and executed by CI, so the code they show
cannot drift from the current API. Each sample's README documents every scenario individually:
its intent, what the code does, the output to expect, and the APIs demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Security.Cryptography/<SampleName>
```

## The samples

### Bodu.Security.Cryptography.Samples.HashingMacAndKdf

Digests and derivation: the cryptographic hashes (BLAKE2b, BLAKE3, Tiger, Skein-256/512/1024,
Whirlpool), keyed hashing and MAC (SipHash-64/128, keyed BLAKE2b, Poly1305), extendable output
(SHAKE128, Ascon-XOF128), incremental hashing with `AppendData` and `VerifyHash`, key derivation
(HKDF, Argon2id, scrypt) against fixed salts, and the RFC 4226 / RFC 6238 one-time-password
generators (HOTP against the RFC test vectors, TOTP against an injected fixed time).
*Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.SymmetricAndAead

Symmetric encryption: single-block round-trips across the block ciphers (Threefish-256/512/1024,
Twofish, Camellia, Serpent-128, Skipjack, Blowfish), the CBC/PKCS7 and CTR cipher modes, the
AEAD constructions (AsconAead128 and AES-GCM/EAX/OCB with authenticated-tamper rejection), and
the stream ciphers (ChaCha20, XChaCha20, Salsa20) — all with fixed keys and nonces.
*Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.AsymmetricKeys

Public-key algorithms: X25519 key agreement against the RFC 7748 vectors, Ed25519 sign/verify
with a fixed seed and tamper rejection, ML-KEM-512/768/1024 encapsulation/decapsulation, and
ML-DSA-44/65/87 sign/verify. Because the post-quantum key generation and encapsulation draw
randomness, those scenarios print only deterministic facts — agreement and verification booleans
and fixed byte sizes — never secret or signature bytes.
*Package: `Bodu.Security.Cryptography`.*

### Bodu.Security.Cryptography.Samples.CustomHash

A consumer-authored hash: `AdditiveDigest` subclasses the library's `BlockHashAlgorithm` base
(implementing the block/finalization hooks with a parameterless constructor and a `Variant`
enum) and then composes identically to the built-ins through the shared `HashAlgorithm` surface.
Its companion `Bodu.Security.Cryptography.Samples.CustomHash.Test` project derives the library's
own `BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>` contract base — supplying a
`HashAlgorithmSpecification` and known-answer rows — so the consumer type is proven against the
exact contract the built-in hashes pass. *Package: `Bodu.Security.Cryptography`.*

## Related

- [IO.Hashing samples](io-hashing.md) — the non-cryptographic checksum and check-digit side of
  the hashing story.
