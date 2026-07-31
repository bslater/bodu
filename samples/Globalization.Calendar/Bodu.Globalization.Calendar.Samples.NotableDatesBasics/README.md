# Bodu.Globalization.Calendar.Samples.NotableDatesBasics

Querying public holidays and observances from an embedded regional data pack — the calendar
family's front door. Everything is offline: the rule data ships as embedded XML resources inside
the data-pack assembly, so output is deterministic and no network or configuration is needed.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.NotableDatesBasics
```

## Scenarios

### QueryingHolidays (`Scenarios/QueryingHolidays.cs`)

**Intent.** Show the primary query surface end to end: one factory call yields a ready,
immutable, thread-safe `NotableDateService`, and `Resolve` answers year, single-day, and range
questions.

**What it does.** Creates the AU service via `AsiaPacificCalendarData.CreateService("AU")`,
resolves the whole of 2024, a single day (Anzac Day), and the Easter window; prints the
supported-territory count and the pack's country list.

**What to expect.**

```
AU 2024: 22 notable dates. First five:
  2024-01-01 New Year's Day         (PublicHoliday, non-working: True)
  2024-01-26 Australia Day          (PublicHoliday, non-working: True)
  2024-02-14 Valentine's Day        (Cultural, non-working: False)
  ...
2024-04-25: Anzac Day
Easter window: Good Friday (03-29), Easter Saturday (03-30), Easter Sunday (03-31), ...
AsiaPacific pack countries: AU, CN, HK, ID, IN, JP, KR, MY, NZ, PH, SG, TH, TW, VN
```

The 22 dates mix categories: public holidays that stop work and cultural observances that do
not — each occurrence carries `Category` and `IsNonWorkingDay` so consumers never guess. The
Easter dates came from the shared catalogue's computus algorithm via the pack's imports.

**APIs demonstrated.** `AsiaPacificCalendarData.CreateService` / `SupportedCountries`,
`INotableDateService.Resolve(int year | DateOnly | DateRange, territory)`,
`GetSupportedTerritories`, `NotableDate` (`Date`, `DisplayName`, `Category`, `IsNonWorkingDay`).

### SubdivisionShadowing (`Scenarios/SubdivisionShadowing.cs`)

**Intent.** State and provincial holidays differ. Show how one country resource serves every
ISO 3166-2 subdivision: a subdivision territory sees the national rules *plus* its own, and
where both define the same concept the most specific rule wins.

**What it does.** Creates AU, AU-VIC, and AU-NSW services, compares the 2026 counts, pins each
state's Labour Day (the classic shadowing example — one concept, different rule per state), and
lists the concepts that exist only in the Victorian view.

**What to expect.**

```
2026 notable dates - AU: 22, AU-VIC: 26, AU-NSW: 26
AU-VIC Labour Day 2026 : 2026-03-09 (Monday)
AU-NSW Labour Day 2026 : 2026-10-05 (Monday)
Concepts only in AU-VIC: Labour Day, King's Birthday, AFL Grand Final Friday, Melbourne Cup Day
```

Victoria observes the second Monday of March (2026-03-09 is the published date), New South
Wales the first Monday of October — same concept id, different declarative rule, selected by
the territory string alone.

**APIs demonstrated.** Subdivision territories (`"AU-VIC"`, `"AU-NSW"`) through
`CreateService`/`Resolve`, territory shadowing semantics.

### FilteringAndCategories (`Scenarios/FilteringAndCategories.cs`)

**Intent.** "Which dates count?" belongs in the query, not in post-filtering. `NotableDateFilter`
is an immutable, composable predicate — build the policy once ("public holidays that actually
stop work"), reuse it everywhere.

**What it does.** Resolves 2024 unfiltered, by category, with a composed
`ForCategory(PublicHoliday).And(IsNonWorkingDay())`, negated (`IsNonWorkingDay().Not()` — the
observances), and by name (`WithName("Christmas Day")`).

**What to expect.**

```
AU 2024 - all: 22, public holidays: 7, non-working public holidays: 7
Working-day observances: Valentine's Day, Harmony Day, Easter Sunday, April Fool's Day, ...
Christmas Day 2024: 2024-12-25 (Wednesday)
```

The narrowing counts show each filter's effect; Easter Sunday appearing among the *working-day*
rows is the give-away detail — in Australia the Sunday itself is not a public holiday in most
states, which is exactly the kind of nuance the rule data encodes and a hard-coded list gets
wrong.

**APIs demonstrated.** `NotableDateFilter.ForCategory` / `IsNonWorkingDay` / `WithName`, the
`And` / `Not` combinators, the filtered `Resolve` overloads.

### ObservedDates (`Scenarios/ObservedDates.cs`)

**Intent.** When a holiday falls on a weekend, many jurisdictions observe it on a weekday
in-lieu. Show that the adjustment machinery does this declaratively — and that the resolved
occurrence keeps its full lineage: emitted date, actual date, observed flag, and reason.

**What it does.** Resolves the 2021 Christmas window for AU — the classic double-substitution
year (Christmas Saturday, Boxing Day Sunday) — and contrasts 2024, where Christmas falls
mid-week and nothing fires.

**What to expect.**

```
  2021-12-24 (Friday   ) Christmas Eve      actual
  2021-12-27 (Monday   ) Christmas Day      observed (actual 2021-12-25, reason: Substitute public holiday)
  2021-12-28 (Tuesday  ) Boxing Day         observed (actual 2021-12-26, reason: Substitute public holiday)
  2024 contrast: 2024-12-25 (Wednesday) observed flag: False
```

Boxing Day lands on *Tuesday* the 28th, not Monday — conflict-aware substitution, because
Christmas already claimed the Monday. `2021-12-27` for Christmas Day is the published
known-good date.

**APIs demonstrated.** `NotableDate.IsObserved` / `ActualDate` / `AdjustmentReason`, the
adjustment/emission behaviour of the pack's rules over a `DateRange` resolve.

### ExpandedTimeline (`Scenarios/ExpandedTimeline.cs`)

**Intent.** The AU pack emits each substituted holiday once, on its observed day — the nominal
day survives only as `ActualDate`. Show `WithActualOccurrences()` reconstructing the full
sequential story: every affected day as its own occurrence, actual and observed alike.

**What it does.** Resolves the same 2021 Christmas window, prints the raw observed-only result
(two occurrences, dated 27 and 28 December), then expands it — 25 and 26 December reappear as
actual occurrences — and demonstrates that expanding again is a no-op.

**What to expect.**

```
  Raw observed-only result:
    2021-12-27 (Monday   ) Christmas Day      actual 2021-12-25
    2021-12-28 (Tuesday  ) Boxing Day         actual 2021-12-26
  Expanded timeline:
    2021-12-25 (Saturday ) Christmas Day      actual
    2021-12-26 (Sunday   ) Boxing Day         actual
    2021-12-27 (Monday   ) Christmas Day      observed (in lieu of 2021-12-25)
    2021-12-28 (Tuesday  ) Boxing Day         observed (in lieu of 2021-12-26)
  Expanding again adds nothing: 4 occurrences either way.
```

The synthesized occurrences match the shape the engine emits when a policy declares
`ActualAndObserved`: `IsObserved` is `false`, no adjustment policy or reason, and every other
field — including `DisplayName` — carries over. The expansion skips occurrences whose actual day
is already present, which is what makes the second call a no-op.

**APIs demonstrated.** `NotableDateSequenceExtensions.WithActualOccurrences`, the
`ObservedOnly` vs `ActualAndObserved` emission distinction, `NotableDate.IsObserved` /
`ActualDate`.

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar.AsiaPacific   # brings the engine transitively
```
