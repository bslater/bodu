# Security.Cryptography Samples

Console applications demonstrating the `Bodu.Security.Cryptography` package. Each sample is a
standalone project; run one with:

```bash
dotnet run --project samples/Security.Cryptography/<SampleName>
```

Every sample is offline and deterministic. All keys, IVs, nonces, salts, and associated data are
fixed (RFC/NIST test-vector material where applicable) and digests/ciphertext/keys print as
lowercase hex, so output is reproducible. The Merkle sample reproduces the RFC 6962 appendix D
vectors byte for byte, so its roots can be checked against the RFC by eye. Where post-quantum key generation or encapsulation
draws randomness, those scenarios print only deterministic facts (agreement/verification
booleans and byte sizes). The `CustomHash.Test` project runs with the library test suites in CI.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` | Cryptographic hashes (BLAKE2b/BLAKE2s/BLAKE3/Tiger/Skein/Whirlpool/Snefru/CubeHash/Ascon), the parameter- and variant-selected families (`CubeHash` tuning, `Tiger.Variant`, `Whirlpool.Version`, the customizable `AsconCxof128`), keyed hashing and MAC (SipHash, keyed BLAKE2b, Poly1305), extendable output (SHAKE128, Ascon-XOF128), incremental hashing with `AppendData`/`VerifyHash`, the `IHashAlgorithmFactory<T>` seam and the comparable `HashValue`, key derivation (HKDF, Argon2id, scrypt), password hashing across all three Argon2 variants with PHC-encoded `Hash`/`Verify` and `ScryptParameters`, and the RFC 4226/6238 HOTP/TOTP generators | `Bodu.Security.Cryptography` |
| `Bodu.Security.Cryptography.Samples.SymmetricAndAead` | Single-block round-trips across the block ciphers (Threefish/Twofish/Camellia/Serpent/Skipjack/Blowfish), the wide-block tweakable Serpent variants, the CBC/PKCS7 and CTR cipher modes, the AEAD constructions (AsconAead128 and AES-GCM/EAX/OCB with tamper rejection) plus the extended-nonce XChaCha20-Poly1305 and XSalsa20-Poly1305 secretbox (which rejects associated data), and the stream ciphers (ChaCha20/XChaCha20/Salsa20/XSalsa20/Rabbit/HC-128) | `Bodu.Security.Cryptography` |
| `Bodu.Security.Cryptography.Samples.MerkleTrees` | The RFC 6962 Merkle-tree family — roots and audit paths with the leaf/node domain separation, the length-bound root (`BindRoot` / `VerifyInclusionBound`) that closes the tree-size ambiguity, append-only consistency proofs, the blocked/streaming/async surface with `MerkleBlockComputation`, write-time accumulation via `MerkleBlockAccumulator`, the `fanOut` and `maxDegreeOfParallelism` knobs (including the `NotSupportedException` on proofs for a non-binary tree), and the `MerkleTreeDiagnostics` trace | `Bodu.Security.Cryptography` |
| `Bodu.Security.Cryptography.Samples.AsymmetricKeys` | X25519 key agreement (RFC 7748 vectors), Ed25519 sign/verify with tamper rejection, ML-KEM-512/768/1024 encapsulation/decapsulation, and ML-DSA-44/65/87 sign/verify | `Bodu.Security.Cryptography` |
| `Bodu.Security.Cryptography.Samples.CustomHash` (+ `.Test`) | A consumer-authored `AdditiveDigest` subclassing the `BlockHashAlgorithm` base and composing identically to the built-ins; the test project derives the shared `BlockHashAlgorithmTests<AdditiveDigestTests, AdditiveDigest, AdditiveDigest.Variant>` contract base with a `HashAlgorithmSpecification` and known-answer rows | `Bodu.Security.Cryptography` |

Each sample project has its own README with the four-part per-scenario breakdown (Intent /
What it does / What to expect / APIs demonstrated).
