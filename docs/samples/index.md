---
title: Samples
---

# Samples

The repository ships runnable, self-contained sample projects under
[`samples/`](https://github.com/bslater/bodu/tree/master/samples), organised by domain folder
named after the namespace segment they demonstrate (`Financial/`, `Formats.Excel/`,
`Globalization.Calendar/`, `IO.Compound/`, `IO.Hashing/`, `Text.Toml/`, `Text.Bencode/`,
`Text.Formats/`, `Text.Configuration/`, `Text.Encoding/`).
This section catalogues them; each domain page walks its samples individually.

Every sample:

- **runs offline by default** — no network, no accounts, no API keys. Exchange-rate samples
  read committed static data files; calendar samples use the embedded data packs. Where a live
  feed *could* be used, a fenced comment block shows the exact switch, and the one deliberately
  online sample (`Bodu.Financial.Samples.LiveRates`) is clearly marked and excluded from CI
  execution.
- **is deterministic** — running a sample twice prints the same output, so samples double as
  executable documentation and as CI smoke tests. Every sample is a member of `bodu.slnx`
  (API drift breaks the build) and is executed by the CI samples step.
- **is documented to a fixed README standard** — one section per scenario stating the intent,
  what the code does, the output to expect (with the load-bearing lines explained), and the
  APIs demonstrated.
- references the library projects via `ProjectReference`, with the equivalent
  `dotnet add package` commands listed per sample.

Run any sample from the repository root:

```bash
dotnet run --project samples/<Domain>/<SampleName>
```

## Domains

| Domain | Samples | Highlights |
|---|---|---|
| [Financial](financial.md) | 7 projects + 1 test project | Money arithmetic and the three-tier rounding model, the offline static-rate-file pattern, read-through caching and tiered stacking, multi-provider aggregation and routing, DI hosting, a consumer-written provider proven by the shipped contract-test base, and the live-provider exception |
| [Formats.Excel](excel.md) | 1 project | The read-only BIFF8 `.xls` reader over a real ~18,000-cell workbook: the session/sheet-directory surface, constant-memory forward-only streaming, the materialized worksheet, and format-classified serial-date decoding |
| [Globalization.Calendar](calendar.md) | 5 projects + 1 test project | Holiday queries with ISO 3166-2 subdivision shadowing, working-day and fiscal arithmetic with `WeekPattern` overrides, fluent calendar authoring with catalogue imports and the XML round trip, DI with live data reload, and custom date algorithms proven by the shared data-pack test base |
| [IO.Compound](io-compound.md) | 1 project | The OLE2 structured-storage container: builder-based authoring and byte-exact read-back, typed OLE property sets on authored and real Word files, signature detection with the v3/v4 sector knob, and walking a real `.doc`'s storage tree |
| [IO.Hashing](io-hashing.md) | 3 projects + 1 test project | The 112-standard parametric CRC catalogue, checksum families with corruption detection, streaming/resumable digests, non-cryptographic bucket routing, identifier check digits across domains with error-class comparisons, and a custom scheme proven by the shared contract-test base |
| [Text.Bencode](bencode.md) | 1 project | A real BitTorrent metainfo file end to end: DOM inspection, canonical byte-exact round trips, the info-hash from the raw `info` slice, and typed POCO mapping with keys containing spaces |
| [Text.Configuration](text-configuration.md) | 2 projects | The parse/resolve/save pipeline with diagnostics and the EditorConfig-style path cascade, `unset` dialect handling, and the `Microsoft.Extensions.Configuration` bridge into `IOptions<T>` |
| [Text.Encoding](text-encoding.md) | 2 projects + 1 test project | The base-encoding catalogue and variants, formatting/parse-style knobs, checksummed Base58Check/Bech32 corruption detection, the runtime registry, and a custom Base36 codec proven by the library's contract-test base |
| [Text.Formats](formats.md) | 2 projects | RFC 4180 CSV/TSV with typed getters and dirty-input policies, streaming reader/writer pipelines, INI comment-preserving edit loops, and DotEnv's literal no-interpolation contract |
| [Text.Toml](toml.md) | 2 projects | The `TomlSerializer` POCO surface with TOML's four native temporal kinds and naming/attribute layering, plus the mutable and read-only DOMs, the UTF-8 token layer, and resumable streaming reads |

## Testing companions

Four samples ship test projects that derive the repository's contract-test bases —
`DatedRateProviderContractTests<T>` (from the shipped `Bodu.Financial.ExchangeRates.Testing`
package), `CalendarDataTestsBase` (repository-internal),
`BinaryEncodingContractTests<TEncoding>` (from the `Bodu.Text.Encoding` test suite), and
`CheckDigitContractTests<TAlgorithm>` (from the `Bodu.IO.Hashing` test suite) —
demonstrating how consumer-written providers, calendars, algorithms, encodings, and
check-digit schemes are validated against the same contracts the built-in implementations
pass. All run in CI with the library suites.
