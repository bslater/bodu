# Code coverage strategy

This guide describes how Bodu measures test coverage, how to reproduce the
numbers locally, and the handful of cases where a literal "99% on one machine"
target is misleading — most importantly the SIMD/scalar split in
`Bodu.Security.Cryptography`.

## Measuring coverage

The test projects reference `coverlet.collector`, and `Microsoft.NET.Test.Sdk`
brings the Microsoft **Code Coverage** collector. Either works; they differ in
how they attribute braces and partially-covered lines (see
[Reading the numbers](#reading-the-numbers)).

Coverage must be collected against the **full** suite — the `regression`
tier — because the default `bvt` run deliberately excludes the exhaustive
vector tables and large sweeps that exercise many branches:

```bash
# coverlet (Cobertura XML) — fast inner loop, per project
dotnet test <Project>/test/<Project>.Test.csproj \
  --settings regression.runsettings \
  --collect:"XPlat Code Coverage"

# Microsoft Code Coverage (.coverage) — matches the CI/report basis
dotnet test bodu.slnx --settings regression.runsettings \
  --collect:"Code Coverage"
dotnet-coverage merge *.coverage -o coverage.xml -f xml
```

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

To account for both paths, collect coverage twice and merge:

```bash
# 1) Native run — covers whichever intrinsic path the CPU supports
dotnet test Bodu.Security.Cryptography/test/*.csproj \
  --settings regression.runsettings --collect:"Code Coverage"

# 2) Scalar run — disable the intrinsics so the fallback path executes
DOTNET_EnableAVX512F=0 DOTNET_EnableAVX2=0 DOTNET_EnableHWIntrinsic=0 \
  dotnet test Bodu.Security.Cryptography/test/*.csproj \
  --settings regression.runsettings --collect:"Code Coverage"

dotnet-coverage merge run1.coverage run2.coverage -o merged.xml -f xml
```

The merged result reflects both the SIMD and scalar implementations. CI that
needs a single authoritative number should run the scalar pass on its build
agents (which are typically not AVX512) and an intrinsic pass on capable
hardware, then merge.

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
| `EcbExchangeRateXmlParser` | 0% | 93.1% |
| `BoeExchangeRateCsvParser` | 0% | 93.9% |
| `RbaExchangeRateWorkbookParser` | 0% | 94.3% |
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
