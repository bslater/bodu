---
title: Fiscal quarters, working weeks, and weekend providers
---

# Fiscal quarters, working weeks, and weekend providers

The [date extensions](date-extensions.md) default to a January-to-December year, a Monday-to-Friday working week, and a Saturday–Sunday weekend. This guide covers the four types that change those defaults — <xref:Bodu.Extensions.CalendarQuarterDefinition> for the common month-anchored quarter systems, <xref:Bodu.Extensions.IQuarterDefinitionProvider> for any other quarter shape (with the built-in <xref:Bodu.Extensions.FiscalWeekQuarterProvider> for 52/53-week retail calendars), <xref:Bodu.Extensions.IWeekendDefinitionProvider> for a weekend that no <xref:Bodu.WorkingDaysOfWeek> preset names — and the bridge between `WorkingDaysOfWeek` and <xref:Bodu.WeekPattern> in <xref:Bodu.Extensions.WorkingDaysOfWeekExtensions>.

All of the examples below were run against the library; the comments show the actual results.

## Pattern 1 — the built-in quarter definitions

<xref:Bodu.Extensions.CalendarQuarterDefinition> names seven fixed quarter systems. Each is a value you pass to the `Quarter` / `FirstDateOfQuarter` / `LastDateOfQuarter` / `IsFirstDateOfQuarter` / `IsLastDateOfQuarter` / `FirstDateOfWeekInQuarter` / `LastDateOfWeekInQuarter` overloads, and to the static `GetFirstDateOfQuarter` / `GetLastDateOfQuarter` / `GetFirstDateOfWeekInQuarter` / `GetLastDateOfWeekInQuarter` companions.

| Value | Q1 | Q2 | Q3 | Q4 | Typical use |
|---|---|---|---|---|---|
| `JanuaryToDecember` (default) | 1 Jan – 31 Mar | 1 Apr – 30 Jun | 1 Jul – 30 Sep | 1 Oct – 31 Dec | Calendar year |
| `FebruaryToJanuary` | 1 Feb – 30 Apr | 1 May – 31 Jul | 1 Aug – 31 Oct | 1 Nov – 31 Jan | Some retail and agricultural years |
| `March25ToMarch24` | 25 Mar – 24 Jun | 25 Jun – 24 Sep | 25 Sep – 24 Dec | 25 Dec – 24 Mar | Historical civil year (Lady Day) |
| `AprilToMarch` | 1 Apr – 30 Jun | 1 Jul – 30 Sep | 1 Oct – 31 Dec | 1 Jan – 31 Mar | India, Japan, many public bodies |
| `April6ToApril5` | 6 Apr – 5 Jul | 6 Jul – 5 Oct | 6 Oct – 5 Jan | 6 Jan – 5 Apr | UK personal tax year |
| `JulyToJune` | 1 Jul – 30 Sep | 1 Oct – 31 Dec | 1 Jan – 31 Mar | 1 Apr – 30 Jun | Australia, New Zealand |
| `OctoberToSeptember` | 1 Oct – 31 Dec | 1 Jan – 31 Mar | 1 Apr – 30 Jun | 1 Jul – 30 Sep | US federal government |
| `Custom` | — | — | — | — | Marker only; passing it throws `InvalidOperationException` — supply an `IQuarterDefinitionProvider` instead |

```csharp
using Bodu.Extensions;

var date = new DateTime(2024, 5, 15);

foreach (CalendarQuarterDefinition definition in Enum.GetValues<CalendarQuarterDefinition>())
{
    if (definition == CalendarQuarterDefinition.Custom) continue;

    Console.WriteLine($"{definition,-20} Q{date.Quarter(definition)}  {date.FirstDateOfQuarter(definition):yyyy-MM-dd} .. {date.LastDateOfQuarter(definition):yyyy-MM-dd}");
}

// JanuaryToDecember    Q2  2024-04-01 .. 2024-06-30
// FebruaryToJanuary    Q2  2024-05-01 .. 2024-07-31
// March25ToMarch24     Q1  2024-03-25 .. 2024-06-24
// AprilToMarch         Q1  2024-04-01 .. 2024-06-30
// April6ToApril5       Q1  2024-04-06 .. 2024-07-05
// JulyToJune           Q4  2024-04-01 .. 2024-06-30
// OctoberToSeptember   Q3  2024-04-01 .. 2024-06-30
```

The definitions only know about *quarters*. The fiscal-year members (`FiscalYear`, `AddFiscalYears`, `FirstDateOfFiscalYear`, …) require a provider, because a fiscal year's label ("FY2024") is a policy decision the enum does not encode.

## Pattern 2 — implementing `IQuarterDefinitionProvider`

<xref:Bodu.Extensions.IQuarterDefinitionProvider> is the extension point for any quarter shape. It has twelve abstract members in `DateTime` / `DateOnly` pairs — `GetQuarter`, `GetQuarterStart`, `GetQuarterEnd`, `GetQuarterStartDate`, `GetQuarterEndDate` (each with a by-date and a by-`(quarter, fiscalYear)` form), `Is53WeekFiscalYear`, and `GetWeeksInFiscalYear` — plus two `GetFiscalYear` members with a **default implementation** that probes the candidate years around the date's calendar year against your `GetQuarterStart(1, y)` / `GetQuarterEnd(4, y)`. You can accept that default or override it directly.

The provider below models a fiscal year that opens on 1 February and is labelled by its opening calendar year: FY2024 runs 1 Feb 2024 – 31 Jan 2025.

```csharp
using Bodu.Extensions;

/// <summary>A fiscal year that opens on 1 February: quarters begin on 1 Feb, 1 May, 1 Aug, and 1 Nov.</summary>
public sealed class FebruaryQuarterProvider : IQuarterDefinitionProvider
{
    private static readonly int[] s_startMonths = { 2, 5, 8, 11 };

    public int GetQuarter(DateTime dateTime) => GetQuarter(DateOnly.FromDateTime(dateTime));

    public int GetQuarter(DateOnly dateOnly) =>
        dateOnly.Month == 1 ? 4 : ((dateOnly.Month - 2) / 3) + 1;

    // The interface supplies GetFiscalYear by probing quarter bounds; a direct answer is cheaper and clearer.
    public int GetFiscalYear(DateTime dateTime) => GetFiscalYear(DateOnly.FromDateTime(dateTime));

    public int GetFiscalYear(DateOnly dateOnly) => dateOnly.Month == 1 ? dateOnly.Year - 1 : dateOnly.Year;

    public DateOnly GetQuarterStartDate(int quarter, int fiscalYear)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quarter, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quarter, 4);

        return new DateOnly(fiscalYear, s_startMonths[quarter - 1], 1);
    }

    public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) =>
        quarter == 4
            ? new DateOnly(fiscalYear + 1, 1, 31)
            : GetQuarterStartDate(quarter + 1, fiscalYear).AddDays(-1);

    public DateOnly GetQuarterStartDate(DateOnly dateOnly) =>
        GetQuarterStartDate(GetQuarter(dateOnly), GetFiscalYear(dateOnly));

    public DateOnly GetQuarterEndDate(DateOnly dateOnly) =>
        GetQuarterEndDate(GetQuarter(dateOnly), GetFiscalYear(dateOnly));

    public DateTime GetQuarterStart(DateTime dateTime) =>
        GetQuarterStartDate(DateOnly.FromDateTime(dateTime)).ToDateTime(TimeOnly.MinValue, dateTime.Kind);

    public DateTime GetQuarterStart(int quarter, int fiscalYear) =>
        GetQuarterStartDate(quarter, fiscalYear).ToDateTime(TimeOnly.MinValue);

    public DateTime GetQuarterEnd(DateTime dateTime) =>
        GetQuarterEndDate(DateOnly.FromDateTime(dateTime)).ToDateTime(TimeOnly.MinValue, dateTime.Kind);

    public DateTime GetQuarterEnd(int quarter, int fiscalYear) =>
        GetQuarterEndDate(quarter, fiscalYear).ToDateTime(TimeOnly.MinValue);

    // Month-anchored years have no 53-week variant; report the conventional 52.
    public bool Is53WeekFiscalYear(int fiscalYear) => false;

    public int GetWeeksInFiscalYear(int fiscalYear) => 52;
}
```

Once a provider exists, every provider overload on the date extensions lights up:

```csharp
using Bodu.Extensions;

var provider = new FebruaryQuarterProvider();
var date = new DateTime(2025, 1, 20);

int quarter        = date.Quarter(provider);                 // 4
int fiscalYear     = date.FiscalYear(provider);              // 2024
DateTime qStart    = date.FirstDateOfQuarter(provider);      // 2024-11-01
DateTime qEnd      = date.LastDateOfQuarter(provider);       // 2025-01-31
bool lastDay       = date.IsLastDateOfFiscalYear(provider);  // false
DateTime fyStart   = DateTimeExtensions.FirstDateOfFiscalYear(2024, provider);   // 2024-02-01
DateTime fyEnd     = DateTimeExtensions.LastDateOfFiscalYear(2024, provider);    // 2025-01-31
DateTime nextYear  = date.AddFiscalYears(1, provider);       // 2026-01-21 — keeps the day offset into the fiscal year
DateTime firstMonQ = date.FirstDateOfWeekInQuarter(DayOfWeek.Monday, provider);  // 2024-11-04
int dateOnlyQ      = new DateOnly(2025, 1, 20).Quarter(provider);                // 4
```

Contract notes for implementers:

- `GetQuarterStart` / `GetQuarterEnd` return the boundary **dates at midnight** and should preserve the receiver's `DateTimeKind`; the extensions compare them against date-truncated values.
- `GetQuarterEnd(q, fy)` must be the day before `GetQuarterStart(q + 1, fy)`, and `GetQuarterEnd(4, fy)` the day before `GetQuarterStart(1, fy + 1)`, or the default `GetFiscalYear` probe will find gaps.
- `AddFiscalYears` preserves the date's offset (in days) from its fiscal-year start, which is why 20 Jan 2025 — day 354 of a 366-day fiscal year — becomes 21 Jan 2026 in the 365-day one.

## Pattern 3 — 52/53-week retail calendars with `FiscalWeekQuarterProvider`

<xref:Bodu.Extensions.FiscalWeekQuarterProvider> is the built-in provider for calendars whose year is made of whole weeks: four 13-week quarters (52 weeks), with a 53rd week folded into Q4 roughly every five or six years. It is configured entirely through its constructor:

| Parameter | Default | Meaning |
|---|---|---|
| `month` | — | The anchor month (1–12). |
| `dayOfWeek` | `Saturday` | The day each fiscal week starts on. |
| `isFiscalYearEnd` | `true` | `true`: `month` is the year's *closing* month and the year opens in the following month. `false`: `month` is the opening month itself. |
| `useNearestDayOfWeek` | `true` | `true`: the year starts on the `dayOfWeek` **nearest** to the 1st of the opening month (may be in the previous month). `false`: the `dayOfWeek` **on or before** it. |
| `pattern` | `Weeks445` | How the 13 weeks of a quarter split into three periods — see below. |

A retail (NRF-style) 4-5-4 calendar ends in January, starts its weeks on Sunday, and opens on the Sunday nearest 1 February:

```csharp
using Bodu.Extensions;

var retail = new FiscalWeekQuarterProvider(
    month: 1,
    dayOfWeek: DayOfWeek.Sunday,
    isFiscalYearEnd: true,
    useNearestDayOfWeek: true,
    pattern: FiscalWeekPattern.Weeks454);

DateTime q1Start = retail.GetQuarterStart(1, 2024);   // 2024-02-04 — the Sunday nearest 1 Feb 2024
DateTime q1End   = retail.GetQuarterEnd(1, 2024);     // 2024-05-04 — 13 weeks later
DateTime q4Start = retail.GetQuarterStart(4, 2024);   // 2024-11-03
DateTime q4End   = retail.GetQuarterEnd(4, 2024);     // 2025-02-01
int weeks2024    = retail.GetWeeksInFiscalYear(2024); // 52
bool long2023    = retail.Is53WeekFiscalYear(2023);   // true — FY2023 ran 29 Jan 2023 – 3 Feb 2024
int weeks2023    = retail.GetWeeksInFiscalYear(2023); // 53

var date = new DateTime(2024, 5, 15);
int quarter      = date.Quarter(retail);              // 2
int fiscalYear   = date.FiscalYear(retail);           // 2024
DateTime qStart  = date.FirstDateOfQuarter(retail);   // 2024-05-05
DateTime qEnd    = date.LastDateOfQuarter(retail);    // 2024-08-03
int janFy        = new DateTime(2025, 1, 15).FiscalYear(retail);         // 2024 — January belongs to the prior label
int lastDayFy    = retail.GetFiscalYear(new DateOnly(2024, 2, 3));       // 2023 — last day of the 53-week year
```

### The three week patterns

<xref:Bodu.Extensions.FiscalWeekPattern> records how each 13-week quarter is divided into three periods (months). The quarter *boundaries* the provider reports are the same for all three — every quarter is 13 weeks, with the 53rd week added to Q4 — so the pattern is informational for consumers that need period-level arithmetic:

| Pattern | Period lengths | Q1 example |
|---|---|---|
| `Weeks544` | 5, 4, 4 | weeks 1–5, 6–9, 10–13 |
| `Weeks454` | 4, 5, 4 | weeks 1–4, 5–9, 10–13 |
| `Weeks445` (default) | 4, 4, 5 | weeks 1–4, 5–8, 9–13; in a 53-week year Q4's third period is 6 weeks |

```csharp
using Bodu.Extensions;

foreach (FiscalWeekPattern pattern in Enum.GetValues<FiscalWeekPattern>())
{
    var p = new FiscalWeekQuarterProvider(month: 1, DayOfWeek.Sunday, pattern: pattern);
    Console.WriteLine($"{pattern}: Q1 {p.GetQuarterStart(1, 2024):yyyy-MM-dd} .. {p.GetQuarterEnd(1, 2024):yyyy-MM-dd}; FY2023 Q4 {p.GetQuarterStart(4, 2023):yyyy-MM-dd} .. {p.GetQuarterEnd(4, 2023):yyyy-MM-dd}");
}

// Weeks544: Q1 2024-02-04 .. 2024-05-04; FY2023 Q4 2023-10-29 .. 2024-02-03
// Weeks454: Q1 2024-02-04 .. 2024-05-04; FY2023 Q4 2023-10-29 .. 2024-02-03
// Weeks445: Q1 2024-02-04 .. 2024-05-04; FY2023 Q4 2023-10-29 .. 2024-02-03
```

## Pattern 4 — `WeekOrdinal`

<xref:Bodu.Extensions.WeekOrdinal> (`First`, `Second`, `Third`, `Fourth`, `Fifth`, `Last`) is the "nth weekday of the month" selector consumed by `NthDateOfWeekInMonth` / `GetNthDateOfWeekInMonth`, and reported by the `WeekOrdinalOfMonth` extension property.

```csharp
using Bodu.Extensions;

var may = new DateTime(2024, 5, 1);

foreach (WeekOrdinal ordinal in Enum.GetValues<WeekOrdinal>())
{
    try
    {
        Console.WriteLine($"{ordinal,-6} Monday: {may.NthDateOfWeekInMonth(DayOfWeek.Monday, ordinal):yyyy-MM-dd}");
    }
    catch (ArgumentOutOfRangeException)
    {
        Console.WriteLine($"{ordinal,-6} Monday: (no such day)");
    }
}

// First  Monday: 2024-05-06
// Second Monday: 2024-05-13
// Third  Monday: 2024-05-20
// Fourth Monday: 2024-05-27
// Fifth  Monday: (no such day)
// Last   Monday: 2024-05-27

WeekOrdinal a = new DateTime(2024, 5, 27).WeekOrdinalOfMonth;   // Fourth
WeekOrdinal b = new DateTime(2024, 5, 31).WeekOrdinalOfMonth;   // Fifth
```

`Fifth` throws when the month has only four of that weekday; `Last` always resolves.

## Pattern 5 — a Friday–Saturday weekend with `IWeekendDefinitionProvider`

<xref:Bodu.Extensions.IWeekendDefinitionProvider> has a single member, `bool IsWeekend(DayOfWeek)`. It is consulted by `IsWeekday`, `IsWeekend`, `NextWeekday`, `PreviousWeekday`, and `WorkingDaysOfWeek.ToWeekPattern(provider)` **only when the working week is `WorkingDaysOfWeek.Custom`**; a named preset carries its own weekend and ignores the provider.

```csharp
using Bodu;
using Bodu.Extensions;

/// <summary>Friday–Saturday weekend, as used across much of the Gulf region.</summary>
public sealed class FridaySaturdayWeekend : IWeekendDefinitionProvider
{
    public bool IsWeekend(DayOfWeek dayOfWeek) =>
        dayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
}
```

```csharp
using Bodu;
using Bodu.Extensions;

var gulf = new FridaySaturdayWeekend();
var friday = new DateTime(2024, 5, 17);

bool defaultWeekend = friday.IsWeekend();                                    // false — Saturday–Sunday default
bool gulfWeekend    = friday.IsWeekend(WorkingDaysOfWeek.Custom, gulf);      // true
bool gulfWeekday    = friday.IsWeekday(WorkingDaysOfWeek.Custom, gulf);      // false
DateTime next       = friday.NextWeekday(WorkingDaysOfWeek.Custom, gulf);    // 2024-05-19 (Sunday)
DateTime previous   = friday.PreviousWeekday(WorkingDaysOfWeek.Custom, gulf);// 2024-05-16 (Thursday)

WeekPattern working = gulf.ToWeekPattern();                                  // the complement of the weekend
string symbols      = working.ToString();                                    // "SMTWT__" (Sunday-first; '_' = unselected)
bool isPreset       = working == WeekPattern.SundayToThursday;               // true
WorkingDaysOfWeek named = working.ToWorkingDaysOfWeek();                     // SundayToThursday
WeekPattern viaEnum = WorkingDaysOfWeek.Custom.ToWeekPattern(gulf);          // same pattern

bool presetIgnoresProvider = friday.IsWeekend(WorkingDaysOfWeek.MondayToFriday, gulf);   // false
```

Two failure modes are worth knowing:

- `WorkingDaysOfWeek.Custom` **without** a provider throws `ArgumentOutOfRangeException` (parameter `workingWeek`) from `IsWeekend` / `IsWeekday`, and `ArgumentNullException` from `ToWeekPattern(provider)`.
- The provider is called once per day tested, so keep `IsWeekend` cheap and pure. A provider whose weekend is the whole week makes `NextWeekday` loop until it runs out of `DateTime` range.

<xref:Bodu.Extensions.IWeekendDefinitionProviderExtensions.ToWeekPattern*> is the bridge from a provider to a <xref:Bodu.WeekPattern>: it asks the provider about each of the seven days and returns the working days. Once you have the pattern, the `WeekPattern` overloads of `IsInWorkingWeek`, `IsRestDay`, `NextWeekday`, and `PreviousWeekday` no longer need the provider at all.

## Pattern 6 — bridging `WorkingDaysOfWeek` and `WeekPattern`

<xref:Bodu.WorkingDaysOfWeek> is a closed list of named working weeks (`MondayToFriday`, `MondayToSaturday`, `MondayToThursdayAndSaturday`, `SaturdayToThursday`, `SaturdayToWednesday`, `SundayToFriday`, `SundayToThursday`, `AllDays`, plus the `Custom` marker); <xref:Bodu.WeekPattern> is an open seven-day bitmask. <xref:Bodu.Extensions.WorkingDaysOfWeekExtensions> converts in both directions:

```csharp
using Bodu;
using Bodu.Extensions;

WeekPattern gulfWeek = WorkingDaysOfWeek.SundayToThursday.ToWeekPattern();
string symbols       = gulfWeek.ToString();                                  // "SMTWT__"

WeekPattern fourDay  = WeekPattern.Parse("_MTWT__");                         // Monday–Thursday
WorkingDaysOfWeek back = fourDay.ToWorkingDaysOfWeek();                      // Custom — no preset matches
bool matched         = fourDay.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek named);   // false
bool isWeekdays      = WeekPattern.Weekdays.TryGetWorkingDaysOfWeek(out named);        // true, named == MondayToFriday
bool allDays         = WorkingDaysOfWeek.AllDays.ToWeekPattern() == WeekPattern.AllDays;   // true
```

| Member | Direction | Notes |
|---|---|---|
| `WorkingDaysOfWeek.ToWeekPattern()` | preset → pattern | `Custom` throws `ArgumentException`; use the provider overload. |
| `WorkingDaysOfWeek.ToWeekPattern(IWeekendDefinitionProvider?)` | preset → pattern | The provider is consulted only for `Custom` (and must then be non-`null`). |
| `WeekPattern.ToWorkingDaysOfWeek()` | pattern → preset | Returns `Custom` when the pattern matches no named preset. |
| `WeekPattern.TryGetWorkingDaysOfWeek(out value)` | pattern → preset | `false` (and `value == Custom`) when no preset matches. |
| `IWeekendDefinitionProvider.ToWeekPattern()` | provider → pattern | Complement of the provider's weekend. |

> [!TIP]
> When a working week is a fixed, application-wide policy, resolve it to a `WeekPattern` once and pass the pattern to the extension methods; the pattern overloads are a single bit test per day and never consult a provider.

## Where to go next

- [Date and time extensions](date-extensions.md) — every overload that accepts the types on this page.
- [WeekPattern](week-pattern.md) — parsing, formatting, and composing the seven-day bitmask.
- [Working-day and notable-date calculations](../calendar/index.md) — `Bodu.Globalization.Calendar` adds public holidays and observed-date rules on top of these working-week primitives.
- [`Bodu.Extensions` API reference](xref:Bodu.Extensions) — full signatures for the provider interfaces and the fiscal-week provider.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
