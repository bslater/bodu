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

```text
  AU 2024: 22 notable dates. First five:
    2024-01-01 New Year's Day         (PublicHoliday, non-working: True)
    2024-01-26 Australia Day          (PublicHoliday, non-working: True)
    2024-02-14 Valentine's Day        (Cultural, non-working: False)
    2024-03-21 Harmony Day            (Observance, non-working: False)
    2024-03-29 Good Friday            (PublicHoliday, non-working: True)
  (every row is notable, but only the PublicHoliday ones stop work - the category and the flag are stated rather than left to be guessed)
  2024-04-25: Anzac Day
  Easter window: Good Friday (03-29), Easter Saturday (03-30), Easter Sunday (03-31), April Fool's Day (04-01), Easter Monday (04-01)  (computed by the shared catalogue computus algorithm, not looked up - which is why the pack answers for years nobody tabulated)
  Supported territories in the AU resource: 9
  AsiaPacific pack countries: AU, CN, HK, ID, IN, JP, KR, MY, NZ, PH, SG, TH, TW, VN  (all embedded in the assembly - no network, no configuration, and no staleness)
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

```text
  2026 notable dates - AU: 22, AU-VIC: 26, AU-NSW: 26  (the state views are supersets: national rules plus state-only concepts, from the one country resource)
  AU-VIC Labour Day 2026 : 2026-03-09 (Monday)
  AU-NSW Labour Day 2026 : 2026-10-05 (Monday)  (seven months from Victoria's, same concept - which is why "Labour Day in Australia" has no answer)
  Concepts only in AU-VIC: Labour Day, King's Birthday, AFL Grand Final Friday, Melbourne Cup Day  (absent from the national view entirely - adding a state costs a rule, not a resource)
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

```text
  AU 2024 - all: 22, public holidays: 7, non-working public holidays: 7  (the last two coincide here - every AU public holiday in 2024 is also a day off - but they are different filters, and elsewhere the counts diverge)
  Working-day observances: Valentine's Day, Harmony Day, Easter Sunday, April Fool's Day, ...  (the complement of the previous filter via Not(), rather than a second filter to keep in sync with it)
  Christmas Day 2024: 2024-12-25 (Wednesday)  (one row here, but a name filter can return several - a concept may be emitted by more than one rule)
```

The counts narrow from every notable date to the public holidays. The last two coincide here —
every Australian public holiday in 2024 is also a day off — but they remain different filters,
and in a territory with a working public holiday they diverge. Easter Sunday appearing among
the *working-day* rows is the give-away detail — in Australia the Sunday itself is not a public holiday in most
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

```text
    2021-12-24 (Friday   ) Christmas Eve      actual
    2021-12-27 (Monday   ) Christmas Day      observed (actual 2021-12-25, reason: Substitute public holiday)
    2021-12-28 (Tuesday  ) Boxing Day         observed (actual 2021-12-26, reason: Substitute public holiday)
  (consecutive weekdays, not both on the Monday - Boxing Day's natural in-lieu slot was already taken, so substitution has to be conflict-aware)
  2024 contrast: 2024-12-25 (Wednesday) observed flag: False  (expected False - the same rules stay quiet when the holiday already falls on a weekday)
```

Boxing Day lands on *Tuesday* the 28th, not Monday — conflict-aware substitution, because
Christmas already claimed the Monday. `2021-12-27` for Christmas Day is the published
known-good date.

**APIs demonstrated.** `NotableDate.IsObserved` / `ActualDate` / `AdjustmentReason`, the
adjustment/emission behaviour of the pack's rules over a `DateRange` resolve.

### StreamingQueries (`Scenarios/StreamingQueries.cs`)

**Intent.** Resolving a range runs every rule for every year in it, so a decade costs ten times
one year. Show `ResolveAsync` yielding a civil year at a time instead of materializing the whole
result — and cancellation actually stopping the remaining computation.

**What it does.** Streams a decade of non-working occurrences, counting them per year, then
streams the same range again and cancels once the first year is past.

**What to expect.**

```text
  Streamed 80 non-working occurrences across 10 years  (element-for-element what the synchronous Resolve returns - streaming changes when, not what)
  first year 2020: 8, last year 2029: 8
  Cancelled after streaming 44 occurrences - later years never resolved.  (cancellation is observed between years, so an abandoned query stops rather than finishing in the background)
```

A civil year is the natural granularity because that is the unit the rules are evaluated in —
there is no partial year to yield — and cancellation is observed at the same boundary. The
streamed results are element-for-element what the synchronous `Resolve` returns over the same
range: streaming changes *when* a consumer gets them, not *what* they are.

**APIs demonstrated.** `INotableDateService.ResolveAsync(DateRange, territory, filter,
cancellationToken)`, `IAsyncEnumerable<NotableDate>` enumeration, cooperative cancellation.

### ExpandedTimeline (`Scenarios/ExpandedTimeline.cs`)

**Intent.** The AU pack emits each substituted holiday once, on its observed day — the nominal
day survives only as `ActualDate`. Show `WithActualOccurrences()` reconstructing the full
sequential story: every affected day as its own occurrence, actual and observed alike.

**What it does.** Resolves the same 2021 Christmas window, prints the raw observed-only result
(two occurrences, dated 27 and 28 December), then expands it — 25 and 26 December reappear as
actual occurrences — and demonstrates that expanding again is a no-op.

**What to expect.**

```text
  Raw observed-only result (each holiday emitted once, on the day off - the weekend days survive only as ActualDate):
    2021-12-27 (Monday   ) Christmas Day      actual 2021-12-25
    2021-12-28 (Tuesday  ) Boxing Day         actual 2021-12-26
  Expanded timeline (the nominal days synthesized back in and re-sorted - what a calendar rendering December needs):
    2021-12-25 (Saturday ) Christmas Day      actual
    2021-12-26 (Sunday   ) Boxing Day         actual
    2021-12-27 (Monday   ) Christmas Day      observed (in lieu of 2021-12-25)
    2021-12-28 (Tuesday  ) Boxing Day         observed (in lieu of 2021-12-26)
  Expanding again adds nothing: 4 occurrences either way.  (idempotent, so it is safe to apply without tracking whether it already ran)
```

The synthesized occurrences match the shape the engine emits when a policy declares
`ActualAndObserved`: `IsObserved` is `false`, no adjustment policy or reason, and every other
field — including `DisplayName` — carries over. The expansion skips occurrences whose actual day
is already present, which is what makes the second call a no-op.

**APIs demonstrated.** `NotableDateSequenceExtensions.WithActualOccurrences`, the
`ObservedOnly` vs `ActualAndObserved` emission distinction, `NotableDate.IsObserved` /
`ActualDate`.

## Layout

```text
Bodu.Globalization.Calendar.Samples.NotableDatesBasics/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect scenario banner
  Scenarios/QueryingHolidays.cs
  Scenarios/SubdivisionShadowing.cs
  Scenarios/FilteringAndCategories.cs
  Scenarios/ObservedDates.cs
  Scenarios/StreamingQueries.cs
  Scenarios/ExpandedTimeline.cs
```

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar.AsiaPacific   # brings the engine transitively
```
