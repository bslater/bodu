# Asymmetric cryptography review checklist

Review gates for the asymmetric algorithms (`X25519`, `Ed25519`, `MLKem*`, `MLDsa*`) and the
HPKE protocol surface. Apply this checklist when adding or changing any asymmetric algorithm,
key codec, or protocol. Each gate names the property to verify and where it is currently
enforced; a new algorithm is not "done" until every applicable row is satisfied. Section 7 is the
exception in scope: it applies to every vectorized kernel in the package, whatever the primitive.
Section 8 covers timing, which no unit test observes: its tests hold the values, and reading the
generated code holds the timing.

> The review tooling is a checklist by design. Per the forensic review, ordinary line-coverage
> numbers do not prove much for this code; what matters is that each structural property below has
> a dedicated negative or boundary test. A failed gate is a missing test, not a missing percentage.

## 1. Import / trust-boundary validation

Every public `Import*` method is a trust boundary and must reject malformed input.

- [ ] **Length.** Wrong-length input is rejected (`ArgumentException`) - enforced generically by
      `AsymmetricAlgorithmTests.ImportMembers_WhenGivenMalformedInput_ShouldReject`.
- [ ] **Canonical encoding.** Non-canonical encodings are rejected, not folded:
  - ML-DSA `s1`/`s2` packed code points > 2η (`MLDsaContractTests.ImportPrivateKey_WhenS1PackingIsNonCanonical_*`).
  - Ed25519 non-canonical `y` / small-order points (`Ed25519Tests.ImportPublicKey_WhenKeyIsSmallOrder_*`).
  - RFC 8410 structures: wrong OID and malformed DER (`*Tests.KeyFormats`).
- [ ] **Internal consistency.** The decoded object is checked for self-consistency, not just shape:
  - ML-DSA `t0` is validated by recomputation (`MLDsaContractTests.ImportPrivateKey_WhenEmbeddedT0IsCorrupted_*`).
  - ML-KEM decapsulation key validates the embedded ek modulus even with a regenerated `H(ek)`
    (`MLKemContractTests.ImportDecapsulationKey_WhenEmbeddedKeyNonCanonicalButHashConsistent_*`).
- [ ] **Exception type.** Container parsers surface `CryptographicException`, raw codecs surface
      `ArgumentException`, and unsupported formats surface `NotSupportedException` - never the inherited
      `NotImplementedException` (`AsymmetricAlgorithmTests.*KeyFormat*`).

## 2. Secret-material lifetime

- [ ] **Zeroization.** Every secret-bearing or secret-derived `byte[]` / `int[]` scratch buffer is
      cleared before it goes out of scope (`CryptographyHelper.Clear` / `ClearAndNullify`). Classify
      each new allocation as public, secret, or derived-public; clear the latter two.
- [ ] **Key replacement.** Replacing key material zeroizes the prior material atomically.
- [ ] **Disposal.** `Dispose` zeroizes all key material and every subsequent operation throws
      `ObjectDisposedException` (`AsymmetricAlgorithmTests.Dispose_*` and the per-family bases).

## 3. Protocol counters and state machines

- [ ] **Preflight overflow.** Every monotonic protocol counter is checked *before* doing cryptographic
      work, not after (HPKE `ThrowIfMessageLimitReached`;
      `HpkeTests.Seal_WhenSequenceLimitReached_*` / `Open_WhenSequenceLimitReached_*`).
- [ ] **Output bounds.** Length-bounded outputs validate the bound at the public API layer
      (HPKE `Export` 255·Nh; `HpkeTests.Export_WhenLengthOutsideBounds_*`).

## 4. Span-writing APIs

- [ ] **Aliasing.** Each span-writing API either tolerates input/output aliasing (with a test) or
      documents the non-aliasing precondition (`X25519Tests.DeriveSharedSecret_WhenDestinationAliasesPeerKey_*`;
      the Poly1305 AEADs' exact in-place round trip, for a message drawn in one pass through a stack
      buffer and for a longer one, in `StreamAeadTransformContractTests.EncryptDecrypt_WhenExactInPlace_*`).
- [ ] **Destination length.** Wrong-length destinations are rejected; the span overload matches the
      allocating overload (per-family span-overload tests).
- [ ] **Zero on failure.** Sensitive destinations are zeroed before throwing
      (`X25519Tests.DeriveSharedSecret_WhenPeerKeyIsLowOrderPoint_ShouldZeroDestinationBeforeThrowing`),
      or left unwritten until the operation has succeeded (the Poly1305 AEADs verify the tag before
      releasing any plaintext, on both of the framings' paths:
      `Poly1305AeadCoreTests.Open*_WhenTagIsTampered_ForEitherPath_ShouldThrowWithoutWritingOutput`).

## 5. Specification conformance

- [ ] **Official vectors.** The algorithm is pinned against its published vectors (NIST ACVP /
      Wycheproof / RFC). Boundary and iterative vectors are included where the spec defines them
      (e.g. RFC 7748 §5.2 iterated ladder).
- [ ] **Verification policy.** Any deviation from the reference acceptance set (e.g. Ed25519
      cofactorless verification) is documented on the public API and backed by divergence tests.
- [ ] **Differential (stretch).** Where practical, a cross-implementation differential test runs in a
      separate CI category. *(Not yet wired; see the forensic review's P3.)*

## 6. BCL façade

- [ ] **Descriptors.** `AlgorithmName` and `SecurityStrengthBits` are exposed; PQ semantics are not
      inferred from `KeySize` alone (`AsymmetricAlgorithmTests.AlgorithmDescriptors_*`).
- [ ] **KeySize / LegalKeySizes.** Deterministic and documented; reassignment preserves key state
      (`AsymmetricAlgorithmTests.KeySize_WhenReassignedAcrossKeyStates_*`).
- [ ] **Unsupported members.** Every unsupported inherited member throws a deliberate, documented
      exception with a test.

## 7. Vector kernels (every primitive)

Applies when adding or changing any SIMD kernel - the AVX-512 kernels, the GHASH / POLYVAL
kernels (PCLMULQDQ, PMULL and the scalar multiply), Argon2's AVX2, SSSE3 and AdvSimd kernels,
scrypt's SSE2 and AdvSimd BlockMix kernels, the BLAKE2b and BLAKE2s compression kernels,
Poly1305's AVX2 and AVX-512 block kernels, the four-way Keccak permutation, and ML-DSA's and
ML-KEM's AVX2 transform, product and ML-DSA's per-coefficient kernels.

- [ ] **Data-independent.** The kernel uses arithmetic, rotations, XORs and shuffles by constant
      indices only - no branch on, and no memory access indexed by, the data. Anything the algorithm
      itself makes data-dependent (Argon2's reference-block choice, scrypt's read of `V[j]`) happens
      outside the kernel (`Argon2Core.IArgon2Kernel`, `ScryptCore.IScryptKernel` and the
      `IVector128Isa` remarks). ML-KEM's and ML-DSA's rejection samplers parse the XOF streams
      outside the four-way permutation, exactly as they parse one stream at a time. Serpent-128's
      counter kernels carry between the counter's words by masks, so no branch depends on the
      counter, which EAX derives from the key (`SerpentCore.Vector256Kernel.AddToCounters`,
      `SerpentCore.AdvanceCounter`).
- [ ] **Bit-identical to the scalar reference.** A differential test compares the kernel with the
      scalar path on seeded random inputs (`Argon2CoreTests.FillBlock_*`,
      `GhashTests.Update_*_ShouldMatchBitSerialReference*`,
      `ScryptCoreTests.BlockMix_WhenUnitsAreRandom_ForEachKernel_ShouldMatchScalarKernel`,
      `ScryptCoreTests.ROMix_WhenBlocksAreRandom_ForEachKernel_ShouldMatchScalarKernel`,
      `Blake2bCoreTests` / `Blake2sCoreTests.Compress_WhenStatesAreRandom_ForEachKernel_ShouldMatchScalarKernel`,
      `Poly1305CoreTests.Update_*_ForEachKernel_ShouldMatchReferenceImplementation`,
      `KeccakPermutationTests.Permute4_*_ForEachKernel_ShouldMatchTheScalarPermutation`,
      `KeccakSponge4Tests.*_ForEachKernel_*`, the `MLDsaEngineTests` / `MLKemEngineTests`
      `*_ForEachKernel_*` tests over edge, run-patterned, signed and seeded polynomials, and
      `SerpentCoreTests.XorCounterKeystream_*_ForEachKernel_*` over every length to forty blocks and
      every carry between the counter's words).
- [ ] **Every kernel meets the published vectors.** The vector corpus runs through each kernel the
      host supports, not only the one dispatch picks
      (`Argon2CoreTests.DeriveTag_WhenEachSupportedKernelFillsTheMatrix_*`,
      `GhashTests.Update_*_ShouldMatchPublishedValue`, `ScryptCoreTests.*_ForEachKernel_*` over RFC
      7914's Salsa20/8, BlockMix and ROMix vectors and the OpenSSL corpus, and the BLAKE2 cores'
      `Compress_WhenHashing*_ForEachKernel_*` over RFC 7693's examples and the official blake2-kat.json,
      and `Poly1305CoreTests.Update_WhenGivenRfc8439AppendixA3Vector_ForEachKernel_ShouldProduceTag`,
      and `KeccakSponge4Tests.Squeeze_WhenAbsorbingFips202Messages_ForEachKernel_*`), and
      `Bodu.Security.Cryptography.Simd.Test` holds the scalar path to the same vectors - for ML-KEM
      and ML-DSA, the linked ACVP suites.
- [ ] **Gated and switchable.** Dispatch goes through a `SimdCapabilities` gate that honours the
      `DisableSimd` switch, and `SimdOptOutTests` asserts the gate is closed under it.
- [ ] **Architecture-only code runs somewhere.** Every shim operation has a test against its scalar
      definition (`Argon2CoreTests.*_ForEachIsa_*`, `GhashTests.*_ForEachIsa_*`,
      `ScryptCoreTests.RotateLanes_*_ForEachIsa_*`, `Blake2bCoreTests.RotateRight_*_ForEachIsa_*`,
      `Blake2sCoreTests.*_ForEachIsa_*`), and ARM64-only
      files (`*.AdvSimd.cs`, `*.PmullIsa.cs`) run in `build-test.yml`'s ARM64 job, which
      `SimdCapabilitiesTests.AdvSimd_WhenProcessIsArm64_*` fails if the gate is closed there.
- [ ] **Scratch is cleared.** Scratch holding secret-derived words is cleared when the kernel's
      caller finishes with it (Argon2's per-segment scratch, H0 and the matrix; scrypt's `B`, `V` and
      ROMix scratch; the powers of `r` a Poly1305 kernel run computes on the stack, cleared as the run
      ends; ML-KEM's 16-bit working copy of each polynomial; the four-way sponge's state and every
      block of a secret or mask stream it squeezes, cleared by the samplers; `Argon2CoreTests.DeriveTag_WhenMatrixStartsWithGarbage_*` and
      `ScryptCoreTests.ROMix_*Garbage*` show neither needs zeroing before use).

## 8. Secret-dependent timing

Applies to every path that takes a private key or a value derived from one: X25519's ladder and
key generation, Ed25519's signing, and ML-KEM's decapsulation.

- [ ] **No branch on, and no index by, a secret.** Secret scalars are recoded, and table entries
      chosen, by arithmetic and masks alone:
  - Ed25519's fixed-base multiplication, which X25519 key generation shares, recodes the scalar into
    signed digits with shifts and masks (`Ed25519Point.RecodeSignedRadix16`), reads all eight
    entries of a row to keep one, negating it by a conditional swap and move
    (`Ed25519Point.SelectBaseMultiple`), and adds the top digit's carry by a conditional move
    (`Ed25519Point.ScalarMultBase`). The tests hold the selection to a plain lookup for every
    signed digit of every row
    (`Ed25519PointTests.SelectBaseMultiple_ForEverySignedDigitOfEveryRow_ShouldMatchAPlainLookup`)
    and the recoding to the scalar's value (`Ed25519PointTests.RecodeSignedRadix16_*`).
  - The Montgomery ladder (`Curve25519.ScalarMult`) swaps by masks
    (`Curve25519FieldElement.ConditionalSwap` / `ConditionalMove`;
    `Curve25519FieldElementTests.ConditionalSwap_*` / `ConditionalMove_*`), and inversion is a fixed
    addition chain.
  - ML-KEM's decapsulation compares the re-encryption and selects the shared secret or the
    implicit-rejection key without a branch (`CryptographyHelper.ConstantTimeDifference` /
    `ConstantTimeSelect`; `CryptoHelpersTests.ConstantTimeDifference_*` / `ConstantTimeSelect_*`).
  - When one of these methods changes, read its generated code on both runtimes
    (`DOTNET_JitDisasm`, with `DOTNET_TieredCompilation=0`) and confirm that its only conditional
    jumps are loop and bounds checks that no secret decides.
- [ ] **Variable time over public values only.** A routine whose timing depends on its inputs says
      so in its name or remarks and is given only public values. Ed25519 verification's
      `Ed25519Point.DoubleScalarMultBaseVartime` and its `ComputeNonAdjacentForm` recoding see the
      signature's S, the challenge k and the public key, and `Ed25519Scalar.IsCanonical` compares
      the public S; no signing or key-generation path calls them.
