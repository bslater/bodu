# Bodu Samples

Runnable, self-contained sample applications demonstrating the Bodu packages the way a
consumer would actually compose them. Every sample:

- **runs fully offline** — no network access, no accounts, no API keys. Exchange-rate
  samples read committed static data files instead of calling live feeds, and each one
  carries a clearly fenced comment block showing exactly how to switch to the real
  web-based provider. (One deliberate exception: `Bodu.Financial.Samples.LiveRates`
  exists precisely to call a live feed; it is clearly marked and excluded from the CI
  samples run.)
- **is deterministic** — running a sample twice prints the same output, so the samples
  double as executable documentation and as CI smoke tests.
- references the library projects directly via `ProjectReference`, so the samples always
  compile against the current source. Each sample's README lists the equivalent
  `dotnet add package` commands for NuGet consumers.

## Running a sample

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.OfflineRates
```

All samples are members of `bodu.slnx`, so `dotnet build bodu.slnx` builds them and any
API drift breaks the build immediately.

## Layout

Domain folders under `samples/` are named by namespace segment — `Core/`, `Collections/`,
`Collections.Concurrent/`, `Financial/`, `Formats.Excel/`, `Globalization.Calendar/`,
`Globalization.Recurrence/`, `IO.Compound/`, `IO.Hashing/`, `IO.Pst/`, `Numerics/`, `Security.Cryptography/`, `Text.Toml/`,
`Text.Yaml/`, `Text.Bencode/`, `Text.Formats/`, `Text.Configuration/`, `Text.Encoding/`, `Text.Filtering/` —
mirroring how
folders map to namespaces in the library source trees. The `samples/` root itself stays lowercase, like `src`/`test`/`bench`, because it is
not a namespace component. Each sample project is a flat folder named after the project.

## Conventions

Sample code is intentionally held to a *different* standard than the shipping libraries.
The samples keep the parts of the repository conventions that make them readable as
documentation, and drop the parts that only matter for shipped binaries:

**Kept:** the standard file header banner, file-scoped namespaces, one public type per
file, an XML `<summary>` on every type and method, and generous inline `//` commentary
explaining *why* each step exists.

**Dropped:** resx-backed exception messages (samples don't ship — plain string literals
are fine), the analyzer/style build gates (`RunAnalyzers=false`), and exhaustive
`<remarks>`/`<exception>` documentation.

Test projects that accompany a sample (for example
`Bodu.Financial.Samples.CustomProvider.Test`) follow the full repository test
conventions — they run in CI alongside the library test suites.

## README standard

Every sample project's README documents its scenarios individually, so a reader knows what
each one is trying to show *before* reading the code. For each `Scenarios/*.cs` file the
README carries a `###` section with four parts:

- **Intent** — the design question the scenario answers, and why it matters.
- **What it does** — a step-by-step account of what the code actually performs.
- **What to expect** — the console output the scenario prints, with the load-bearing lines
  explained (e.g. why a counter stays at 1, or why two totals agree).
- **APIs demonstrated** — the specific types and members the scenario exercises.

Because samples are deterministic, the "what to expect" output is the *actual* output — if a
change to the libraries alters it, the README review catches the drift alongside the CI run.

## Index

| Domain | Samples |
|---|---|
| Core | [`samples/Core/`](Core/README.md) — the functional railway (Option/Result/Either/Memoizer), the utility toolbox (sequences, pooled buffers, enumerable/string/numeric extensions, WeekPattern, async primitives), and text-encoding detection/transcoding |
| Collections | [`samples/Collections/`](Collections/README.md) — the specialized collection catalogue, ranges/graphs/trees/Aho-Corasick, and the probabilistic sketches (Bloom, count-min, HyperLogLog) |
| Collections.Concurrent | [`samples/Collections.Concurrent/`](Collections.Concurrent/README.md) — the thread-safe variants with single-flight GetOrAdd and a deterministic parallel-safety demo |
| Financial | [`samples/Financial/`](Financial/README.md) — money arithmetic, offline exchange rates, caching, aggregation, DI, custom providers, JSON serialization, the live-provider exception |
| Formats.Excel | [`samples/Formats.Excel/`](Formats.Excel/README.md) — the read-only BIFF8 `.xls` reader: sheet directory, forward-only streaming, materialized worksheets, serial-date decoding |
| Globalization.Calendar | [`samples/Globalization.Calendar/`](Globalization.Calendar/README.md) — holiday queries and subdivisions, working-day arithmetic, authored calendars, DI + reload, custom algorithms |
| Globalization.Recurrence | [`samples/Globalization.Recurrence/`](Globalization.Recurrence/README.md) — the RFC 5545 `RRULE` form, the Vixie cron dialect, calendar-free anchored intervals, `RDATE`/`EXDATE` set composition, and an integrating scheduling host |
| IO.Compound | [`samples/IO.Compound/`](IO.Compound/README.md) — OLE2 structured storage: builder authoring + read-back, OLE property sets, detection and the v3/v4 knob, a real `.doc`'s tree |
| IO.Pst | [`samples/IO.Pst/`](IO.Pst/README.md) — the PST node database: detection, raw property/table contexts, streaming under strict validation, and the `OutlookMailStore` folder/message/attachment walk |
| IO.Hashing | [`samples/IO.Hashing/`](IO.Hashing/README.md) — the CRC catalogue, checksum families, streaming/resumable digests, identifier check digits, and a custom scheme with contract tests |
| Numerics | [`samples/Numerics/`](Numerics/README.md) — Fraction rational arithmetic and continued fractions, the interval algebra, streaming statistics, and the JSON converters |
| Security.Cryptography | [`samples/Security.Cryptography/`](Security.Cryptography/README.md) — hashes/MAC/XOF/KDF/OTP, block/stream ciphers and AEAD, asymmetric key agreement/signatures/KEM, and a custom hash with contract tests |
| Text.Bencode | [`samples/Text.Bencode/`](Text.Bencode/README.md) — a real torrent file end to end: DOM inspection, canonical byte-exact round trips, the raw-slice info-hash, typed POCO mapping |
| Text.Configuration | [`samples/Text.Configuration/`](Text.Configuration/README.md) — the parse/resolve/save cascade with diagnostics and `unset` dialects, plus the Microsoft.Extensions.Configuration bridge into `IOptions<T>` |
| Text.Encoding | [`samples/Text.Encoding/`](Text.Encoding/README.md) — the base-encoding catalogue and variants, formatting/style knobs, checksummed schemes, the runtime registry, and a custom Base36 codec with contract tests |
| Text.Filtering | [`samples/Text.Filtering/`](Text.Filtering/README.md) — the include/exclude filtering engine: AnyMatch sets and gitignore-style ordered rules, the glob grammar with cost-tier classification, deciding-pattern diagnostics, and the statistics/observer telemetry |
| Text.Formats | [`samples/Text.Formats/`](Text.Formats/README.md) — CSV/TSV with typed getters and dirty-input policies, streaming pipelines, comment-preserving INI edits, DotEnv's literal contract |
| Text.Toml | [`samples/Text.Toml/`](Text.Toml/README.md) — the TomlSerializer POCO surface with native temporal kinds, plus both DOMs, the token layer, and resumable streaming reads |
| Text.Yaml | [`samples/Text.Yaml/`](Text.Yaml/README.md) — the YamlSerializer POCO surface with implicit scalar typing, plus the mutable and read-only DOMs, the token layer, and the stream/async facade |
