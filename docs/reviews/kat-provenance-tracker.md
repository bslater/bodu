# KAT Provenance Tracker — Bodu.Security.Cryptography

Tracks each cipher / hash / MAC / AEAD / asymmetric algorithm and the provenance of its known-answer tests (KATs):
whether the tests load **dynamically from an embedded original vector file via a reader** (the target pattern), are
**inline literals transcribed from an official source**, are **self-referential** (no external authority exists), or are
**outstanding** (need an official vector file we don't yet have).

Legend for **Loading**:
- **Embedded+reader** — original vector file shipped as `EmbeddedResource`, parsed by a reader into KAT records, driven
  via `[DynamicData]`. This is the goal.
- **Inline-cited** — expected values are literal `[DataRow]`/records transcribed from a named official source (an RFC or
  FIPS worked-example table with no separate machine-readable vector file). Legitimate, but not file-driven.
- **Self-ref** — no external authority exists (bespoke construction); cross-checked against a second in-house
  implementation only. Cannot be pinned to a published source.
- **Outstanding** — needs an official vector file we do not yet hold.

---

## 1. Converted to embedded-file + reader (official source)

| Algorithm | Official source | Reader | Rows |
|---|---|---|---|
| Ascon-AEAD128 | NIST SP 800-232 / ascon-c `LWC_AEAD_KAT_128_128` | `NistLwcKatReader` | 1089 |
| Ascon-XOF128 | NIST SP 800-232 / ascon-c `LWC_XOF_KAT_128_512` | `NistLwcXofKatReader` | 1025 |
| Ascon-CXOF128 | NIST SP 800-232 / ascon-c `LWC_CXOF_KAT_128_512` | `NistLwcXofKatReader` | 1089 |
| SipHash-2-4-128 | veorq/SipHash `vectors.h` (`vectors_sip128`) | `SipHashReferenceVectorsReader` | 64 |
| SipHash-2-4 (64-bit) | veorq/SipHash `vectors.h` (`vectors_sip64`) | `SipHashReferenceVectorsReader` | 64 |
| Snefru-128 (8-pass) | Merkle 2.5a `testSnefru` / `correctSnefruOutput` | `MerkleSnefruKatReader` | 11 |
| Snefru-256 (8-pass) | Merkle 2.5a `testSnefru256` / `correctSnefru256Output` | `MerkleSnefruKatReader` | 11 |
| Tiger-192 | NESSIE `test-vectors-nessie-format.dat` | `NessieHashKatReader` | 650 |
| Tiger / Tiger2 (extra) | Biham + NESSIE `m:/h:` reference file | `TigerReferenceKatReader` | 25 |
| CubeHash 224/256/384/512 | SHA-3 Round 2 `ShortMsgKAT` (CubeHash16/32 = 160/16/32/160) | `Sha3ShortMsgKatReader` | 1024 |
| Skein 256/512/1024 (hash) | Skein 1.3 `skein_golden_kat_short` | `SkeinGoldenKatReader` | 13 |
| BLAKE2b / BLAKE2s | official `blake2-kat.json` (keyed + unkeyed) | `Blake2KatReader` | 1024 |
| BLAKE3 (unkeyed hash) | official `test_vectors.json` (github.com/BLAKE3-team/BLAKE3) | `Blake3KatReader` | 35 |
| Whirlpool (ISO/IEC 10118-3) | OpenSSL `evpmd_whirlpool.txt` (ISO test-message set) | `OpenSslDigestKatReader` | 9 |
| X25519 | Wycheproof | (pre-existing loader) | — |
| Ed25519 | Wycheproof | (pre-existing loader) | — |
| ML-KEM 512/768/1024 | NIST ACVP | `MLKemAcvpVectors` | — |
| ML-DSA 44/65/87 | NIST ACVP | `MLDsaAcvpVectors` | — |
| HPKE | RFC 9180 test vectors (JSON) | (pre-existing loader) | — |

## 2. Inline literals from an official source (cited, correct)

These are pinned to a named authority but are transcribed literals rather than a loaded file. Where a machine-readable
vector file exists they are **candidates for conversion**; where the source is only an RFC/FIPS worked-example table,
inline is the natural form.

| Algorithm | Source | Convertible to file? |
|---|---|---|
| AES (block) | FIPS-197 Appendix C | No separate file — FIPS table |
| Skipjack | FIPS-185 §8 + NSA reference | No separate file |
| Blowfish | Schneier / Eric Young `vectors-2.txt` | Yes — designer vector file exists |
| Twofish | Designer KAT set | Yes — designer `ecb_tbl.txt` |
| Camellia | RFC 3713 / RFC 5528 | RFC tables (+ NESSIE file exists) |
| Tiger-192 | — | **Converted (see §1)** |
| Threefish 256/512/1024 | Crypto++ `threefish.txt` (Skein golden KAT) | Yes — Crypto++ vector file |
| SHAKE128/256 | FIPS 202 / CAVP | Yes — NIST CAVP `.rsp` |
| Poly1305 | RFC 8439 §2.8.2 | RFC table |
| Whirlpool | — | **Converted (see §1)** — OpenSSL ISO evptests file |
| AES-GCM-SIV | RFC 8452 Appendix C.1 | RFC table |
| AES-CCM | Cross-checked vs BCL `AesCcm` oracle | Oracle, not a file |
| AES-SIV | RFC 5297 Appendix A.1 | RFC table |
| CBC-CTS (CS3) | Derived from NIST SP 800-38A F.2.1 | Derived (no NIST CTS file) |

## 3. Self-referential — no external authority (cannot be pinned)

| Algorithm | Note |
|---|---|
| Serpent-256/-512/-1024 (wide tweakable) | Bespoke; cross-checked vs in-house Python port only |
| CubeHash — non-standard round configs (`80`,`160`,`300`,`10`-round) | No published vectors for these parameters |
| Threefish — all-zero baseline row | Trivial self-capture (non-zero rows externally confirmed) |

## 4. Outstanding — need an official vector file (see §5 for what to supply)

| Algorithm | Needed | Status |
|---|---|---|
| Twofish / Blowfish | designer vector files | Inline today; convertible |
| ChaCha20 / XChaCha20 / Salsa20 / XSalsa20 | RFC 8439 / RFC 7539 / NaCl / DJB | To assess (some inline) |
| Rabbit | RFC 4503 test vectors | To assess |
| HC-128 | eSTREAM test vectors | To assess |
| FNV1a, Adler (Bodu.IO.Hashing) | reference vectors | Separate package — to assess |
| CRC catalogue (Bodu.IO.Hashing) | RevEng catalogue check values | Separate package — likely already catalogue-driven |

## 5. Files to supply (RFC / source detail)

Reachable from this environment: `raw.githubusercontent.com` and package registries only. NIST, technion, and most
other hosts return a hard 403 at the gateway, so for those, upload the file or paste a `raw.githubusercontent.com` link.

- **Tiger2**: `0x80`-padding variant, 192-bit. Crypto++ `TestVectors/` or OpenSSL. (Tiger uses `0x01`; Tiger2 uses `0x80`.)
- **CubeHash (standard)**: SHA-3 Round 2 submission KATs `ShortMsgKAT_{224,256,384,512}.txt` for **CubeHash16+16/32+32**
  (init 16 / per-block 16 / finalization 32, 32-byte block). The exotic-round Bodu configs are unpinnable — skip.
- **Skein**: Skein 1.3 NIST submission `skein_golden_kat` / `skein_golden_kat_internals`.
- **ChaCha20/Salsa20 family**: RFC 8439 §2.3.2 (ChaCha20 block) + the NaCl / DJB Salsa20 / XSalsa20 vectors.
- **Rabbit**: RFC 4503 Appendix A. **HC-128**: eSTREAM test vectors.

---

_This is a living document — update the tables as each algorithm is converted. One algorithm per commit._
