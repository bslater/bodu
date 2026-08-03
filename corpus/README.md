# Bodu calendar validation corpus

External evidence used to validate the `Bodu.Globalization.Calendar` catalogues, kept in
the repository so every reconciliation is reproducible from committed artifacts. The
embedded regression vector tables live with the tests
(`Bodu.Globalization.Calendar/test/Globalization.Calendar/Fixtures/Vectors/`); this tree
holds the *sources they are reconciled against* and the tooling that produces reference
data.

## Naming (fixed — do not conflate these roles)

| Name | Role |
|---|---|
| **IMD Rashtriya Panchang Corpus** | Official published expected values (Government of India, IMD Positional Astronomy Centre). Published results tied to stated locations and conventions — not an executable oracle. |
| **calcal Modern Hindu Reference** | Primary executable oracle: the R package [`calcal`](https://github.com/robjhyndman/calcal) (Apache-2.0), pinned by version and archive hash. An independent implementation of the canonical algorithms — not official data. |
| **Calendrical C Comparison** | Secondary implementation-language comparison: [`i40west/calendrical`](https://github.com/i40west/calendrical). |
| **Reingold–Dershowitz Calendrical Calculations** | The canonical published algorithm family (book, 4th ed., CUP 2018). Algorithmic authority; licence-controlled source. |

## Source classes

Every data file in this tree declares one of (per row or in its provenance header):

- `official-published` — transcribed from an issued official edition.
- `official-astronomy-derived` — deterministically derived from a published official
  ephemeris, derivation recorded.
- `reference-generated` — produced by the pinned executable oracle.
- `third-party-comparison` / `independent-reconciliation` — another implementation or
  platform dataset (for example ICU-derived tables), suitable for cross-checks but not
  represented as a gazette.
- `unavailable` — the official edition has not been acquired or does not exist yet.
  Future official announcements are never invented.

## Lineage warning (no majority voting)

Implementations sharing the Reingold–Dershowitz source model (`calcal`, the C comparison)
are independent *implementations*, not independent *authorities*: agreement among them
does not outvote an official printed value. Bodu's own `HinduLunarCalculator` is **not**
an R–D port — it is a Meeus-series sidereal/tithi model — so Bodu-versus-calcal
comparisons are independent at both the algorithm-design and implementation level.
Disagreements between the two model families are expected and are classified
(`convention-equivalent`, `model-induced`, …) rather than silently resolved; see the
reconciliation tests in the calendar test project.

## What runs where

- **This repository / CI**: schema validation, corpus loading, and the exhaustive
  reconciliation tests (`HinduReferenceCorpusReconciliationTests` — **active** since the
  generated corpus was committed 2026-08-03). No R required.
- **Regeneration (user-side or a dedicated CI job)**: reference-corpus generation with
  the pinned R environment under `hindu/reference/calcal/`. The committed
  `hindu-reference-daily.csv` was generated 2026-08-03 from the user-supplied CRAN
  archive `calcal_1.0.4.tar.gz` (SHA-256 recorded in the reference README alongside
  `renv.lock`); its first activation caught — and led to the fix of — the engine's
  phantom-adhika lunation defect (see the verification report).
- **Acquisition**: official PDFs are *not* committed until redistribution rights are
  confirmed — commit their URL, SHA-256, and acquisition metadata instead (see
  `hindu/data/raw/imd-rashtriya-panchang/`).

## Layout

```text
corpus/
├── README.md                    this file
├── bahai/                       UHJ 50-year Badi table 172-221 B.E. (official-published)
├── hindu/
│   ├── data/
│   │   ├── raw/imd-rashtriya-panchang/   edition manifest + acquisition records (no PDFs)
│   │   ├── normalized/                    generated/transcribed CSVs land here
│   │   └── schemas/                       JSON Schemas for the daily/event/provenance shapes
│   └── reference/calcal/                  pinned R generator + environment verification
├── india/                       DoPT central-government holiday memoranda (official-published)
├── islamic/                     independent reconciliation tables (ICU-derived, KFUPM/van Gent evidence notes)
├── persian/                     independent reconciliation tables (ICU-derived)
├── sikh/                        SGPC Nanakshahi document inventory (acquisition records only)
├── sri-lanka/                   gazetted Poya days (official-published)
├── thailand/                    Bank of Thailand financial-institution holidays (official-published)
└── uk/                          GOV.UK bank-holiday feed 2019-2028, archived verbatim (official-published)
```

## Source register (research pass, 2026-08-03)

Datasets delivered by the external research pass, each verified before commit
(`tools/verify-bahai-poya-vectors.py` for the astronomical braces; per-file
`# verification:` header lines record the result):

| File | Rows | Source class | Source |
|---|---:|---|---|
| `bahai/uhj-holy-days-172-221-be.csv` | 550 | `official-published` (secondary reproduction of the UHJ primary) | Universal House of Justice 50-year Badi table, via the NZ national Baha'i institution's Holy Days PDF |
| `sri-lanka/poya-gazette-2023-2027.csv` | 62 | `official-published` | Sri Lanka government gazettes 2287/04, 2341/46, 2395/33, 2438/22, 2493/05 |
| `thailand/bot-financial-holidays-2020-2026.csv` | 126 | `official-published` (unofficial English translations; Thai text prevails) | Bank of Thailand annual + amendment notices |
| `india/dopt-holidays-2026.csv` | 51 | `official-published` | DoPT Office Memorandum F.No.12/2/2023-JCA (2026) |
| `sikh/sgpc-nanakshahi-inventory.csv` | 4 | acquisition records | SGPC Nanakshahi calendar/Jantri PDFs (Gurmukhi image-layout; no OCR transcription) |

## Hand-back deliveries (2026-08-03)

Sources the build environment could not reach (proxy-blocked hosts), fetched user-side
and handed back:

| File | Source class | Source |
|---|---|---|
| `uk/bank-holidays-2019-2028.json` | `official-published` (OGL v3.0; raw feed committed verbatim) | GOV.UK bank-holiday dataset — see `uk/README.md`; drives the 264-row GB pack sweep |
| `hindu/data/raw/imd-rashtriya-panchang/manifest.json` (`editions[]`) | acquisition records (link-and-hash; PDFs not committed) | IMD Rashtriya Panchang: the 1947 S.E. (2025-26) edition in all 14 listed languages plus the English 1944-1946 and 1948 S.E. editions; delivered via a transient GitHub release and chat uploads |
| `sikh/sgpc-nanakshahi-inventory.csv` (hash columns) | acquisition records (link-and-hash; PDFs not committed) | All four SGPC Nanakshahi documents (556-558 calendars + 557 Jantri) delivered and hash-pinned; controlled Gurmukhi transcription still outstanding |
| `hindu/data/normalized/hindu-reference-daily.csv` | `reference-generated` | 73,048 rows (1990-2039, four Hindu models) from the pinned calcal 1.0.4 oracle, generated in-container from the user-supplied CRAN archive; activates the reconciliation tests |
| `hindu/data/normalized/imd-rp-<saka>se-principal-festivals.csv` (×5) | `official-published` | The five English editions' Principal Festivals tables (501 rows, contiguous 2022-03-22 – 2027-04-19; 1947 transcribed from renders, the rest machine-extracted from text layers), verified by `tools/verify-imd-festival-vectors.py` (two documented printer's errata: 1947 row 52, 1948 row 82) |

Findings recorded by the same pass:

- **Umm al-Qura post-1450 AH**: the official KACST site (ummulqura.org.sa, relaunched
  2026) is an *interactive* calendar/converter service; **no fixed, versioned month-start
  publication for 1451 AH onward exists**. The vector tables' 1451+ residual is therefore
  "nothing published to reconcile yet" — interactive outputs, if ever sampled, are a
  separate source class (`official-interactive-output`), not `official-published`.
- **Drik Panchang**: transcription skipped — its terms assert copyright over data and no
  permission basis was established. Not used, not committed.

## Outstanding hand-backs (user-side)

- **SGPC Gurmukhi transcription** — dual-person controlled transcription of the four
  acquired, hash-pinned PDFs (see `sikh/sgpc-nanakshahi-inventory.csv`); no OCR
  shortcut, and the 558 calendar's legacy ASCII-mapped Gurmukhi text layer is not
  admissible without controlled conversion.
- **DoPT 2015–2025 backfill** — official PDFs located by the research pass but not
  retrieved; per-year memoranda still wanted.
