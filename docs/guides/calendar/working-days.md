---
title: Working-day arithmetic
---

# Working-day arithmetic

`Bodu.Globalization.Calendar` ships parallel extension surfaces — `NotableDateOnlyExtensions` (over `DateOnly`), `NotableDateTimeExtensions` (over `DateTime`), and `NotableDateTimeOffsetExtensions` (over `DateTimeOffset`) — under the `Bodu.Extensions` namespace. Each method hangs off an <xref:Bodu.Globalization.Calendar.INotableDateService> and produces working-day-aware results that respect the working-week definition and the non-working notable dates for the queried territory.

These extensions are **not** auto-imported. Add the using directive explicitly:

```csharp
using Bodu.Extensions;
```

Every method takes an `INotableDateService service` and a territory `string`. Working-day operations accept an optional trailing `WeekPattern? workingWeek = null`; notable-date operations accept an optional trailing `NotableDateFilter? filter = null`. The service is always passed explicitly — there is no ambient context in v2.

`DateOnly` is the authoritative surface and carries the full method set. The `DateTime` and `DateTimeOffset` surfaces are a subset (see [Surface differences](#surface-differences)).

The signatures below show the `DateOnly` overloads.

## Lookup

| Method | Returns |
|---|---|
| `IsWeekend(WeekPattern? workingWeek = null)` | `true` when the date falls outside the working week (no service needed). |
| `IsWorkingDay(service, territory, WeekPattern? workingWeek = null)` | `true` when the date is neither a weekend nor a non-working notable date for the territory. |
| `IsNonWorkingDay(service, territory, WeekPattern? workingWeek = null)` | `true` when the date *is* a weekend or a non-working notable date. |
| `IsNotableDate(service, territory, NotableDateFilter? filter = null)` | `true` when any notable date (matching the optional filter) applies for the territory. |

```csharp
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

bool isWeekend  = today.IsWeekend();                       // weekend per the default Mon–Fri week
bool isHoliday  = today.IsNotableDate(service, "AU-NSW");
bool isWorking  = today.IsWorkingDay(service, "AU-NSW");
bool isClosed   = today.IsNonWorkingDay(service, "AU-NSW");
```

## Navigation

| Method | Effect |
|---|---|
| `NextWorkingDay(service, territory, WeekPattern? workingWeek = null)` | Advance to the next working day. |
| `PreviousWorkingDay(service, territory, WeekPattern? workingWeek = null)` | Retreat to the previous working day. |
| `NextNonWorkingDay(service, territory, WeekPattern? workingWeek = null)` | Advance to the next non-working day. Throws `InvalidOperationException` if none is found. |
| `PreviousNonWorkingDay(service, territory, WeekPattern? workingWeek = null)` | Retreat to the previous non-working day. Throws `InvalidOperationException` if none is found. |
| `NextNotableDate(service, territory, NotableDateFilter? filter = null)` | The next `NotableDate?` matching the optional filter, or `null` when none is found. |
| `PreviousNotableDate(service, territory, NotableDateFilter? filter = null)` | The previous `NotableDate?` matching the optional filter, or `null`. |

```csharp
DateOnly nextOpen    = today.NextWorkingDay(service, "AU-NSW");
DateOnly lastOpen    = today.PreviousWorkingDay(service, "AU-NSW");
NotableDate? holiday = today.NextNotableDate(service, "AU-NSW");
```

## Snap

The `Snap*` operations are no-ops when the input is already a working day; otherwise they shift to the nearest working day in the requested direction.

| Method | Effect |
|---|---|
| `SnapToWorkingDay(service, territory, WeekPattern? workingWeek = null)` | If the date is non-working, advance forward to the first working day. |
| `SnapToWorkingDayBackward(service, territory, WeekPattern? workingWeek = null)` | If the date is non-working, retreat to the previous working day. |
| `SnapToNearestWorkingDay(service, territory, WeekPattern? workingWeek = null)` | If the date is non-working, choose the closer of forward / backward snaps. |

```csharp
DateOnly saturday = new DateOnly(2026, 1, 3);                    // Saturday
DateOnly snapped  = saturday.SnapToWorkingDay(service, "AU-NSW"); // Monday 5 Jan 2026
```

## Arithmetic

| Method | Effect |
|---|---|
| `AddWorkingDays(int count, service, territory, WeekPattern? workingWeek = null)` | Add (or subtract, when `count` is negative) the signed number of working days, skipping non-working dates. |
| `WorkingDaysBetween(DateOnly end, service, territory, WeekPattern? workingWeek = null)` | Count the working days between the receiver and `end`. |

```csharp
DateOnly inFive  = today.AddWorkingDays(5, service, "AU-NSW");
DateOnly fiveAgo = today.AddWorkingDays(-5, service, "AU-NSW");
int span         = today.WorkingDaysBetween(inFive, service, "AU-NSW");
```

## Enumeration

Each enumeration returns a lazily-evaluated `IEnumerable<DateOnly>` (or `IEnumerable<NotableDate>`) over the inclusive range from the receiver to `end`. They are safe to combine with LINQ.

| Method | Yields |
|---|---|
| `EnumerateWorkingDays(DateOnly end, service, territory, WeekPattern? workingWeek = null)` | Every working day in the inclusive range. |
| `EnumerateNonWorkingDays(DateOnly end, service, territory, WeekPattern? workingWeek = null)` | Every non-working day in the inclusive range. |
| `EnumerateNotableDates(DateOnly end, service, territory, NotableDateFilter? filter = null)` | Every notable date in the inclusive range. |

```csharp
DateOnly start = new DateOnly(2026, 1, 1);
DateOnly end   = new DateOnly(2026, 1, 31);

foreach (DateOnly workday in start.EnumerateWorkingDays(end, service, "AU-NSW"))
{
    // process each open business day in January 2026
}
```

## Notable-date lookups

| Method | Returns |
|---|---|
| `GetNotableDates(service, territory, NotableDateFilter? filter = null)` | The notable dates that apply on the receiver date. |
| `GetNotableDatesInMonth(service, territory, NotableDateFilter? filter = null)` | Notable dates in the calendar month containing the receiver. *(`DateOnly` only.)* |
| `GetNotableDatesInYear(service, territory, NotableDateFilter? filter = null)` | Notable dates in the calendar year containing the receiver. *(`DateOnly` only.)* |

```csharp
IReadOnlyList<NotableDate> onDay     = today.GetNotableDates(service, "AU-NSW");
IReadOnlyList<NotableDate> thisMonth = today.GetNotableDatesInMonth(service, "AU-NSW");
IReadOnlyList<NotableDate> thisYear  = today.GetNotableDatesInYear(service, "AU-NSW");
```

To resolve a whole year independently of a receiver date, prefer the by-year service extension `service.Resolve(2026, "AU-NSW")`. See [Using NotableDateService](notable-dates.md).

## The working week

The optional trailing `WeekPattern? workingWeek` argument overrides the default Monday–Friday working week for a single call. `WeekPattern` is the <xref:Bodu.WeekPattern> value type from `Bodu.Core`; any day outside the pattern is treated as a weekend. Use the named presets for common shapes, or compose a custom pattern for non-standard schedules:

| Preset | Working days | Weekend days |
|---|---|---|
| `WeekPattern.MondayToFriday` *(default)* | Mon–Fri | Saturday + Sunday (most western territories). |
| `WeekPattern.SundayToThursday` | Sun–Thu | Friday + Saturday (parts of the Middle East). |
| `WeekPattern.MondayToSaturday` | Mon–Sat | Sunday only. |
| `WeekPattern.SaturdayToThursday` | Sat–Thu | Friday only. |
| `WeekPattern.AllDays` | Every day | No weekend — every day is working unless a non-working notable date applies. |

```csharp
using Bodu;                 // WeekPattern
using Bodu.Extensions;

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

// Sunday–Thursday working week (Friday/Saturday weekend, e.g. parts of the Middle East):
DateOnly nextOpen = today.NextWorkingDay(service, "AE", WeekPattern.SundayToThursday);
bool     isOpen   = today.IsWorkingDay(service, "AE", WeekPattern.SundayToThursday);
```

When omitted, the working-day extensions fall back to Monday–Friday. To bake a non-default working week into resolution itself (so adjustment triggers such as `IfWeekend` agree), set it on the resource's `<ResolutionPolicy workingDays="…">` (a 7-character Sunday-first binary string). See [Identity and resolution](identity-and-resolution.md).

## Fiscal-year helpers

`NotableDateFiscalExtensions` (over `DateOnly`) computes working-day boundaries of a fiscal year or quarter. Each method takes the month the fiscal year starts in (`1`–`12`), the service, the territory, and an optional working week:

| Method | Returns |
|---|---|
| `FirstWorkingDayOfFiscalYear(int fiscalYearStartMonth, service, territory, WeekPattern? workingWeek = null)` | The first working day of the fiscal year containing the receiver. |
| `LastWorkingDayOfFiscalYear(int fiscalYearStartMonth, service, territory, WeekPattern? workingWeek = null)` | The last working day of that fiscal year. |
| `FirstWorkingDayOfFiscalQuarter(int fiscalYearStartMonth, service, territory, WeekPattern? workingWeek = null)` | The first working day of the fiscal quarter containing the receiver. |
| `LastWorkingDayOfFiscalQuarter(int fiscalYearStartMonth, service, territory, WeekPattern? workingWeek = null)` | The last working day of that fiscal quarter. |

```csharp
using Bodu.Extensions;

DateOnly today = DateOnly.FromDateTime(DateTime.Today);

// Australian fiscal year starts in July (month 7):
DateOnly fyOpen  = today.FirstWorkingDayOfFiscalYear(7, service, "AU-NSW");
DateOnly fyClose = today.LastWorkingDayOfFiscalYear(7, service, "AU-NSW");
DateOnly qOpen   = today.FirstWorkingDayOfFiscalQuarter(7, service, "AU-NSW");
```

## Surface differences

`DateOnly` is authoritative and carries every method above. The `DateTime` and `DateTimeOffset` surfaces are subsets:

| Capability | `DateOnly` | `DateTime` | `DateTimeOffset` |
|---|---|---|---|
| Lookup (`IsWeekend`, `IsWorkingDay`, `IsNonWorkingDay`, `IsNotableDate`) | ✓ | ✓ | ✓ |
| `GetNotableDates` | ✓ | ✓ | ✓ |
| Navigation (`NextWorkingDay`, `PreviousWorkingDay`) | ✓ | ✓ | ✓ |
| Snap (`SnapToWorkingDay`, `SnapToWorkingDayBackward`, `SnapToNearestWorkingDay`) | ✓ | ✓ | ✓ |
| `AddWorkingDays`, `WorkingDaysBetween` | ✓ | ✓ | ✓ |
| `EnumerateWorkingDays`, `EnumerateNotableDates` | ✓ | ✓ | ✓ |
| `NextNonWorkingDay`, `PreviousNonWorkingDay`, `EnumerateNonWorkingDays` | ✓ | ✓ | — |
| `NextNotableDate`, `PreviousNotableDate` | ✓ | ✓ | — |
| `GetNotableDatesInMonth`, `GetNotableDatesInYear` | ✓ | — | — |
| Fiscal helpers (`NotableDateFiscalExtensions`) | ✓ | — | — |

When you need the month/year notable-date lookups or the fiscal helpers, work in `DateOnly`. The same operations over `DateTime` / `DateTimeOffset` are otherwise identical in shape.

## Public holidays vs. observances

Working-day arithmetic respects a resolved date's `IsNonWorkingDay` flag — not its category. A `NotableDateCategory.PublicHoliday` occurrence with `IsNonWorkingDay = false` does **not** cause working-day arithmetic to skip the date. Conversely, a `BankHoliday`, `Civic`, or `Cultural` occurrence flagged non-working *does* skip.

Authors choose this when defining the rule (via `defaultNonWorkingDay` / `nonWorking`). The data packs follow the convention that nationally legislated closures (public holidays, bank closures) are non-working, while purely commemorative observances are not.

## Territory-specific calculation

Every method takes a territory `string`. Because territories are hierarchical, a call with `"AU-NSW"` honours both national `AU` rules and NSW-specific `AU-NSW` rules. See [Territories and regional composition](territories.md) for the containment semantics.

## Where to go next

- **[Using NotableDateService](notable-dates.md)** — building the service, filters, and range queries.
- **[Territories and regional composition](territories.md)** — how the territory argument composes national and regional rules.
- **[Observance adjustment rules](adjustment-rules.md)** — how a rule's nominal date becomes the observed non-working day that working-day arithmetic ultimately skips.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — `NotableDateOnlyExtensions`, `NotableDateTimeExtensions`, `NotableDateTimeOffsetExtensions`, `NotableDateFiscalExtensions`.
