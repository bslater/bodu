# Globalization.Calendar Samples

Console applications demonstrating the `Bodu.Globalization.Calendar` package family. Each sample
is a standalone project; run one with:

```bash
dotnet run --project samples/Globalization.Calendar/<SampleName>
```

Unlike the exchange-rate samples, nothing here even *could* go online: all rule data ships as
embedded XML resources in the engine and data-pack assemblies, so every sample is offline and
deterministic by construction. The "bring your own data" story is the `NotableDateDocumentBuilder`
(see CustomCalendar) rather than a live feed.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Globalization.Calendar.Samples.NotableDatesBasics` | Data-pack factories over embedded resources, year/day/range queries, ISO 3166-2 subdivision shadowing (AU vs AU-VIC/AU-NSW), composable `NotableDateFilter`s, observed-date substitution with full lineage | `Bodu.Globalization.Calendar`, `...Calendar.AsiaPacific` |
| `Bodu.Globalization.Calendar.Samples.WorkingDays` | Working-day predicates and arithmetic (`AddWorkingDays` T+2, snaps, counting/enumeration), fiscal-period boundaries, `WeekPattern` overrides (Sun–Thu weeks) | `Bodu.Globalization.Calendar`, `...Calendar.AsiaPacific` |
| `Bodu.Globalization.Calendar.Samples.CustomCalendar` | Fluent authoring (`NotableDateDocumentBuilder`), declarative adjustment policies, importing the shared catalogues (`CommonNotableDateResources`), the XML save/load round trip | `Bodu.Globalization.Calendar`, `...Calendar.Builder` |
| `Bodu.Globalization.Calendar.Samples.ServiceHosting` | `AddNotableDateService` singleton registration and `AddReloadableNotableDateService` + `MutableNotableDateResourceProvider.Reload` live data swap | `Bodu.Globalization.Calendar`, `...Calendar.DependencyInjection`, `...Calendar.AsiaPacific` |
| `Bodu.Globalization.Calendar.Samples.CustomAlgorithm` (+ `.Test`) | Custom `INotableDateAlgorithm` via `NotableDateAlgorithmRegistry`, declarative `Algorithm(key)` rules, lambda adapters, the commented trust-gated Plugins route; the test project derives `CalendarDataTestsBase` | `Bodu.Globalization.Calendar`, `...Calendar.Builder` (+ `...Calendar.Plugins` commented) |

## Known-good dates

Scenario output pins published dates where determinism allows exact assertions: AU-VIC Labour
Day 2026 → 2026-03-09, AU Christmas Day 2021 → observed 2021-12-27. Lunar and astronomical
festivals carry a ±1–2-day tolerance in the rule data's own test suites and are deliberately not
used for exact output here.
