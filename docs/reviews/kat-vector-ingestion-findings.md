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

## Ascon-CXOF128 — UNRESOLVED (open item)

The user-supplied ascon-c `LWC_CXOF_KAT_128_512` vectors (customization `Z`
beginning at `0x10`) were **not pinned**. An independent SP 800-232 Ascon
implementation was built and **validated against the official `Ascon-Hash256("")`
vector** (`0B3BE585…`, exact match), confirming the permutation, little-endian
byte order, rate, and padding. Under the standard length-prefixed customization
(`LE64(|Z|·8) || Z`), that verified reference reproduced **neither** the
user-supplied vectors **nor** Bodu's runtime output.

Two facts stand unreconciled and need an authoritative CXOF vector to resolve:

1. **The user flagged these vectors as low-confidence** (the raw ascon-c KAT
   file body could not be retrieved; values came from partial snippets), so they
   are not reliable ground truth and were not pinned (pinning them would assert a
   red test on unverified data).
2. **Bodu's `AsconCxof128.Customize` omits any customization length-prefix** — it
   absorbs `Z` directly, pads, permutes, then XORs `1` into state word S4 for
   domain separation. That "XOR-1-into-S4" scheme matches an older Ascon draft
   rather than the NIST SP 800-232 length-prefixed construction. Whether this is
   a genuine non-compliance cannot be confirmed from memory of the customization
   encoding alone; the exact SP 800-232 CXOF customization procedure must be
   cross-checked against an authoritative published CXOF KAT before either
   pinning vectors or changing the implementation.

**Ascon-XOF128** remains without officially-verified vectors as well (the ascon-c
`LWC_XOF_KAT_128_512` file body was likewise unretrievable). Both CXOF128 and
XOF128 should be revisited once a NIST SP 800-232 XOF/CXOF example or the raw
ascon-c KAT file is available.
