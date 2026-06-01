---
title: Working-day arithmetic
---

# Working-day arithmetic

`Bodu.Globalization.Calendar` ships parallel extension surfaces — `NotableDateOnlyExtensions` (over `DateOnly`) and `NotableDateTimeExtensions` (over `DateTime`) — under the `Bodu.Extensions` namespace. Each method hangs off an `INotableDateService` and produces working-day-aware results that respect the configured weekend definition and the non-working notable dates for the queried territory.

Every method has two overload shapes:

- **Explicit service** — the first overload takes an `INotableDateService` parameter. Use this whenever you build the service via dependency injection.
- **Ambient service** — the second overload omits the service argument and reads it from `NotableDateContext.Default`. Use this in scripts and console apps, or assign a richly configured service to `NotableDateContext.Default` once at start-up so the parameterless form works everywhere downstream.

The signatures below show the explicit-service overload for `DateOnly`. The `DateTime` overloads are identical in shape; both also accept an optional `calendarType` parameter for calendar-system disambiguation.

## Lookup

| Method | Returns |
|---|---|
| `IsWorkingDay(service, territoryCode?, calendarType?)` | `true` when the date is neither a weekend nor a non-working notable date for the territory. |
| `IsNonWorkingDay(service, territoryCode?, calendarType?)` | `true` when the date *is* a weekend or a non-working notable date. |
| `IsNotableDate(service, territoryCode?, calendarType?)` | `true` when any notable date — working or non-working — applies for the territory. |

```csharp
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

bool isHoliday  = today.IsNotableDate(service, "AU-NSW");
bool isWorking  = today.IsWorkingDay(service, "AU-NSW");
bool isWeekend  = today.IsNonWorkingDay(service, "AU-NSW") && !today.IsNotableDate(service, "AU-NSW");
```

## Navigation

`count` (default `1`) lets you skip several working days in one call.

| Method | Effect |
|---|---|
| `NextWorkingDay(service, count, territoryCode?, calendarType?)` | Advance to the *n*th following working day. |
| `PreviousWorkingDay(service, count, territoryCode?, calendarType?)` | Retreat to the *n*th preceding working day. |
| `NextNonWorkingDay(service, count, territoryCode?, calendarType?)` | Advance to the *n*th following non-working day. |
| `PreviousNonWorkingDay(service, count, territoryCode?, calendarType?)` | Retreat to the *n*th preceding non-working day. |
| `NextNotableDate(service, territoryCode?, calendarType?)` | Advance to the next date with any notable-date rule applicable to the territory. |
| `PreviousNotableDate(service, territoryCode?, calendarType?)` | Retreat to the previous notable date. |

```csharp
DateOnly nextOpen     = today.NextWorkingDay(service, "AU-NSW");
DateOnly twoOpenAhead = today.NextWorkingDay(service, count: 2, territoryCode: "AU-NSW");
DateOnly lastOpen     = today.PreviousWorkingDay(service, "AU-NSW");
DateOnly nextHoliday  = today.NextNotableDate(service, "AU-NSW");
```

## Arithmetic

| Method | Effect |
|---|---|
| `AddWorkingDays(service, days, territoryCode?, calendarType?)` | Add (or subtract, when `days` is negative) the signed number of working days, skipping non-working dates. |
| `WorkingDaysBetween(endDate, service, territoryCode?, calendarType?)` | Count the working days between `startDate` and `endDate` (exclusive on the start, inclusive on the end — see the method's XML docs for precise boundary semantics). |

```csharp
DateOnly inFive   = today.AddWorkingDays(service, 5, "AU-NSW");
DateOnly fiveAgo  = today.AddWorkingDays(service, -5, "AU-NSW");
int spanWorkdays  = today.WorkingDaysBetween(inFive, service, "AU-NSW");
```

## Snap

The `Snap*` operations are no-ops when the input is already a working day; otherwise they shift to the nearest working day in the requested direction.

| Method | Effect |
|---|---|
| `SnapToWorkingDay(service, territoryCode?, calendarType?)` | If the date is non-working, advance forward to the first working day. |
| `SnapToWorkingDayBackward(service, territoryCode?, calendarType?)` | If the date is non-working, retreat to the previous working day. |
| `SnapToNearestWorkingDay(service, territoryCode?, calendarType?)` | If the date is non-working, choose the closer of forward / backward snaps. |

```csharp
DateOnly saturday = new DateOnly(2026, 1, 3);     // Saturday
DateOnly snapped  = saturday.SnapToWorkingDay(service, "AU-NSW");   // Monday 5 Jan 2026
```

## Enumeration

Each enumeration returns an `IEnumerable<DateOnly>` (or `IEnumerable<DateTime>`) that lazily yields dates in the requested range. They are safe to combine with LINQ.

| Method | Yields |
|---|---|
| `EnumerateWorkingDays(endDate, service, territoryCode?, calendarType?)` | Every working day in the inclusive range. |
| `EnumerateNonWorkingDays(endDate, service, territoryCode?, calendarType?)` | Every non-working day in the inclusive range. |
| `EnumerateNotableDates(endDate, service, territoryCode?, calendarType?)` | Every notable date in the inclusive range. |
| `GetNotableDates(endDate, service, territoryCode?, calendarType?)` | Materialised list of notable dates in the range. |
| `GetNotableDatesInMonth(service, territoryCode?, calendarType?)` | Notable dates in the calendar month containing the input. |
| `GetNotableDatesInYear(service, territoryCode?, calendarType?)` | Notable dates in the calendar year containing the input. |

```csharp
DateOnly start = new DateOnly(2026, 1, 1);
DateOnly end   = new DateOnly(2026, 1, 31);

foreach (DateOnly workday in start.EnumerateWorkingDays(end, service, "AU-NSW"))
{
    // process each open business day in January 2026
}
```

## Ambient service with `NotableDateContext`

When a service is registered once at composition time, the parameterless overloads consult <xref:Bodu.Globalization.Calendar.NotableDateContext>`.Default`:

```csharp
// In Program.cs / Startup.cs:
NotableDateContext.Default = new NotableDateService(
    ruleProviders:     AsiaPacificCalendarData.CreateProviders(),
    weekendDefinition: CalendarWeekendDefinition.SaturdaySunday);

// Anywhere downstream — no service parameter needed:
DateOnly today    = DateOnly.FromDateTime(DateTime.Today);
DateOnly nextOpen = today.NextWorkingDay(territoryCode: "AU-NSW");
bool     isOpen   = today.IsWorkingDay("AU-NSW");
```

If `NotableDateContext.Default` is left unassigned, the property lazily constructs a `NotableDateService` backed by the embedded minimal rule set (currently New Year's Day only). For region-specific holidays, assign a configured service explicitly.

## Weekend behaviour

The service's `WorkingWeek` property (a <xref:Bodu.WeekPattern>) defines which days are working; any day outside the pattern is treated as a weekend. Use the named presets on <xref:Bodu.WeekPattern> for common shapes, or construct a custom pattern for non-standard schedules:

| Preset | Working days | Weekend days |
|---|---|---|
| `WeekPattern.AllDays` | Every day | No weekend — every day counts as working unless a non-working notable date applies. |
| `WeekPattern.MondayToFriday` | Mon–Fri | Saturday + Sunday (most western territories). |
| `WeekPattern.SundayToThursday` | Sun–Thu | Friday + Saturday (parts of the Middle East). |
| `WeekPattern.MondayToSaturday` | Mon–Sat | Sunday only. |
| `WeekPattern.SaturdayToThursday` | Sat–Thu | Friday only. |
| Custom | Any caller-supplied bitmask | Any complementary subset via <xref:Bodu.WeekPattern>. |

Set this at service construction time via the `workingWeek` constructor argument. Mixed-weekend services are best modelled by composing multiple services or by combining weekend definitions in custom rules.

## Public holidays vs. observances

Working-day arithmetic respects the rule's `IsNonWorkingDay` flag — not the category. A `NotableDateCategory.Holiday` rule with `IsNonWorkingDay = false` does **not** cause working-day arithmetic to skip the date. Conversely, a `Bank`, `Civic`, or `Cultural` rule authored with `IsNonWorkingDay = true` *does* skip.

Authors choose this when defining the rule. The data packs follow the convention that nationally legislated closures (public holidays, bank closures) have `IsNonWorkingDay = true`, while purely commemorative observances (Mother's Day, ANZAC Day in non-RSL contexts) have `IsNonWorkingDay = false`.

## Territory-specific calculation

Every method takes a `territoryCode` parameter. Without one, only rules authored without a territory scope contribute — usually a minimal set. Always pass a territory when querying for a real consumer scenario.

Because `TerritoryCode` is hierarchical, a call with `"AU-NSW"` honours both national `AU` rules and NSW-specific `AU-NSW` rules. See [Territories and regional composition](territories.md) for the containment semantics.

## Where to go next

- **[Using NotableDateService](notable-dates.md)** — building the service, filters, range queries, cache invalidation.
- **[Territories and regional composition](territories.md)** — how the `territoryCode` parameter composes national and regional rules.
- **[Observance adjustment rules](adjustment-rules.md)** — how a rule's nominal date becomes the observed non-working day that working-day arithmetic ultimately skips.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — `NotableDateOnlyExtensions`, `NotableDateTimeExtensions`, `NotableDateContext` field-by-field reference.
