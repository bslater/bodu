# Bodu.Security.Cryptography.Samples.AsymmetricKeys

The asymmetric half of `Bodu.Security.Cryptography`: elliptic-curve key agreement and signatures (X25519,
Ed25519) plus the NIST post-quantum families (ML-KEM key encapsulation, ML-DSA signatures). The classical
scenarios import fixed keys and are fully reproducible; the post-quantum scenarios generate fresh keys and
print only the derived, deterministic outcomes. Offline; no data files, no key files.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.AsymmetricKeys
```

> **Determinism note.** X25519 and Ed25519 import *fixed* private keys, so their public keys, shared secret,
> and signature reproduce exactly (and X25519 cross-checks the RFC vectors). ML-KEM and ML-DSA generate a
> fresh key pair each run — and ML-KEM encapsulation and hedged ML-DSA signing draw randomness — so their
> ciphertext / secret / signature bytes differ every run. Those scenarios therefore print only what *is*
> deterministic: the agreement and verification booleans and the fixed byte sizes, never the secret bytes.

## Scenario 1 — KeyAgreementX25519

**Intent.** Show a Diffie-Hellman key agreement: two parties who have only exchanged public keys arrive at
the same shared secret, which no eavesdropper can compute.

**What it does.** Imports Alice's and Bob's private scalars from the RFC 7748 §6.1 test vectors, exports
their public keys (cross-checking the RFC), then has each party derive the shared secret from the other's
public key.

**What to expect.**

```text
--- X25519 key agreement (RFC 7748 6.1 vectors) ---

  Alice public : 8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a  (matches RFC: True)
  Bob public   : de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f  (matches RFC: True)

  Alice derives: 4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742
  Bob derives  : 4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742
  secrets agree: True
  matches RFC  : True
```

Both parties compute the identical secret, and it equals the published RFC value — the whole point of the
agreement, reproduced from the fixed private keys.

**APIs demonstrated.** `X25519.Create`, `ImportPrivateKey`, `ExportPublicKey`, `DeriveSharedSecret`.

## Scenario 2 — SignaturesEd25519

**Intent.** Show a digital signature: a signer with a private key produces a signature that anyone with the
public key can verify, and that fails for any modified message.

**What it does.** Imports a fixed 32-byte seed, signs a message, and prints the deterministic public key and
64-byte signature. A verifier holding only the public key accepts the genuine signature and rejects it after
one message byte is flipped.

**What to expect.**

```text
--- Ed25519 sign / verify (fixed seed) ---

  public key : 03a107bff3ce10be1d70dd18e74bc09967e4d6309ba50d5f1ddc8664125531b8
  signature  : 6c35aa8bfb6658934c4d47d2a90de806777c8dc7599519efed9441927b5bf589f95b3080ac185ec6ada22166ebafadc0ba1d7a3617187b6635f2e7536690ae03

  verify (genuine message)  = True
  verify (tampered message) = False
```

Ed25519 key generation and signing are deterministic, so the public key and signature are fixed functions of
the seed and message — the private seed never leaves the signer.

**APIs demonstrated.** `Ed25519.Create`, `ImportPrivateKey`, `ExportPublicKey`, `SignData`, `ImportPublicKey`,
`VerifyData`.

## Scenario 3 — KemMlKem

**Intent.** Show a post-quantum key-encapsulation mechanism (ML-KEM / FIPS 203): instead of agreeing on a
secret from two static keys, a sender *encapsulates* a fresh secret against a receiver's public key, and the
receiver decapsulates the ciphertext to recover it.

**What it does.** For ML-KEM-512, -768, and -1024, the receiver generates a key pair, the sender encapsulates
against the exported encapsulation key, and the receiver decapsulates. It also flips a ciphertext byte to
show ML-KEM's implicit rejection — a tampered ciphertext yields a *different* secret rather than throwing.

**What to expect.**

```text
--- ML-KEM key encapsulation (FIPS 203) ---

  ML-KEM-512  : secrets agree=True  (secret 32B, ciphertext 768B)
      tampered ciphertext -> different secret (implicit rejection): True
  ML-KEM-768  : secrets agree=True  (secret 32B, ciphertext 1088B)
      tampered ciphertext -> different secret (implicit rejection): True
  ML-KEM-1024 : secrets agree=True  (secret 32B, ciphertext 1568B)
      tampered ciphertext -> different secret (implicit rejection): True
```

The shared secret is always 32 bytes; the ciphertext grows with the parameter set (768 / 1088 / 1568 bytes).
The secret and ciphertext *bytes* are random per run, so only the agreement and rejection booleans and the
sizes are printed.

**APIs demonstrated.** `MLKem512` / `MLKem768` / `MLKem1024`, `GenerateKey`, `ExportEncapsulationKey`,
`ImportEncapsulationKey`, `Encapsulate`, `Decapsulate`.

## Scenario 4 — SignaturesMlDsa

**Intent.** Show post-quantum digital signatures (ML-DSA / FIPS 204) with the same accept / reject contract
as Ed25519, across all three parameter sets.

**What it does.** For ML-DSA-44, -65, and -87, the signer generates a key pair and signs; the verifier holds
only the public key, accepts the genuine signature, and rejects it after the message is tampered with.

**What to expect.**

```text
--- ML-DSA sign / verify (FIPS 204) ---

  ML-DSA-44 : verify(genuine)=True  verify(tampered)=False  (signature 2420B)
  ML-DSA-65 : verify(genuine)=True  verify(tampered)=False  (signature 3309B)
  ML-DSA-87 : verify(genuine)=True  verify(tampered)=False  (signature 4627B)
```

The signature size grows with the parameter set (2420 / 3309 / 4627 bytes). ML-DSA signing is hedged with
randomness, so the signature bytes vary per run — only the verification outcomes and sizes are printed.

**APIs demonstrated.** `MLDsa44` / `MLDsa65` / `MLDsa87`, `GenerateKey`, `SignData`, `ExportPublicKey`,
`ImportPublicKey`, `VerifyData`.

## Layout

```text
Bodu.Security.Cryptography.Samples.AsymmetricKeys/
  Program.cs                        # runs the scenarios in order
  Hex.cs                            # shared lowercase-hex encode/decode helpers
  Scenarios/KeyAgreementX25519.cs
  Scenarios/SignaturesEd25519.cs
  Scenarios/KemMlKem.cs
  Scenarios/SignaturesMlDsa.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` — hashes, MACs, XOFs, KDFs, and OTPs.
- `Bodu.Security.Cryptography.Samples.SymmetricAndAead` — block ciphers, cipher modes, AEAD, stream ciphers.
