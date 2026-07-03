# Bodu.Globalization.Calendar

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A resource-driven notable-date (holiday / observance) engine for .NET 8. Calendars are described as declarative, importable documents — concepts, rules, calculation strategies, adjustment policies, and resolution policies — that the engine resolves into concrete occurrences for a territory and date range. The package ships the calculation strategies, astronomical algorithms, range-resolution machinery, and working-day extensions; the regional holiday data ships in the companion `Bodu.Globalization.Calendar.<Region>` data packages.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar
```

Targets `net8.0`.

## Core model

| Type | Summary |
|---|---|
| `NotableDateService` / `INotableDateService` | Stateless resolver that computes occurrences from a loaded resource over a date range, applying filters and resolution policy |
| `NotableDateResource` / `NotableDateResourceLoader` | Immutable validated document and its loader (resolves imports, validates the schema, binds custom algorithms) |
| `NotableDateDefinition` | A notable-date concept: stable id, display name, inherited defaults, and child rules |
| `NotableDateRule` | One calculation method for a concept: applicability scope, strategy, and adjustment policies |
| `NotableDate` | A resolved occurrence: actual vs. observed date, category, priority, duration, working-day flags |
| `NotableDateFilter` | Composable immutable predicate over resolved occurrences (category, tag, name, duration, observed state) |
| `TerritoryCode` | Validated ISO 3166-1 country with optional ISO 3166-2 subdivision and parent/child containment |

Shared faith and civil catalogues (`global-core`, `christian-western`, `christian-orthodox`, `catholic`, `global-islamic`, `global-jewish`, `global-buddhist`, `global-hindu`, `global-persian`, …) are embedded in this core library and resolved through `CommonNotableDateResources`; the regional data packs import them via their region hubs.

## Calculation strategies and algorithms

The `Bodu.Globalization.Calendar.Algorithms` namespace provides the `IDateCalculationStrategy` implementations — `FixedDateStrategy`, `DayOfWeekInMonthStrategy`, `RelativeWeekdayInMonthStrategy`, `WeekdayNearDateStrategy`, `OffsetFromRuleStrategy`, and `AlgorithmDateStrategy` — together with the astronomical / calendrical `INotableDateAlgorithm` engines: `EasterCalculator`, `HinduLunarCalculator`, `LunarPhaseCalculator`, `SolarTermCalculator`, `TibetanLosarCalculator`, and `MatarikiCalendar`. Custom algorithms register through `NotableDateAlgorithmRegistry` and dispatch by key.

## Range resolution

The `Bodu.Globalization.Calendar.RangeResolution` namespace defines how overlapping and duplicate occurrences collapse: `ResolutionPolicy` carries the duplicate, collision, priority-direction, observed-range, and emission rules (`CollisionPolicy`, `DuplicatePolicy`, `PriorityDirection`, `ObservedDateRangePolicy`, `EmissionMode`), with `INotableDateCollisionResolver` as the custom extension point.

## Working-day extensions

The `Bodu.Extensions` namespace adds working-day and notable-date helpers over `DateOnly`, `DateTime`, and `DateTimeOffset`:

```csharp
using Bodu.Extensions;

bool isHoliday = date.IsNotableDate(service, territory);
DateOnly settle = date.AddWorkingDays(2, service, territory);
DateOnly next   = date.NextWorkingDay(service, territory);
```

Members include `IsWorkingDay`, `IsWeekend`, `AddWorkingDays`, `NextWorkingDay` / `PreviousWorkingDay`, `SnapToWorkingDay`, `WorkingDaysBetween`, the `IsNotableDate` / `GetNotableDates` / `EnumerateNotableDates` family, and fiscal-period helpers.

## Related packages

| Package | Purpose |
|---|---|
| `Bodu.Globalization.Calendar.<Region>` | Per-country holiday data bundles (Americas, AsiaPacific, Europe, MiddleEast, Africa) |
| `Bodu.Globalization.Calendar.Builder` | Fluent API for authoring notable-date documents |
| `Bodu.Globalization.Calendar.Plugins` | Trust-gated loading of external algorithm plugins |
| `Bodu.Globalization.Calendar.DependencyInjection` | `IServiceCollection` registration |

## Testing

Tests live in `test/` as MSTest classes. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Globalization.Calendar/test/Bodu.Globalization.Calendar.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Globalization.Calendar/test/Bodu.Globalization.Calendar.Test.csproj --settings regression.runsettings
```

The suite uses self-contained `*KnownAnswerTests` classes that pin Easter offsets, lunar/solar-term, and strategy-resolution results against published dates, with shared XML fixtures under `test/Globalization.Calendar/Fixtures/`.

## License

MIT. © Bodu Pty. Ltd.
