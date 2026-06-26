# Asymmetric cryptography review checklist

Review gates for the asymmetric algorithms (`X25519`, `Ed25519`, `MLKem*`, `MLDsa*`) and the
HPKE protocol surface. Apply this checklist when adding or changing any asymmetric algorithm,
key codec, or protocol. Each gate names the property to verify and where it is currently
enforced; a new algorithm is not "done" until every applicable row is satisfied.

> The review tooling is a checklist by design. Per the forensic review, ordinary line-coverage
> numbers do not prove much for this code; what matters is that each structural property below has
> a dedicated negative or boundary test. A failed gate is a missing test, not a missing percentage.

## 1. Import / trust-boundary validation

Every public `Import*` method is a trust boundary and must reject malformed input.

- [ ] **Length.** Wrong-length input is rejected (`ArgumentException`) — enforced generically by
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
      `ArgumentException`, and unsupported formats surface `NotSupportedException` — never the inherited
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
      documents the non-aliasing precondition (`X25519Tests.DeriveSharedSecret_WhenDestinationAliasesPeerKey_*`).
- [ ] **Destination length.** Wrong-length destinations are rejected; the span overload matches the
      allocating overload (per-family span-overload tests).
- [ ] **Zero on failure.** Sensitive destinations are zeroed before throwing
      (`X25519Tests.DeriveSharedSecret_WhenPeerKeyIsLowOrderPoint_ShouldZeroDestinationBeforeThrowing`).

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
