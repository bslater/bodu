---
title: Date and time extensions
---

# Date and time extensions

<xref:Bodu.Extensions.DateTimeExtensions> and <xref:Bodu.Extensions.DateOnlyExtensions> are two parallel families of extension methods — 66 and 56 partial files respectively — that answer the calendar questions `DateTime` and `DateOnly` leave to the caller: the first and last date of a week, month, quarter, or year; the next Friday; the third Monday of the month; the ISO week number; the fiscal quarter under a July-to-June year; whether a day is a rest day under a Sunday-to-Thursday working week; an age; a value truncated to the hour. The `DateOnly` family mirrors the `DateTime` family member for member, minus the time-of-day and conversion members that have no meaning for a date.

Two rules apply everywhere:

- **Boundary results are dates.** Every `First…` / `Last…` / `Next…` / `Previous…` member returns midnight (`TimeOfDay == 00:00`) with the receiver's <xref:System.DateTime.Kind> preserved.
- **Defaults are explicit.** A parameterless overload uses <xref:System.Globalization.CultureInfo.CurrentCulture> for week rules and names, a January-to-December calendar year for quarters, and a Saturday–Sunday weekend for weekday tests. Every one of those defaults has an overload that takes the rule as a parameter — see the [overload matrix](#overload-matrix) below.

> [!NOTE]
> Five members — `IsoWeekOfYear`, `IsoYear`, `WeekOrdinalOfMonth`, `IsFirstDateOfMonth`, and `IsLastDateOfMonth` — are C# 14 **extension properties** in the shipped library, so they are read without parentheses (`date.IsoWeekOfYear`). Consuming them needs `<LangVersion>14</LangVersion>` (or `latest` on the .NET 10 SDK); the API reference renders them in method form because DocFX cannot yet model extension blocks. Everything else is a classic extension method.

> [!NOTE]
> There is **no `TimeOnly` extension surface** in `Bodu.Core`. `TimeOnly` appears only as `TimeOnly.MinValue` when a `DateOnly` is lifted to a `DateTime` internally.

## Pattern 1 — boundaries of a week, month, quarter, or year

```csharp
using System.Globalization;
using Bodu;
using Bodu.Extensions;

var date = new DateTime(2024, 5, 15, 14, 30, 0);   // Wednesday 15 May 2024

DateTime weekStartGb = date.FirstDateOfWeek(CultureInfo.GetCultureInfo("en-GB"));   // 2024-05-13 (Monday)
DateTime weekStartUs = date.FirstDateOfWeek(CultureInfo.GetCultureInfo("en-US"));   // 2024-05-12 (Sunday)
DateTime weekStartGulf = date.FirstDateOfWeek(WorkingDaysOfWeek.SundayToThursday);  // 2024-05-12 (Sunday)
DateTime weekEnd     = date.LastDateOfWeek();          // 2024-05-19 under en-GB (Sunday)

DateTime monthStart   = date.FirstDateOfMonth();       // 2024-05-01
DateTime monthEnd     = date.LastDateOfMonth();        // 2024-05-31
DateTime quarterStart = date.FirstDateOfQuarter();     // 2024-04-01 — calendar quarters by default
DateTime quarterEnd   = date.LastDateOfQuarter();      // 2024-06-30
DateTime yearStart    = date.FirstDateOfYear();        // 2024-01-01
DateTime yearEnd      = date.LastDateOfYear();         // 2024-12-31

DateTime firstFriday  = date.FirstDateOfWeekInMonth(DayOfWeek.Friday);     // 2024-05-03
DateTime lastFriday   = date.LastDateOfWeekInMonth(DayOfWeek.Friday);      // 2024-05-31
DateTime firstMonQ    = date.FirstDateOfWeekInQuarter(DayOfWeek.Monday);   // 2024-04-01
DateTime lastSunYear  = date.LastDateOfWeekInYear(DayOfWeek.Sunday);       // 2024-12-29

TimeSpan tod = monthStart.TimeOfDay;                    // 00:00:00 — boundaries drop the time of day
```

`FirstDateOfWeek()` / `LastDateOfWeek()` read <xref:System.Globalization.DateTimeFormatInfo.FirstDayOfWeek> from the culture (Monday for `en-GB`, Sunday for `en-US` and the invariant culture). The `WorkingDaysOfWeek` overload starts the week on the first working day of that preset — Saturday for `SaturdayToThursday`, Sunday for `SundayToThursday`, Monday for `MondayToFriday`.

## Pattern 2 — navigating by day of week

```csharp
using Bodu;
using Bodu.Extensions;

var date = new DateTime(2024, 5, 15);   // Wednesday

DateTime nextWed     = date.NextDateOfWeek(DayOfWeek.Wednesday);           // 2024-05-22 — strictly after
DateTime sameWed     = date.NextOrSameDateOfWeek(DayOfWeek.Wednesday);     // 2024-05-15 — today qualifies
DateTime prevMon     = date.PreviousDateOfWeek(DayOfWeek.Monday);          // 2024-05-13
DateTime prevOrSame  = date.PreviousOrSameDateOfWeek(DayOfWeek.Wednesday); // 2024-05-15
DateTime nearestSun  = date.NearestDateOfWeek(DayOfWeek.Sunday);           // 2024-05-12 — 3 days back beats 4 forward
DateTime nearestSat  = date.NearestDateOfWeek(DayOfWeek.Saturday);         // 2024-05-18

DateTime nextWorking = date.NextWeekday(WorkingDaysOfWeek.MondayToFriday); // 2024-05-16 (Thursday)
DateTime afterFriday = new DateTime(2024, 5, 17).NextWeekday(WorkingDaysOfWeek.MondayToFriday);   // 2024-05-20 (Monday)
DateTime mwfOnly     = new DateTime(2024, 5, 17).NextWeekday(WeekPattern.Parse("_M_W_F_"));       // 2024-05-20
DateTime prevWorking = date.PreviousWeekday(WorkingDaysOfWeek.SundayToThursday);                  // 2024-05-14

DateTime lastMonday  = date.NthDateOfWeekInMonth(DayOfWeek.Monday, WeekOrdinal.Last);    // 2024-05-27
DateTime fifthFriday = date.NthDateOfWeekInMonth(DayOfWeek.Friday, WeekOrdinal.Fifth);   // 2024-05-31
DateTime thanksgiving = DateTimeExtensions.GetNthDateOfWeekInMonth(2024, 11, DayOfWeek.Thursday, WeekOrdinal.Fourth);   // 2024-11-28

var seriesStart = new DateTime(2024, 1, 1, 9, 0, 0);
DateTime nextRun = seriesStart.NextOccurrence(TimeSpan.FromDays(7), after: date);        // 2024-05-20 09:00
DateTime lastRun = seriesStart.PreviousOccurrence(TimeSpan.FromDays(7), before: date);   // 2024-05-13 09:00
```

- `Next…` / `Previous…` are strict; the `…OrSame…` variants accept the receiver's own day. `NearestDateOfWeek` prefers the earlier date on a tie.
- `NextWeekday` / `PreviousWeekday` step one day at a time until the day is a working day under the supplied <xref:Bodu.WorkingDaysOfWeek> preset, <xref:Bodu.WeekPattern>, or `Custom` + <xref:Bodu.Extensions.IWeekendDefinitionProvider> — see [Fiscal quarters, working weeks, and weekend providers](calendar-shapes-and-providers.md).
- `NthDateOfWeekInMonth` takes a <xref:Bodu.Extensions.WeekOrdinal> (`First` … `Fifth`, `Last`) and throws `ArgumentOutOfRangeException` when the month has no such day (a fifth Monday in May 2024, for example); `Last` never throws.
- `NextOccurrence(interval, after)` / `PreviousOccurrence(interval, before)` treat the receiver as the start of a fixed-interval series and return the first occurrence strictly after (or before) the bound, preserving the start's time of day. When `after` is at or before the start, the start itself is returned.

## Pattern 3 — ISO 8601 weeks

The ISO members are independent of culture: weeks start on Monday and week 1 is the week containing the first Thursday.

```csharp
using Bodu.Extensions;

var date = new DateTime(2024, 12, 30);   // Monday — ISO week 1 of 2025

int week    = date.IsoWeekOfYear;                                   // 1   (C# 14 extension property)
int isoYear = date.IsoYear;                                         // 2025
int mid     = new DateTime(2024, 5, 15).IsoWeekOfYear;              // 20

DateTime w1Start = DateTimeExtensions.GetFirstDateOfIsoWeek(2025, 1);   // 2024-12-30
DateTime w1End   = DateTimeExtensions.GetLastDateOfIsoWeek(2025, 1);    // 2025-01-05
int weeks2026    = DateTimeExtensions.GetIsoWeeksInYear(2026);          // 53
int weeks2024    = DateTimeExtensions.GetIsoWeeksInYear(2024);          // 52
```

## Pattern 4 — culture week numbers and week-of-month

`WeekOfYear` and `WeekOfMonth` follow the BCL's <xref:System.Globalization.CalendarWeekRule> and first-day-of-week — from the current culture, an explicit culture, or an explicit rule pair.

```csharp
using System.Globalization;
using Bodu.Extensions;

var date = new DateTime(2024, 12, 30);

int cultureWeek = date.WeekOfYear(CultureInfo.GetCultureInfo("en-US"));                 // 53
int ruleWeek    = date.WeekOfYear(CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday); // 53 — BCL numbering, not ISO
int weekOfMonth = date.WeekOfMonth(CalendarWeekRule.FirstDay, DayOfWeek.Sunday);        // 5
WeekOrdinal ordinal = date.WeekOrdinalOfMonth;                                          // Fifth (the 30th is in the 5th group of 7)
WeekOrdinal third   = new DateTime(2024, 5, 15).WeekOrdinalOfMonth;                     // Third

DateTime week20 = DateTimeExtensions.GetStartDateOfWeek(2024, 20, CultureInfo.GetCultureInfo("en-GB"));   // 2024-05-13
```

> [!WARNING]
> `WeekOfYear(CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)` reproduces <xref:System.Globalization.Calendar.GetWeekOfYear*>, which numbers the last days of December as week 53 even when ISO 8601 assigns them to week 1 of the next year. Use `IsoWeekOfYear` / `IsoYear` when you need ISO semantics.

`WeekOrdinalOfMonth` is *not* a week number: it reports which seven-day group of the month the date falls in (days 1–7 → `First`, 8–14 → `Second`, …, 29–31 → `Fifth`), which is exactly the ordinal `NthDateOfWeekInMonth` would need to land on that date.

## Pattern 5 — quarters and fiscal years

```csharp
using Bodu.Extensions;

var date = new DateTime(2024, 5, 15);

int calendarQ = date.Quarter();                                            // 2
int auQ       = date.Quarter(CalendarQuarterDefinition.JulyToJune);        // 4 — Australian financial year
int inQ       = date.Quarter(CalendarQuarterDefinition.AprilToMarch);      // 1
int ukTaxQ    = date.Quarter(CalendarQuarterDefinition.April6ToApril5);    // 1

DateTime q4Start = date.FirstDateOfQuarter(CalendarQuarterDefinition.JulyToJune);      // 2024-04-01
DateTime q1End   = date.LastDateOfQuarter(CalendarQuarterDefinition.April6ToApril5);   // 2024-07-05
DateTime fyQ1    = DateTimeExtensions.GetFirstDateOfQuarter(2024, 1, CalendarQuarterDefinition.JulyToJune);   // 2024-07-01
bool isStart     = date.IsFirstDateOfQuarter(CalendarQuarterDefinition.JanuaryToDecember);   // false
```

<xref:Bodu.Extensions.CalendarQuarterDefinition> covers the month- and day-anchored calendars (`JanuaryToDecember`, `JulyToJune`, `AprilToMarch`, `April6ToApril5`, `March25ToMarch24`, `OctoberToSeptember`, `FebruaryToJanuary`). Passing `CalendarQuarterDefinition.Custom` to any of these overloads throws `InvalidOperationException` — a custom shape needs an <xref:Bodu.Extensions.IQuarterDefinitionProvider>, which unlocks the fiscal-year members (`FiscalYear`, `FirstDateOfFiscalYear`, `LastDateOfFiscalYear`, `IsFirstDateOfFiscalYear`, `IsLastDateOfFiscalYear`, `AddFiscalYears`). Both are covered in [Fiscal quarters, working weeks, and weekend providers](calendar-shapes-and-providers.md).

## Pattern 6 — predicates

```csharp
using System.Globalization;
using Bodu;
using Bodu.Extensions;

var saturday = new DateTime(2024, 5, 18);

bool weekend    = saturday.IsWeekend();                                         // true  — Saturday–Sunday default
bool weekday    = saturday.IsWeekday();                                         // false
bool sixDayWeek = saturday.IsWeekday(WorkingDaysOfWeek.MondayToSaturday);       // true
bool gulfRest   = saturday.IsWeekend(WorkingDaysOfWeek.SundayToThursday);       // true
bool working    = saturday.IsInWorkingWeek(WorkingDaysOfWeek.SaturdayToWednesday);   // true
bool rest       = saturday.IsRestDay(WeekPattern.Weekdays);                     // true
bool friOff     = DateTimeExtensions.IsWeekend(DayOfWeek.Friday, WorkingDaysOfWeek.SundayToThursday);   // true

bool lastOfQ    = new DateTime(2024, 6, 30).IsLastDateOfQuarter();              // true
bool firstOfM   = new DateTime(2024, 5, 1).IsFirstDateOfMonth;                  // true  (extension property)
bool leap       = saturday.IsLeapYear();                                        // true
bool inMay      = saturday.IsInRange(new DateTime(2024, 5, 1), new DateTime(2024, 5, 31));   // true — inclusive
int daysInMay   = saturday.DaysInMonth();                                       // 31
int daysInYear  = saturday.DaysInYear();                                        // 366
string dayName  = saturday.DayName(CultureInfo.GetCultureInfo("fr-FR"));        // "samedi"
string month    = saturday.MonthName(CultureInfo.InvariantCulture);             // "May"
```

`IsWeekday` / `IsWeekend` and `IsInWorkingWeek` / `IsRestDay` are complementary pairs. The first pair takes the named <xref:Bodu.WorkingDaysOfWeek> preset (and, for `Custom`, a weekend provider); the second takes either the preset or a raw <xref:Bodu.WeekPattern>. `IsInRange` has an overload on `DateTime?` that returns `false` for `null`.

## Pattern 7 — age, truncation, and day boundaries

```csharp
using Bodu.Extensions;

var born = new DateTime(1990, 2, 28);
var asAt = new DateTime(2024, 5, 15, 14, 30, 45, 123);

int age = born.Age(asAt);                                     // 34 — the receiver is the earlier date
DateTime hour    = asAt.Truncate(DateTimeResolution.Hour);    // 2024-05-15 14:00:00
DateTime month   = asAt.Truncate(DateTimeResolution.Month);   // 2024-05-01 00:00:00
DateTime start   = asAt.StartOfDay();                         // 2024-05-15 00:00:00 (Midnight() is the same)
DateTime end     = asAt.EndOfDay();                           // 2024-05-15 23:59:59.9999999 — the last tick
DateTime noon    = asAt.Midday();                             // 2024-05-15 12:00:00
DateTime shifted = asAt.Add(years: 1, months: -1, days: 0.5); // 2025-04-16 02:30:45 — fractional days allowed
```

`Age()` without an argument measures against today; the two-argument form is deterministic. A 29 February birth date is treated as 28 February in non-leap years, and a reference date before the birth date yields 0. <xref:Bodu.Extensions.DateTimeResolution> has the eight stops `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, and `Tick`.

## Pattern 8 — conversions and static companions

```csharp
using System.Globalization;
using Bodu.Extensions;

var utc = new DateTime(2024, 5, 15, 14, 30, 0, DateTimeKind.Utc);

DateOnly day            = utc.ToDateOnly();                          // 2024-05-15
DateTimeOffset asOffset = utc.ToDateTimeOffset();                    // 2024-05-15 14:30 +00:00 — offset from Kind
DateTimeOffset explicitOffset = new DateTime(2024, 5, 15, 14, 30, 0).ToDateTimeOffset(TimeSpan.FromHours(10));   // +10:00
string iso    = utc.ToIsoString();                                   // "2024-05-15T14:30:00.0000000Z"
long unix     = utc.ToUnixTimeSeconds();                             // 1715783400
DateTime back = DateTimeExtensions.FromUnixTimeSeconds(1715783400);  // 2024-05-15 14:30:00

DateTime leapDay = DateTimeExtensions.FromDayOfYear(2024, 60);           // 2024-02-29
DateTime febEnd  = DateTimeExtensions.GetLastDateOfMonth(2024, 2);       // 2024-02-29
DateTime firstFri = DateTimeExtensions.GetFirstDateOfWeekInMonth(2024, 5, DayOfWeek.Friday);   // 2024-05-03
string mai       = DateTimeExtensions.GetMonthName(5, CultureInfo.GetCultureInfo("de-DE"));   // "Mai"
DateTime later   = DateTimeExtensions.Max(utc, utc.AddDays(1));           // the later of the two
DateTime? maybe  = DateTimeExtensions.Min((DateTime?)null, utc);          // null-aware overloads
TimeSpan elapsed = utc.ElapsedTimeSince();                                // now - utc
```

`ToDateTimeOffset(TimeSpan offset)` requires a non-UTC receiver — a `DateTimeKind.Utc` value with a non-zero offset throws `ArgumentException`, exactly as the <xref:System.DateTimeOffset> constructor does. `ToIsoString` has overloads for suppressing fractional seconds, for stamping a `DateTimeKind`, and for an arbitrary format plus culture.

Every `Get…` static on `DateTimeExtensions` is the year/month-addressed companion of an instance member, for when you have no `DateTime` to start from: `GetFirstDateOfMonth`, `GetLastDateOfMonth`, `GetFirstDateOfQuarter`, `GetLastDateOfQuarter` (each with an optional `CalendarQuarterDefinition`), `GetFirstDateOfWeekInMonth`, `GetLastDateOfWeekInMonth`, `GetFirstDateOfWeekInQuarter`, `GetLastDateOfWeekInQuarter`, `GetNthDateOfWeekInMonth`, `GetNearestDateOfWeek`, `GetFirstDateOfIsoWeek`, `GetLastDateOfIsoWeek`, `GetIsoWeeksInYear`, `GetStartDateOfWeek`, `GetMonthName`, `GetDayNumber`, `FromDayOfYear`, `FromUnixTimeSeconds` / `FromUnixTimeMilliseconds`, `FirstDateOfFiscalYear` / `LastDateOfFiscalYear`, and `Max` / `Min`.

## Pattern 9 — the `DateOnly` family

Every calendar member above exists on `DateOnly` with the same name and rules. The differences are the members that need a time of day or an instant:

```csharp
using Bodu.Extensions;

var day = new DateOnly(2024, 5, 15);

DateOnly weekStart = day.FirstDateOfWeek();                            // 2024-05-13 under en-GB
DateOnly friday    = day.NextDateOfWeek(DayOfWeek.Friday);             // 2024-05-17
int isoWeek        = day.IsoWeekOfYear;                                // 20
int auQ            = day.Quarter(CalendarQuarterDefinition.JulyToJune);// 4
DateOnly lastMon   = day.NthDateOfWeekInMonth(DayOfWeek.Monday, WeekOrdinal.Last);   // 2024-05-27
DateOnly moved     = day.Add(years: 0, months: 1, days: -1);           // 2024-06-14 — whole days only
int age            = new DateOnly(1990, 2, 28).Age(day);               // 34
DateOnly nextRun   = new DateOnly(2024, 1, 1).NextOccurrence(intervalDays: 7, after: day);   // 2024-05-20
DateOnly w1        = DateOnlyExtensions.GetFirstDateOfIsoWeek(2025, 1);// 2024-12-30
long unix          = day.ToUnixTimeSeconds();                          // 1715731200 — midnight UTC
```

| `DateTime` member | `DateOnly` counterpart |
|---|---|
| `Add(years, months, double days)` | `Add(years, months, int days)` |
| `NextOccurrence(TimeSpan, after)` / `PreviousOccurrence(TimeSpan, before)` | `NextOccurrence(int intervalDays, after)` / `PreviousOccurrence(int intervalDays, before)` |
| `StartOfDay`, `EndOfDay`, `Midnight`, `Midday`, `Truncate(DateTimeResolution)` | — (no time of day) |
| `ToDateOnly`, `ToDateTimeOffset`, `ToTimeSpan`, `ToIsoString`, `ElapsedTimeSince` | — (use `DateOnly.ToDateTime` / `ToString("O")`) |
| `GetDayNumber`, `GetMonthName`, static `IsWeekday(DayOfWeek, …)` / `IsWeekend(DayOfWeek, …)` | — (call the `DateTimeExtensions` statics) |
| everything else — boundaries, navigation, ISO and culture weeks, quarters and fiscal years, predicates, `Age`, Unix time, `Max` / `Min`, the `Get…` companions | same name, same rules |

## Overload matrix

| Member family | Parameterless default | `CultureInfo?` | `WorkingDaysOfWeek` / `WeekPattern` | `CalendarQuarterDefinition` | `IQuarterDefinitionProvider` | `IWeekendDefinitionProvider` |
|---|---|---|---|---|---|---|
| `FirstDateOfWeek`, `LastDateOfWeek` | current culture's first day | ✓ | `WorkingDaysOfWeek` (first working day starts the week) | — | — | — |
| `Quarter`, `FirstDateOfQuarter`, `LastDateOfQuarter`, `IsFirstDateOfQuarter`, `IsLastDateOfQuarter`, `FirstDateOfWeekInQuarter`, `LastDateOfWeekInQuarter` | `JanuaryToDecember` | — | — | ✓ (not `Custom`) | ✓ | — |
| `FiscalYear`, `AddFiscalYears`, `IsFirstDateOfFiscalYear`, `IsLastDateOfFiscalYear`, static `FirstDateOfFiscalYear` / `LastDateOfFiscalYear` | — (provider required) | — | — | — | ✓ | — |
| `IsWeekday`, `IsWeekend` | Saturday–Sunday weekend | — | `WorkingDaysOfWeek` | — | — | ✓ optional; **required** with `WorkingDaysOfWeek.Custom` |
| `IsInWorkingWeek`, `IsRestDay` | — (pattern required) | — | `WorkingDaysOfWeek` or `WeekPattern` | — | — | — |
| `NextWeekday`, `PreviousWeekday` | — (pattern required) | — | `WorkingDaysOfWeek` or `WeekPattern` | — | — | ✓ with `WorkingDaysOfWeek.Custom` |
| `WeekOfYear`, `WeekOfMonth` | current culture's rule + first day | ✓ | — | — | — | — (also `(CalendarWeekRule, DayOfWeek)`) |
| `DayName`, `MonthName`, static `GetMonthName` | current culture | ✓ | — | — | — | — |
| `DaysInMonth` | Gregorian | ✓ (also `Calendar?`) | — | — | — | — |
| `DaysInYear` | Gregorian | — (`Calendar?` only) | — | — | — | — |
| static `GetStartDateOfWeek(year, week, culture)` | current culture | ✓ | — | — | — | — |

A `null` `CultureInfo?` or `Calendar?` argument falls back to the same default as the parameterless overload.

## API summary

| Group | `DateTime` members |
|---|---|
| Boundaries | `FirstDateOfWeek`, `LastDateOfWeek`, `FirstDateOfMonth`, `LastDateOfMonth`, `FirstDateOfQuarter`, `LastDateOfQuarter`, `FirstDateOfYear`, `LastDateOfYear`, `FirstDateOfWeekInMonth`, `LastDateOfWeekInMonth`, `FirstDateOfWeekInQuarter`, `LastDateOfWeekInQuarter`, `FirstDateOfWeekInYear`, `LastDateOfWeekInYear` |
| Navigation | `NextDateOfWeek`, `PreviousDateOfWeek`, `NextOrSameDateOfWeek`, `PreviousOrSameDateOfWeek`, `NearestDateOfWeek`, `NextWeekday`, `PreviousWeekday`, `NthDateOfWeekInMonth`, `NextOccurrence`, `PreviousOccurrence`, `Add` |
| ISO weeks | `IsoWeekOfYear`, `IsoYear` (properties); static `GetFirstDateOfIsoWeek`, `GetLastDateOfIsoWeek`, `GetIsoWeeksInYear` |
| Culture weeks | `WeekOfYear`, `WeekOfMonth`, `WeekOrdinalOfMonth` (property); static `GetStartDateOfWeek` |
| Quarters and fiscal years | `Quarter`, `FiscalYear`, `AddFiscalYears`, `IsFirstDateOfQuarter`, `IsLastDateOfQuarter`, `IsFirstDateOfFiscalYear`, `IsLastDateOfFiscalYear`; static `FirstDateOfFiscalYear`, `LastDateOfFiscalYear` |
| Predicates | `IsWeekday`, `IsWeekend`, `IsInWorkingWeek`, `IsRestDay`, `IsFirstDateOfMonth`, `IsLastDateOfMonth` (properties), `IsLeapYear`, `IsInRange` |
| Time of day | `StartOfDay`, `EndOfDay`, `Midnight`, `Midday`, `Truncate(DateTimeResolution)` |
| Measures and names | `Age`, `DaysInMonth`, `DaysInYear`, `DayName`, `MonthName`, `ElapsedTimeSince` |
| Conversions | `ToDateOnly`, `ToDateTimeOffset`, `ToTimeSpan`, `ToIsoString`, `ToUnixTimeSeconds`, `ToUnixTimeMilliseconds`; static `FromUnixTimeSeconds`, `FromUnixTimeMilliseconds`, `FromDayOfYear`, `GetDayNumber`, `GetMonthName`, `Max`, `Min` |
| Static companions | `GetFirstDateOfMonth`, `GetLastDateOfMonth`, `GetFirstDateOfQuarter`, `GetLastDateOfQuarter`, `GetFirstDateOfWeekInMonth`, `GetLastDateOfWeekInMonth`, `GetFirstDateOfWeekInQuarter`, `GetLastDateOfWeekInQuarter`, `GetNthDateOfWeekInMonth`, `GetNearestDateOfWeek`, `IsWeekday(DayOfWeek, …)`, `IsWeekend(DayOfWeek, …)` |

## Where to go next

- [Fiscal quarters, working weeks, and weekend providers](calendar-shapes-and-providers.md) — `CalendarQuarterDefinition`, `IQuarterDefinitionProvider`, `FiscalWeekQuarterProvider`, `IWeekendDefinitionProvider`, and the `WorkingDaysOfWeek` ↔ `WeekPattern` bridge.
- [WeekPattern](week-pattern.md) — the seven-day bitmask the working-week overloads consume.
- [String extensions](string-extensions.md) — the sibling `string` surface.
- [Working-day and notable-date calculations](../calendar/index.md) — when a plain working week is not enough and public holidays matter, `Bodu.Globalization.Calendar` layers on top of these extensions.
- [`Bodu.Extensions` API reference](xref:Bodu.Extensions) — every overload with full signatures.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
