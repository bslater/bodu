# Code coverage strategy

This guide describes how Bodu measures test coverage, how to reproduce the
numbers locally, and the handful of cases where a literal "99% on one machine"
target is misleading — most importantly the SIMD/scalar split in
`Bodu.Security.Cryptography`.

## Measuring coverage

**coverlet is the authoritative basis.** The test projects reference
`coverlet.collector` (supplied centrally by `Directory.Build.targets`, gated on
`IsTestProject`), and its `line-rate` already uses the line basis this document
defines — see [Reading the numbers](#reading-the-numbers) — so the tool's number
and the project's working definition are the same number. `Microsoft.NET.Test.Sdk`
also brings the Microsoft **Code Coverage** collector; it remains available as a
secondary diagnostic, but reported figures come from coverlet.

Coverage must be collected against the **full** suite — the `regression`
tier — because the default `bvt` run deliberately excludes the exhaustive
vector tables and large sweeps that exercise many branches. `coverage.runsettings`
pairs that tier filter with a fully configured collector, so the ordinary
`regression.runsettings` run stays uninstrumented and fast:

```bash
# Collect (per project, into artifacts/coverage/raw/)
bld/collect-coverage.sh                          # every test project in bodu.slnx
bld/collect-coverage.sh --project <Project>/test/<Project>.Test.csproj
bld/collect-coverage.sh --shard 3/8              # one balanced slice, for CI

# Merge and publish
bld/merge-coverage.sh                            # -> artifacts/coverage/report/
pwsh tools/New-CoverageMatrix.ps1                # -> docs/articles/coverage-baseline.md
```

Pass the settings file, and **do not** also pass a `--collect` switch: a
command-line collector is a second, unconfigured instance whose exclusion lists
are empty, which silently readmits generated sources to the denominator.

The current measured figures live in [Coverage baseline](coverage-baseline.md).

`tools/Filter-CoverageXml.ps1` extracts a single assembly's results from a
Microsoft Code Coverage XML export for focused review.

## Reading the numbers

- **Generated code is excluded.** Microsoft Code Coverage drops `*.Designer.cs`
  resource accessors; coverlet does not. Compare like for like by filtering out
  `*.Designer.cs` before computing a percentage.
- **Partially-covered lines.** When block data is collected, Microsoft Code
  Coverage reports a line that took only some of its branches as
  *partially covered* and excludes it from `line_coverage`. The project's
  working definition of line coverage counts a line as covered when it executed
  at all, i.e. `(lines_covered + lines_partially_covered) / total`. coverlet's
  `line-rate` already uses that basis.
- **Defensive throws and closing braces.** Guard clauses that are unreachable
  through the public API (for example a validated enum's `default` switch arm,
  or a `Utf8JsonReader` "unexpected end" guard that the reader itself prevents)
  and coverlet's closing-brace artifacts will never reach 100% and are left
  uncovered by design.

## The 90% floor

Every collected package sits at or above **90% line coverage**, and
`bld/coverage-thresholds.json` holds a per-package floor at roughly its measured
rate. The floor is a gate, not a target: a package well above it is not
"finished", and a package at it is not in trouble. What the invariant buys is
that a new gap has to be introduced deliberately — the ratchet fails the build
before an untested subsystem can arrive quietly inside an otherwise healthy
package total.

Two consequences worth stating, because both were live questions while the floor
was being established:

- **A package total can hide an entire dead subsystem.** `Bodu.IO.Pst` read 79.9%
  overall while `PstDataTree.cs` — the `XBLOCK`/`XXBLOCK` layout every node payload
  above roughly 8&#160;KB uses — was 30 of 37 lines uncovered, because the whole
  reference corpus is small files whose nodes each fit one block. The number to
  interrogate is the shape of the gap, not the percentage.
- **Reaching the floor with unrelated lines is the failure mode the floor exists
  to prevent.** The honest fix for that package was a synthetic-container builder
  (`PstFixtureBuilder`), which authors a structurally valid PST in memory so the
  multi-block trees, the cyclic content encoding, the subnode index block and every
  validation guard can be reached at all. Where a corpus cannot produce a shape,
  author the shape; do not bank easier lines from elsewhere in the same package.

## Hardware-gated SIMD paths (the AVX512 split)

`Bodu.Security.Cryptography` ships hardware-accelerated implementations
(`ThreefishBlockCipher.{256,512,1024}.Avx512.cs`, `Blake2b/2s/3.Avx512.cs`,
…) alongside scalar fallbacks. The JIT selects exactly one path per process
based on the CPU, so **no single machine can cover both**:

- On an **AVX512-capable** host the existing known-answer tests drive the
  `*.Avx512.cs` path to ~100%, and the scalar fallback drops correspondingly.
- On a host **without** AVX512 the scalar path is covered and every
  `*.Avx512.cs` file reports 0%.

These files are therefore **not a missing-test gap** — they are exercised by
the standard KAT suites; coverage simply depends on where the suite runs. The
report that prompted this work was collected on a non-AVX512 machine, which is
why those files appeared at 0%.

> **Measured correction — see [Coverage baseline](coverage-baseline.md).** That
> reasoning holds for whichever path the host *can* execute, but it has been used
> to wave away both. Collected on an **AVX512-capable** host with
> `Bodu.Security.Cryptography.Simd.Test` in the merge, the split is:
>
> | Path | Covered / total | Rate |
> |---|--:|--:|
> | `*.Avx512.cs` intrinsic | 676 / 680 | 99.4% |
> | scalar reference | 327 / 993 | 32.9% |
>
> The scalar fallbacks are a **real** gap, not a hardware artifact:
> `ThreefishBlockCipher.1024.cs` sits at 8.3%, `.256.cs` at 19.0%, and `.512.cs`
> at 12.8%. The two-run merge is what should close this, but
> `Bodu.Security.Cryptography.Simd.Test` runs **3 tests** against the main
> suite's 28,757 — the feature switch works, the suite behind it is a stub.
>
> The symmetry is what makes this easy to miss: on a non-AVX512 machine the same
> gap is invisible, because there the scalar paths are the ones the KAT suite
> drives and the intrinsics read as 0% "by design". Whichever host you measure
> on, one side is under-tested and the other side's 0% supplies a ready
> explanation for it.
>
> Closing it means running the existing known-answer suites under the switch, not
> authoring new vectors.

### The dual pass — now automated

The two-run merge this section prescribes is wired into the tooling:

```bash
bld/collect-coverage.sh --project Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj
bld/collect-coverage.sh --project Bodu.Security.Cryptography/test/Bodu.Security.Cryptography.Test.csproj --scalar
bld/merge-coverage.sh
```

`--scalar` re-runs the suite with `DOTNET_EnableAVX512F=0 DOTNET_EnableAVX2=0
DOTNET_EnableHWIntrinsic=0` into a parallel `<Name>.scalar` directory.
ReportGenerator takes the maximum hit count per line, so merging the two yields
the union rather than either half. `.github/workflows/coverage.yml` runs the
scalar pass automatically in whichever job collected the crypto suite.

Measured effect of adding the second pass — no test code was written:

| | Intrinsic paths | Scalar paths | Package |
|---|--:|--:|--:|
| Native run only | 99.4% | 32.9% | 93.4% |
| Both passes merged | 99.4% | **99.0%** | **98.4%** |

`Bodu.Security.Cryptography` is the only package with hardware-gated dispatch, so
it is the only one that needs this; disabling intrinsics elsewhere would cost
wall clock for nothing.

**This does not retire the `n/a (hardware-gated)` classification** in
`tools/New-CoverageMatrix.ps1`. A single-pass local run on a machine without
AVX-512 still cannot execute the intrinsic files, and reporting them as 0% there
would be wrong. The classification stays for that case; after a dual pass it
simply never triggers, because neither path is unreachable any more.

### The switch itself

The dual pass closes the numbers but does not exercise the shipped `DisableSimd`
feature, which is a product contract rather than a coverage figure.
`Bodu.Security.Cryptography.Simd.Test` covers that separately: it sets the switch
through `runtimeconfig.template.json` and compiles in the published known-answer
suites for every SIMD-gated primitive, so the scalar fallbacks are held to the
same vectors as the accelerated paths.

The suites are **linked**, not duplicated, and not reached by referencing the main
test assembly — nothing there has to be unsealed or made visible. `KatCensus` is
deliberately excluded: `KatCensusTests` rewrites the committed `kat-census.txt`,
and a second writer in a second assembly would corrupt it.

Run on its own, that assembly executes the scalar implementations at 87–95% and
every Threefish `*.Avx512.cs` file at **0%** — measured proof that the switch is
engaged and the intrinsic path is not running. The BLAKE intrinsic files show
four lines of their static constructor, which initialize the rotation constants
whenever the type is touched; no intrinsic compute code executes.

Adding it changed the package figure not at all, which is the point: it buys
confidence in the feature, and a second independent source of scalar coverage
should the environment-variable pass ever be removed.

## Stale paths across a folder or namespace refactor

Coverage is keyed by source-file path. When a report is collected — or several
runs are **merged** — across a commit that *moves or renames* source files, the
result silently double-counts: the old paths linger as **phantom entries at 0%**
that no longer exist on disk, sitting alongside the real entries for the renamed
files. The phantom rows drag every module aggregate down even though the live
code is well covered.

The flatten in **#528** (`Bodu.Financial.ExchangeRates.<Provider>` →
`Bodu.Financial.ExchangeRates`, moving `src/Financial.ExchangeRates.<Provider>/…`
to `src/Financial.ExchangeRates/…`) is the worked example. A report spanning that
commit listed the provider modules at 41–61% and every parser at 0%, when the
real per-file coverage was already healthy:

| File | Report (phantom path) | Actual (live path) |
|---|---|---|
| `EcbRateXmlParser` | 0% | 93.1% |
| `BoeRateCsvParser` | 0% | 93.9% |
| `RbaRateWorkbookParser` | 0% | 94.3% |
| `YahooChartResponseParser` | 0% | 87.7% |
| `OfxSpotRateHistoryResponseParser` | 0% | 94.6% |

**Remedy:** re-collect on a clean post-refactor checkout, **per project**, and
discard any row whose file path does not resolve on disk before computing a
percentage. A path that exists under two different folder spellings is the
tell-tale of a cross-refactor artifact, not a coverage gap.

The genuinely low spots this re-measurement surfaced were narrow — the
`OfxRateProvider` owned-client constructor path (now covered) and the
file-system feed/response/workbook caches' best-effort I/O swallow blocks. The
caches' `Store` `IOException` path is covered; their `UnauthorizedAccessException`
catches and `TryGet` read-fault catches are left uncovered by design per
[Reading the numbers](#reading-the-numbers) — the test process runs as root, so
permission denial cannot be forced, and a mid-read I/O fault is not reproducible
cross-platform.

## Source compiled into more than one assembly

`Bodu.Text.Serialization/shared/**` is not only shipped as its own assembly — it
is also `Compile Include`d directly into `Bodu.Text.Toml`, `Bodu.Text.Bencode`
and `Bodu.Text.Yaml`, each under its own format symbol. The `Link=` metadata
affects only IDE display, so the PDB document path — and therefore the Cobertura
`filename` — is the real on-disk path under `Bodu.Text.Serialization/shared/`.

Those lines consequently appear **once per host assembly**. ReportGenerator will
not collapse them, and it is right not to: they genuinely belong to three
different assemblies. Measured on a Toml + Bencode collection, the shared
`ConverterFactory.cs` appears twice, and summing the shared files across hosts
inflates them from 624 distinct lines to 2,360.

`tools/New-CoverageMatrix.ps1` therefore does two things no report filter can
express:

- reports the shared source once, as the synthetic unit
  **`Bodu.Text.Serialization (shared source)`**, whose coverage is the *union*
  across the hosts (a line counts as covered when any host covers it); and
- **subtracts** those files from the `Toml` / `Bencode` / `Yaml` rows, so the
  solution total does not count them once per host.

### There are three such sets, not one

The serializer core is the largest, but two more source sets are `Compile
Include`d across projects, and each gets its own synthetic unit. The table lives
at `$SharedSourceSets` in `tools/New-CoverageMatrix.ps1`; keep it in step with
the host csproj globs.

| Shared set | Synthetic unit | Compiled into |
|---|---|---|
| `Bodu.Text.Serialization/shared/**` | `Bodu.Text.Serialization (shared source)` | `Toml`, `Bencode`, `Yaml` |
| `Bodu.IO.Hashing/shared/**` (`CrcCore.cs`) | `Bodu.IO.Hashing (shared source)` | `IO.Hashing`, `IO.Pst`, `Formats.Outlook.Msg` |
| `shared/Caching/**` | `Caching (shared source)` | `Financial.ExchangeRates.Caching`, `Globalization.Calendar.Caching` |

Attribution matters even where the totals do not move. The file map is keyed by
repo-relative path, so a shared file is never *double-counted* — but before this
table existed, the two smaller sets were attributed wholesale to whichever host
the merge happened to emit first. `Bodu.Formats.Outlook.Msg` carried all 48 lines
of `CrcCore.cs` and `Bodu.IO.Pst` showed none of it; closing a line in that file
would have moved a row that does not own it. Splitting the sets out cost
`Outlook.Msg` 1.1pp and gained `ExchangeRates.Caching` 0.3pp, with the solution
figure unchanged — a correction, not a regression, so the two `Outlook.Msg`
ratchet floors were lowered by hand to match. That is the only circumstance in
which a floor comes down.

The three sets are not alike in one respect that the stale-numbering check cares
about. `Bodu.Text.Serialization/shared/**` selects **whole members** by format
symbol, so its hosts legitimately instrument different line sets and it is exempt
from that check. The other two select only a namespace declaration, which carries
no sequence points — so if their hosts ever disagree about which lines are
instrumentable, that really is stale data and should be reported.

The same keying — by source path and line number rather than by class — also
collapses the duplicate rows a file containing several classes would otherwise
contribute.

Read a per-package figure from the generated matrix, never from a single
project's Cobertura file: that file reports every assembly the test project
touched transitively, most of them at a near-zero rate that says nothing about
the package.

## Generated catalogues

Large generated catalogues are covered by a single reflective sweep rather than
per-item tests:

- **`Bodu.Financial.Currencies`** — `CurrencyCatalogueTests` enumerates every
  shipped `ICurrency` tag type, validates its static metadata against the
  `CurrencyRegistry`, and exercises the generated constructors. Regenerating the
  catalogue (`dotnet run --project tools/CurrencyCatalogueGenerator`) keeps the
  sweep green without new tests.

## Adding coverage tests

Follow the test conventions in `CLAUDE.md`. When a gap is a cross-cutting
behavioural contract rather than a single member, a dedicated
`<Type>CoverageTests.cs` (or `<Type>Tests.<Area>.cs`) partial is the
established home; prefer data-driven `[DataRow]`/`[DynamicData]` rows that cover
happy, edge, and exception cases together.
