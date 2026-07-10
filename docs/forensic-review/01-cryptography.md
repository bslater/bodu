# WS-1 — Cryptography

**Packages:** `Bodu.Security.Cryptography/src/`, `Bodu.IO.Hashing/src/`.

**Overall assessment: ship-quality.** This is a carefully engineered cryptography library. Constant-time discipline is consistently applied, AEAD tag verification uniformly routes through `CryptographicOperations.FixedTimeEquals`, key material is comprehensively zeroized on `Dispose` and in `finally` blocks, and the hand-rolled Curve25519/Ed25519 field arithmetic is branch-free and matches the ref10 reference. **No Critical or High severity issue was found.**

## Findings

| # | file:line | category | severity | status | finding | recommendation |
|---|---|---|---|---|---|---|
| 1 | `Ed25519.cs:368-376` | Duplication | Low | CONFIRMED | The small-order-point rejection block (`if (rPoint.IsSmallOrder() \|\| publicPoint.IsSmallOrder()) return false;`) is present **twice**, verbatim, with an identical preceding comment, in the security-critical verify path. Correctness-neutral but dead-code duplication. | Delete the second copy (lines 373-376). |
| 2 | `Blake2b.Avx512.cs:79-88` | unsafe bounds | Low | PLAUSIBLE → cleared | `ProcessBlockAvx512` reads 16 `ulong`s via `Unsafe.As<byte,ulong>`/`Unsafe.Add` with no length check on the `block` span; a sub-128-byte span would over-read. Guarded in practice: the buffered-hash base only ever hands full 128-byte blocks, and the scalar path makes the same assumption (SIMD/scalar parity holds). | Optionally add `Debug.Assert(block.Length >= 128)` to document the caller contract. |
| 3 | `MLDsaEngine.Sampling.cs:57` (`RejBoundedPoly`) | const-time | Info | cleared | Half-byte rejection sampling of the secret polynomials from SHAKE256(ρ′‖nonce) is variable-time over secret-derived data — but this is the standard FIPS 204 reference behavior (the rejection outcome depends on pseudo-random bytes, not the long-term secret), so it is not a practical leak. `RejNttPoly` correctly operates on the public seed ρ. | No change; matches the specification. |
| 4 | `CryptographyHelper.RandomNumberGenerator.cs:141-156` | RNG | Info | cleared | `FillWithRandomBytesExcluding` uses an unbounded `while (buffer[i]==forbidden)` redraw. Terminates with probability 1 (geometric, p=1/256 per draw); a bounded variant (`TryFillWithRandomNonZeroBytes`, max 8 redraws) exists for the DoS-sensitive path. `GC.AllocateUninitializedArray` buffers are always fully overwritten before return. | No change. |

## Hot-path notes

- Field arithmetic (`Curve25519FieldElement`) accumulates 5×5 limb products in `UInt128` with a fixed carry chain — no per-call allocation, no boxing. `Invert`/`Pow22523` use fixed addition chains (constant-time, value-independent). The Montgomery ladder (`Curve25519.cs:66-90`) performs exactly one `ConditionalSwap` pair per bit with a mask, so the operation sequence is scalar-independent.
- Block-cipher key schedules are built once per key into pooled/`new` arrays and cleared on `Dispose`. Serpent's prekey/seed expansion buffers (`SerpentBlockCipher.cs:434,469-470`) and Serpent128's `prekeys`/`seed`/`paddedKey` are zeroed in `finally`.
- AEAD decrypt paths (`GcmModeTransform.cs:299-311`, `EaxModeTransform.cs:225-238`, `AsconAead128.cs:322-328`) verify the tag **before** emitting plaintext and `Clear` the output buffer on mismatch — correct verify-then-decrypt with no release of unverified plaintext.

## Architecture / alignment notes

- Constant-time comparison is well-centralized: `CryptographyHelper.ConstantTimeDifference`/`ConstantTimeSelect` (mask-derived, branch-free) drive ML-KEM's FIPS 203 implicit rejection (`MLKemEngine.cs:147-148`); all AEAD/MAC/OTP/hash-value comparisons route through `CryptographicOperations.FixedTimeEquals`. `HashValue`/`AuthenticationTag`/`SignatureValue`/`SecretBytes` each expose an explicit fixed-time `FixedTimeEquals` distinct from ordinary `Equals`, with XML docs steering callers to the right one.
- Ed25519 `VerifyData` compares recomputed R against signature R with `SequenceEqual` (`Ed25519.cs:398`) — correct, since both operands are public; a fixed-time compare is not required, and the code documents this.
- X25519 correctly implements RFC 7748 §6.1: clamps a stack copy (never the stored key), rejects the all-zero shared secret with a constant-time accumulation in `Curve25519.ScalarMult`, and offers `IsLowOrderPoint` as a branch-free preflight. Key formats restricted to raw + RFC 8410; XML/encrypted PKCS#8 throw `NotSupportedException`.
- Sibling ciphers follow a consistent `IBlockCipher` + `*Transform` shape with shared abstract bases (`SerpentBlockCipherBase`, `ThreefishBlockCipher`); AVX-512 variants are gated on `Avx512F.VL.IsSupported` with scalar fallbacks.

## Duplication notes

- Finding #1 (Ed25519 duplicated small-order check) is the only genuine code duplication found in a security path.
- The `MLKemEngine.cs:430` local `CryptographicOperationsFixedTimeEquals` wrapper is a redundant one-line indirection around the BCL call — cosmetic.

## Convention notes

- No hard-coded exception message literals in production code (grep hits resolve to doc-comment `<code>` examples in `HashValue.cs`/`AuthenticationTag.cs`); all throws use `CryptoResourceStrings`/`HashingResourceStrings` with `CultureInfo.CurrentCulture`. File-scoped namespaces and one-type-per-file observed throughout.
