# calcal Modern Hindu Reference — pinned generation environment

Generates the reference corpus (`../../data/normalized/hindu-reference-daily.csv`) from the
R package [`calcal`](https://github.com/robjhyndman/calcal) (Apache-2.0), pinned to
**1.0.4**. This runs on a developer machine or a dedicated CI job — the repository's
default build never executes R, and the corpus is consumed as a committed artifact.

## One-time setup (produces `renv.lock`, which must be committed)

```r
install.packages("renv")
renv::init(bare = TRUE)
renv::install("calcal@1.0.4")
renv::snapshot()
```

Record the package archive SHA-256 alongside the lock (CRAN archive
`calcal_1.0.4.tar.gz`), or the exact Git commit if installed from GitHub — a version
number alone is not a sufficient pin.

## Recorded pin (first generation, 2026-08-03)

The committed corpus was generated in the Claude Code build container (CRAN is
unreachable there, so the environment was assembled without `renv::install`):

- **R 4.3.3** (`r-base-core` 4.3.3-2build2, Ubuntu apt) with **vctrs 0.6.5**
  (`r-cran-vctrs`, Ubuntu apt).
- **calcal 1.0.4** installed with `R CMD INSTALL` from the CRAN source archive
  `calcal_1.0.4.tar.gz`, SHA-256
  `e429f2dd436673021e1e37a74d97a089e9e47aa11ab2016dbfd4bcc1138cd3d6`
  (`Packaged: 2026-02-27 23:26:40 UTC`, `Repository: CRAN`), supplied user-side.
- `renv.lock` records the resulting versions; because the packages were installed
  outside renv, the lock was authored to match the verified environment rather than
  produced by `renv::snapshot()`.
- The adapter in `generate_corpus.R` was confirmed against this release on first run and
  rewritten from the specification's illustrative Lisp-style names to calcal 1.0.4's
  vectorized vctrs API (see the ADAPTER NOTE in the script).

## Generation

```bash
Rscript verify_environment.R
Rscript generate_corpus.R \
  --start-date 1990-01-01 \
  --end-date   2039-12-31 \
  --output     ../../data/normalized/hindu-reference-daily.csv \
  --model      all
```

The generator refuses to write a corpus whose embedded smoke checks fail. **On first run,
confirm the calcal function names and return shapes** (the adapter follows the corpus
specification's illustrative shape and is expected to need adjustment against the pinned
release — that is by design; fix the adapter, not the smoke checks).

After generation, commit `renv.lock`, the generated CSV, and note the generation log's
version/hash lines in the commit message. The reconciliation tests in
`Bodu.Globalization.Calendar/test` activate automatically once the CSV exists.

## Container recipe (optional)

Any `rocker/r-ver:4.x` image works; set the container timezone explicitly, restore from
`renv.lock`, then disable network access for the generation step. Old Hindu model output
is for algorithm-family regression only — never compare it against modern Panchang
expectations.
