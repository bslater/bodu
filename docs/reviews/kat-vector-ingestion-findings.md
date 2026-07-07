# KAT Vector Ingestion — Findings

Follow-up to the crypto KAT coverage audit (review 2). Official / near-official
known-answer vectors were pinned against the implementation and run to detect
bugs, on the GCM-SIV precedent (a masked non-compliance that a symmetric
round-trip could not catch). Each vector set below was **independently
recomputed** before any conclusion was drawn, so a mismatch could be attributed
to the implementation, not to an unverified vector.

## CBC-CTS (CS3) — BUG FOUND AND FIXED

`CtsModeTransform` claimed CS3 / IEEE-1619 / NIST SP 800-38A Addendum interop
but its ciphertext-stealing path omitted the CBC chaining of the final block:
`EncryptCts` raw-encrypted the padded last block `P_n || E[tail..]` instead of
encrypting `(P_n || 0) XOR E`, and `DecryptCts` mirrored the omission — so the
mode round-tripped self-consistently while producing ciphertext matching no
standard CBC-CTS.

- **Detection:** AES-128 CBC-CS3 vectors derived from the NIST SP 800-38A F.2.1
  example inputs (17/31/47-byte plaintexts). Bodu emitted `0x89…` for the
  17-byte case versus the standard `0xB8…`. Ground truth was recomputed with a
  pure-Python AES-128 + CS3 implementation.
- **Fix:** XOR the zero-padded final plaintext block with the penultimate
  ciphertext block `E` before the raw encryption (encrypt) and XOR it off on
  decrypt. Block-aligned path (plain CBC, correct for SP 800-38A without
  stealing) unchanged.
- **Commit:** _Fix CBC-CTS to chain the final block; pin NIST-derived CS3 KAT_.

## Threefish 256 / 512 / 1024 — CONFIRMED (provenance upgraded)

The pinned `DefaultKeyAndTweak` ciphertexts already match the Skein/Threefish
golden KAT as mirrored in Crypto++ `threefish.txt` (Test Vectors 6/8/10,
little-endian word64 decode) **byte-for-byte**. No bug. Provenance upgraded from
`InternalRegression` (self-captured) to `ReferenceImplementation`, closing the
earlier "self-referential" gap with a second independent external source.

## SHAKE128 / SHAKE256 — CONFIRMED (coverage extended)

Existing vectors are correct. Added FIPS 202 rows over message inputs the
variant harness did not cover (`0x00`, `0x00010203`, lowercase `"abc"`) and, for
SHAKE256, the 512-bit output length. All expected outputs recomputed from the
FIPS 202 SHAKE definitions.

## Ascon-XOF128 and Ascon-CXOF128 — BUG FOUND AND FIXED

Once the raw ascon-c `LWC_XOF_KAT_128_512` and `LWC_CXOF_KAT_128_512` files
became available, an independent SP 800-232 Ascon reference — validated against
the official `Ascon-Hash256("")` vector (`0B3BE585…`, exact match) — was used to
adjudicate. That reference reproduces **all 1025 XOF and all 1089 CXOF** reference
rows, and it exposed that Bodu's `AsconXof128` / `AsconCxof128` were **entirely
non-compliant** with NIST SP 800-232. Their previously pinned "reference" digests
were **self-captured from the broken implementation** (e.g. the XOF empty-message
digest `D2AE52E6…` is the exact output of the buggy code), so the existing tests
were green over wrong answers — the same masking pattern as the GCM-SIV and
CBC-CTS bugs.

Three independent defects, each sufficient to break interop:

1. **Wrong permutation round count.** Both XOFs passed `absorptionRounds = 8` to
   the sponge base. SP 800-232 Ascon-XOF128 / CXOF128 use the full 12-round
   `Ascon-p[12]` for every absorption and squeeze round (the reduced-round "a"
   variants from Ascon v1.2 were dropped from the standard). Fixed to `12`.
2. **Wrong IV constants.** The pre-computed initial-state words for both XOF128
   and CXOF128 did not correspond to `p12` of the SP 800-232 raw IVs
   (`0x0000080000cc0003` for XOF128, `0x0000080000cc0004` for CXOF128). Replaced
   with the correct post-`p12` states (verified against the reference files).
3. **Wrong CXOF customization construction.** `Customize` absorbed `Z` directly,
   padded, and XORed `1` into state word S4 — an older Ascon draft's domain
   separation. SP 800-232 instead absorbs `LE64(bitlength(Z)) || Z` as a
   length-prefixed stream, then closes the phase with padding and `p12` (no
   S4 XOR). Rewritten accordingly; the now-dead `XorS4` base helper was removed.

The fix pins the exact official ascon-c vectors (both the corrected in-tree
sequential-input digests and a verbatim batch of file rows whose customization
`Z` begins at `0x10`). Full crypto regression is green.

**Note on the earlier interim provenance note:** the user's first hand-transcribed
CXOF snippets could not be reconciled and were correctly *not* pinned at that
point; the authoritative raw KAT files resolved it decisively.
