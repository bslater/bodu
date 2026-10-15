---
title: Conformance and test vectors
---

# Conformance and test vectors

A library that implements a standard is only as trustworthy as the evidence that it follows it. This page lists what each Bodu package is held to: the specifications and published test vectors its tests check against, the conformance corpora it runs, the defects other libraries have fixed in the same formats, and the places where Bodu deliberately or unavoidably differs. Every source named here is committed to the repository or pinned by version and hash, so each check is reproducible from a clone.

Use it to judge how a behavior was established before you depend on it, and to know which differences from another library are decisions rather than defects.

## Kinds of evidence

| Kind | What it is | Weight |
|---|---|---|
| **Specification vectors** | Test vectors published with a standard: the RFC appendices, NIST's ACVP and SP 800-232 vectors, the reference implementations' known-answer files. | Normative: a mismatch is a Bodu defect. |
| **Conformance suites** | A format's own published suite, run case by case: toml-test, yaml-test-suite, the CRC catalogue's check values. | Normative for the cases the package claims to support. |
| **Official published data** | For calendars, the dates a government or institution publishes: gazettes, official holiday feeds, almanacs. | Authoritative for the years they cover. |
| **Third-party comparison** | Another implementation's test suite or output, restated in Bodu's dialect. | Evidence, not authority: agreement between implementations does not outvote a specification. |
| **Release-note fix catalogues** | Every defect fix other libraries list in their release notes for the same format, each with its scenario. | A scenario Bodu must handle, unless Bodu documents a different behavior. |

## Hashing and cryptography

`Bodu.Security.Cryptography` is held to the published vectors of every algorithm it implements. The test suite's known-answer census (`Bodu.Security.Cryptography/test/kat-census.txt`) records 7,232 vectors across eleven families - digests, AEAD, block ciphers, KDFs, signatures, key agreement, key encapsulation, key generation and validation, stream ciphers, and HPKE - and a second test assembly runs the known-answer suites again with SIMD acceleration switched off, so the scalar paths are held to the same vectors as the accelerated ones.

| Area | Sources |
|---|---|
| Public-key | RFC 7748 and the Wycheproof X25519 set (X25519); RFC 8032 and the Wycheproof EdDSA set (Ed25519); NIST ACVP vectors for ML-KEM (FIPS 203) and ML-DSA (FIPS 204); RFC 9180 (HPKE). |
| AEAD | NIST SP 800-232 and the reference implementation's known-answer files (ASCON); NIST SP 800-38D (GCM); RFC 3610 and the Wycheproof AES-CCM set (CCM); RFC 7253 (OCB); the EAX paper and the Wycheproof AES-EAX set; RFC 5297 and the Wycheproof AES-SIV-CMAC set (SIV); RFC 8452 and the Wycheproof AES-GCM-SIV set (GCM-SIV); the XChaCha20 Internet-Draft vector and libsodium's `crypto_secretbox` vector (the Poly1305 stream AEADs). |
| Stream ciphers and MACs | RFC 8439 (ChaCha20, Poly1305); RFC 4503 (Rabbit); the eSTREAM vectors (Salsa20, HC-128); the reference SipHash vectors. |
| Digests and XOFs | The official BLAKE2 and BLAKE3 known-answer files; the NESSIE sets (Tiger); the ISO test vectors (Whirlpool); the Skein, CubeHash, and Snefru reference vectors. |
| KDFs and OTP | RFC 9106 and the reference implementation's traces (Argon2); RFC 7914 and an OpenSSL-generated corpus (scrypt); RFC 5869 (HKDF); RFC 4226 and RFC 6238 (HOTP, TOTP). |

The package is **not independently audited**; the [security posture guide](../../guides/cryptography/security-posture.md) states which operations are documented as constant-time, what is zeroized, and what the tests can and cannot show.

`Bodu.IO.Hashing` checks every one of its 112 named CRC standards against the check value the [RevEng CRC catalogue](https://reveng.sourceforge.io/crc-catalogue/all.htm) publishes for the input `123456789`, and its fingerprints, checksums, and check digits against their published known answers.

## Text and serialization

| Package | Held to |
|---|---|
| `Bodu.Text.Encoding` | The RFC 4648 vectors (Base16, Base32, Base64), RFC 9285 (Base45), BIP 173 and BIP 350 (Bech32, Bech32m), the Z85 specification, and the published vectors of the other alphabets. |
| `Bodu.Text.Toml` | The [toml-test](https://github.com/toml-lang/toml-test) corpus, vendored with its MIT license: 266 valid and 505 invalid cases, run under both the TOML 1.0.0 and 1.1.0 profiles. |
| `Bodu.Text.Yaml` | The [yaml-test-suite](https://github.com/yaml/yaml-test-suite), pinned as a git submodule: all 402 cases are classified, with none left unexplained. 255 parse and match their JSON, 12 parse without a value comparison, 94 are rejected as the suite expects, and 41 are valid YAML that the package's tree profile rejects by design (complex keys, anchor override, non-core tags, and the like). |
| `Bodu.Text.Delimited` | RFC 4180, and a regression corpus derived from csv-spectrum. |
| `Bodu.Text.DotEnv` | A conformance corpus derived from python-dotenv and godotenv. |
| `Bodu.Text.Ini` | Python's `configparser`, whose strict mode the `Strict` options preset matches. |
| `Bodu.Text.Configuration` | The EditorConfig specification, version 0.17.2. |

## Release-note fix catalogues

The structured-format readers are also held to the defects other libraries have fixed. For each format, the release notes of comparable libraries were read in full, and every fix they list became a row: the scenario the fix's regression test or issue describes, restated as input and expected output and run against Bodu. Each row is classed `applies` (Bodu must do what the fix established), `dialect` (Bodu documents a different behavior, with the reason), `n/a` (an API, language, or platform matter Bodu does not share), or `unknown` (no scenario could be found).

| Area | Runs against | Libraries read | Fixes | Rows | `applies` |
|---|---|---:|---:|---:|---:|
| `bencode` | `Bodu.Text.Bencode` | 7 | 77 | 135 | 76 |
| `toml` | `Bodu.Text.Toml` | 14 | 797 | 1,119 | 714 |
| `yaml` | `Bodu.Text.Yaml` | 9 | 994 | 1,505 | 889 |
| `delimited` | `Bodu.Text.Delimited` | 9 | 716 | 789 | 235 |
| `dotenv` | `Bodu.Text.DotEnv` | 8 | 314 | 506 | 254 |
| `ini` | `Bodu.Text.Ini` | 8 | 282 | 323 | 110 |
| `configuration` | `Bodu.Text.Configuration` | 6 | 135 | 195 | 93 |
| `compound` | `Bodu.IO.Compound` | 6 | 313 | 397 | 276 |
| `biff` | `Bodu.IO.Biff`, `Bodu.Formats.Excel.Binary` | 6 | 807 | 867 | 369 |
| `pst` | `Bodu.IO.Pst`, `Bodu.Formats.Outlook.Pst` | 4 | 521 | 583 | 212 |
| `msg` | `Bodu.Formats.Outlook.Msg` | 5 | 418 | 447 | 139 |
| `recurrence/cron-fixes` | `Bodu.Globalization.Recurrence` | 21 | 1,383 | 2,032 | 946 |

A row that fails is a Bodu defect: it gets an issue, a failing regression test, and then the fix. Each area's README in the repository's [`corpus/`](https://github.com/bslater/bodu/tree/master/corpus) directory lists its sources, when each was read, the licenses, and every `dialect` decision with its reason. No upstream file is committed: a row restates a minimal reproduction, and a library under a copyleft license contributes only inputs written for the row.

## Recurrence

RFC 5545 publishes its recurrence examples as prose, so they are transcribed: the standard's section 3.8.5.3 examples are held as official data. Everything else is third-party comparison - libical's test corpus, python-dateutil's output for generated sub-daily rules, and the test suites of 21 cron libraries restated in Bodu's dialect: 5,353 rows, of which 4,585 run and 768 are excluded, each under a flag that names the dialect difference. There is no cron standard. The closest is the `crontab(5)` manual of Vixie cron and its descendant cronie, which the corpus follows where another library disagrees with it, recording the disagreement.

## Calendars

The calendar engine and its data packs are reconciled against published dates, kept under [`corpus/`](https://github.com/bslater/bodu/tree/master/corpus) with each file's source class in its header:

| Source | Covers |
|---|---|
| GOV.UK bank-holiday feed, 2019-2028, archived verbatim | The United Kingdom's bank holidays |
| Sri Lanka government gazettes, 2023-2027 | Poya days |
| Bank of Thailand holiday notices, 2020-2026 | Thai financial-institution holidays |
| Government of India DoPT memorandum, 2026 | Indian central-government holidays |
| Universal House of Justice table, 172-221 B.E. | Baha'i holy days |
| IMD Rashtriya Panchang principal-festivals tables | Hindu festivals, 2022-2027 |
| The calcal implementation of Reingold and Dershowitz | Hindu calendar reference data, 1990-2039 (implementation evidence, not an authority) |

Each regional data pack's tests pin every floating or computed holiday - Easter offsets, nth-weekday rules, lunar and Hijri festivals, weekend substitutions - to confirmed published dates.

## Known limits

- **Moon-sighting festivals.** The engine computes the Hijri calendar by rule, while many countries fix Islamic festivals by local moon sighting, so a computed Eid can differ by one or two days from the date a country announces. The data packs' tests accept up to two days for these festivals, and exact dates everywhere else.
- **Umm al-Qura after 1450 AH.** No fixed, versioned month-start publication exists from 1451 AH onward, so those years have nothing published to reconcile against yet.
- **Hindu calendar models.** Bodu's Hindu lunar calculator follows a different astronomical model from the Reingold-Dershowitz family, so disagreements with the reference data are expected; each is classified (for example as convention-equivalent or model-induced) rather than silently resolved.
- **YAML profile.** `Bodu.Text.Yaml` implements a tree profile of YAML 1.2, not the full representation graph; the 41 yaml-test-suite cases it rejects by design are the features outside that profile. See [the YAML introduction](../serialization/yaml/index.md).
- **Cron dialects.** Cron libraries disagree, chiefly over the two day fields; Bodu's rules are stated in the [cron guide](../../guides/recurrence/cron.md), and the corpus rows a dialect difference excludes are recorded with the reason.
- **Cryptography audit.** Passing published vectors shows an implementation computes the right answers; it does not show resistance to side channels. See the [security posture guide](../../guides/cryptography/security-posture.md).

## Where to go next

- **[Reading untrusted input](untrusted-input.md)** - the limits and validation levels each reader applies to a crafted input.
- **[Errors and exceptions](errors-and-exceptions.md)** - what each package throws when an input fails the rules above.
- **[Versioning, stability, and support](../versioning-and-support.md)** - which releases receive fixes.
