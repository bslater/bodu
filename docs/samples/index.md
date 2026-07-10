---
title: Samples
---

# Samples

The repository ships runnable, self-contained sample projects under
[`samples/`](https://github.com/bslater/bodu/tree/master/samples), organised by domain folder
named after the namespace segment they demonstrate (`Financial/`, `Globalization.Calendar/`).
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
| [Globalization.Calendar](calendar.md) | 5 projects + 1 test project | Holiday queries with ISO 3166-2 subdivision shadowing, working-day and fiscal arithmetic with `WeekPattern` overrides, fluent calendar authoring with catalogue imports and the XML round trip, DI with live data reload, and custom date algorithms proven by the shared data-pack test base |

## Testing companions

Two samples ship test projects that derive the repository's contract-test bases —
`DatedRateProviderContractTests<T>` (from the shipped `Bodu.Financial.ExchangeRates.Testing`
package) and `CalendarDataTestsBase` (repository-internal) — demonstrating how consumer-written
providers, calendars, and algorithms are validated against the same contracts the built-in
implementations pass. Both run in CI with the library suites.
