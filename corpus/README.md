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
  reconciliation tests (`HinduReferenceCorpusReconciliationTests` — Inconclusive until a
  generated corpus is committed). No R required.
- **User-side (or a dedicated CI job)**: reference-corpus generation with the pinned R
  environment under `hindu/reference/calcal/` — this container has no R runtime and no
  CRAN access. The generated `hindu-reference-daily.csv` and the `renv.lock` produced by
  the first pinned run are committed back here.
- **Acquisition**: official PDFs are *not* committed until redistribution rights are
  confirmed — commit their URL, SHA-256, and acquisition metadata instead (see
  `hindu/data/raw/imd-rashtriya-panchang/`).

## Layout

```text
corpus/
├── README.md                    this file
├── hindu/
│   ├── data/
│   │   ├── raw/imd-rashtriya-panchang/   edition manifest + acquisition records (no PDFs)
│   │   ├── normalized/                    generated/transcribed CSVs land here
│   │   └── schemas/                       JSON Schemas for the daily/event/provenance shapes
│   └── reference/calcal/                  pinned R generator + environment verification
├── islamic/                     independent reconciliation tables (ICU-derived, KFUPM/van Gent evidence notes)
└── persian/                     independent reconciliation tables (ICU-derived)
```
