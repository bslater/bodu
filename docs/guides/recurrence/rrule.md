---
title: RFC 5545 recurrence rules
---

# RFC 5545 recurrence rules

<xref:Bodu.Globalization.Recurrence.RecurrenceRule> is an immutable RFC 5545 `RRULE`: a base `FREQ` refined by `INTERVAL`, bounded by `COUNT` or `UNTIL`, and filtered by the `BY*` rule parts. The rule carries **no start of its own** — the series start (`DTSTART`) is passed to every occurrence query — so one parsed rule can be applied to any number of series. It parses from text, renders back to canonical text, compares by value, and answers `GetNextOccurrence` / `GetPreviousOccurrence` over both `DateTime` and `DateTimeOffset`.

This page is the per-form reference. For the "which form" decision and the due-ness recipe every form shares, start at the [recurrence overview](index.md); for the vocabulary (*occurrence*, *inclusive boundary*, *frequency period*) see [Core concepts](../../docs/recurrence/concepts.md).

> [!NOTE]
> Occurrence enumeration supports `DAILY`, `WEEKLY`, `MONTHLY`, and `YEARLY`. A rule with a sub-daily frequency (`SECONDLY`, `MINUTELY`, `HOURLY`) still parses and round-trips, but every occurrence query on it throws <xref:System.NotSupportedException> — the message reads "The recurrence frequency 'Hourly' is not supported; sub-daily frequencies are a planned follow-on." Time-of-day within a daily-or-coarser rule is fully supported through `BYHOUR` / `BYMINUTE` / `BYSECOND`.

## Pattern 1 — parse, inspect, and format

`Parse` accepts the bare rule text or the text with its `RRULE:` property prefix, in any case. The typed parts expose every component; the `BY*` lists are `IReadOnlyList<int>` (or `IReadOnlyList<WeekDayNum>` for `BYDAY`) and are empty when the part is absent.

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule rule = RecurrenceRule.Parse("RRULE:FREQ=WEEKLY;INTERVAL=2;BYDAY=TU;WKST=SU;COUNT=5");

RecurrenceFrequency frequency = rule.Frequency;   // Weekly
int interval                  = rule.Interval;    // 2
int? count                    = rule.Count;       // 5
DateTime? until               = rule.Until;       // null — bounded by COUNT instead
DayOfWeek weekStart           = rule.WeekStart;   // Sunday
WeekDayNum tuesday            = rule.ByDay[0];    // Ordinal = 0, Day = Tuesday

string canonical = rule.ToString();
// "FREQ=WEEKLY;INTERVAL=2;COUNT=5;BYDAY=TU;WKST=SU"
```

`ToString()` renders the **canonical** form, which re-parses to an equal rule: parts in RFC 5545 order (`FREQ`, `INTERVAL`, `COUNT`, `UNTIL`, `BYSECOND`, `BYMINUTE`, `BYHOUR`, `BYDAY`, `BYMONTHDAY`, `BYYEARDAY`, `BYWEEKNO`, `BYMONTH`, `BYSETPOS`, `WKST`), with `INTERVAL=1` and `WKST=MO` omitted because they are the defaults, upper-case tokens, and no `RRULE:` prefix. `byday=fr;freq=monthly;bysetpos=-2` therefore renders as `FREQ=MONTHLY;BYDAY=FR;BYSETPOS=-2`.

The type implements <xref:System.IFormattable>, but only the general specifier is defined: `ToString("G")` and `ToString(null)` return the canonical text and any other specifier throws <xref:System.FormatException> ("The format string 'X' is not supported."). The `IFormatProvider` argument is ignored — rule text is culture-invariant.

`UNTIL` keeps the kind it was parsed with: `UNTIL=20260131T000000Z` yields a <xref:System.DateTimeKind>`.Utc` value and renders with the `Z`; a floating `UNTIL=20260131T000000` (or a date-only `UNTIL=20260131`, which is read as midnight) stays `Unspecified` and renders without it.

## Pattern 2 — validate configuration with `TryParse`

Three `TryParse` shapes exist. The <xref:System.IParsable`1> / <xref:System.ISpanParsable`1> overloads answer a boolean; the third adds an `out string? failureMessage` that names the defect, which is the one to use when the rule text comes from a user or a configuration file:

```csharp
using Bodu.Globalization.Recurrence;

string text = "FREQ=DAILY;COUNT=3;UNTIL=20260101T000000Z";

if (!RecurrenceRule.TryParse(text, out RecurrenceRule? rule, out string? failureMessage))
{
    Console.WriteLine(failureMessage);
    // "The COUNT and UNTIL rule parts cannot both appear in the same rule."
}

// The boolean-only overloads, for callers that do not need the reason:
bool ok1 = RecurrenceRule.TryParse("FREQ=DAILY", out RecurrenceRule? r1);
bool ok2 = RecurrenceRule.TryParse("FREQ=DAILY".AsSpan(), provider: null, out RecurrenceRule? r2);
```

Other messages you will see verbatim: a missing frequency ("A recurrence rule requires a frequency (FREQ) component."), a repeated part ("The recurrence-rule component 'FREQ' appears more than once."), a value out of range ("The recurrence-rule component 'INTERVAL=0' is not valid."), and empty input ("The recurrence-rule text is empty or contains only white space."). `Parse` throws <xref:System.FormatException> with the same message, and <xref:System.ArgumentNullException> for a `null` string.

## Pattern 3 — build a rule fluently

<xref:Bodu.Globalization.Recurrence.RecurrenceRuleBuilder> is the code-first alternative to text. It is constructed with the frequency; every other method returns the same builder so calls chain, and `Build()` produces the immutable rule. The builder can be reused after `Build()`, and each `By*` call **replaces** the values previously supplied for that part rather than appending.

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule lastFriday = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
    .WithInterval(1)
    .WithCount(12)
    .ByDay(new WeekDayNum(-1, DayOfWeek.Friday))       // "-1FR": the last Friday of each month
    .Build();
// FREQ=MONTHLY;COUNT=12;BYDAY=-1FR

RecurrenceRule weekdays = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
    .ByDay(DayOfWeek.Monday, DayOfWeek.Friday)         // plain DayOfWeek values: every Monday and Friday
    .WithWeekStart(DayOfWeek.Sunday)
    .WithUntil(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc))
    .Build();
// FREQ=WEEKLY;UNTIL=20261231T000000Z;BYDAY=MO,FR;WKST=SU

bool same = lastFriday.Equals(RecurrenceRule.Parse("FREQ=MONTHLY;COUNT=12;BYDAY=-1FR"));   // true
```

| Method | Rule part | Accepted values |
|---|---|---|
| `WithInterval(int)` | `INTERVAL` | ≥ 1 |
| `WithCount(int)` | `COUNT` | ≥ 1; clears any `UNTIL` |
| `WithUntil(DateTime)` | `UNTIL` | any instant; clears any `COUNT` |
| `WithWeekStart(DayOfWeek)` | `WKST` | any day; default Monday |
| `BySecond(params int[])` | `BYSECOND` | 0–60 |
| `ByMinute(params int[])` | `BYMINUTE` | 0–59 |
| `ByHour(params int[])` | `BYHOUR` | 0–23 |
| `ByDay(params WeekDayNum[])` | `BYDAY` | ordinal `±1…±53` or `0` for every occurrence |
| `ByDay(params DayOfWeek[])` | `BYDAY` | convenience for ordinal `0` |
| `ByMonthDay(params int[])` | `BYMONTHDAY` | ±1…±31, never 0 |
| `ByYearDay(params int[])` | `BYYEARDAY` | ±1…±366, never 0 |
| `ByWeekNo(params int[])` | `BYWEEKNO` | ±1…±53, never 0 |
| `ByMonth(params int[])` | `BYMONTH` | 1–12 |
| `BySetPos(params int[])` | `BYSETPOS` | ±1…±366, never 0 |

`WithCount` and `WithUntil` are mutually exclusive: supplying one clears the other, matching the RFC rule that `COUNT` and `UNTIL` cannot both appear. Out-of-range values throw <xref:System.ArgumentOutOfRangeException> at the call, not at `Build()`.

<xref:Bodu.Globalization.Recurrence.WeekDayNum> is a `readonly record struct (int Ordinal, DayOfWeek Day)`. `Ordinal` `0` means every occurrence of the day (`IsEveryOccurrence` is `true`); a positive ordinal counts from the start of the frequency period and a negative one from its end, so `2TU` is the second Tuesday of the month in a `MONTHLY` rule and the second Tuesday of the *year* in a `YEARLY` rule. A `+` sign is accepted on input (`+1MO`) and dropped on output (`1MO`).

## Pattern 4 — enumerate occurrences

`GetOccurrences(start)` yields the series in ascending order, each value preserving the `Kind` of `start`. The sequence is bounded when the rule declares `COUNT` or `UNTIL`; otherwise it continues to the end of the representable calendar, so bound it with `Take` or use the windowed overload. Membership depends only on the rule and the start — never on a window — so the two overloads always agree.

```csharp
using Bodu.Globalization.Recurrence;

var start = new DateTime(2026, 1, 5, 9, 0, 0);          // Monday 09:00

RecurrenceRule bounded = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
DateTime[] three = bounded.GetOccurrences(start).ToArray();
// 2026-01-05 09:00, 2026-01-06 09:00, 2026-01-07 09:00

RecurrenceRule mondays = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");
DateTime[] firstFive = mondays.GetOccurrences(start).Take(5).ToArray();   // unbounded: Take is required

DateTime[] march = mondays
    .GetOccurrences(start, from: new DateTime(2026, 3, 1), to: new DateTime(2026, 3, 31))
    .ToArray();
// 2026-03-02, 03-09, 03-16, 03-23, 03-30 — the inclusive [from, to] window
```

The start instant is emitted **only when it satisfies the rule**. `FREQ=WEEKLY;BYDAY=WE` anchored on Monday 5 January 2026 begins on Wednesday 7 January; nothing is added implicitly. Time-of-day comes from the start unless a `BY*` time part overrides it: `FREQ=DAILY;BYHOUR=9,17;BYMINUTE=30` from `2026-01-05 09:00` yields 09:30 and 17:30 that day, and `FREQ=DAILY;BYHOUR=2` from `2026-01-01 09:30` yields its first occurrence at `2026-01-02 02:30`, because 02:30 on 1 January precedes the start.

## Pattern 5 — point queries with inclusive flags

`GetNextOccurrence(start, after, inclusive)` and `GetPreviousOccurrence(start, before, inclusive)` answer a single instant, or `null`. With `inclusive: false` (the default) the boundary instant is excluded; with `inclusive: true` an occurrence exactly equal to the boundary is returned.

```csharp
using Bodu.Globalization.Recurrence;

var start = new DateTime(2026, 1, 5, 9, 0, 0);
RecurrenceRule mondays = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");
var at = new DateTime(2026, 1, 12, 9, 0, 0);              // exactly an occurrence

DateTime? nextExclusive = mondays.GetNextOccurrence(start, at);                      // 2026-01-19 09:00
DateTime? nextInclusive = mondays.GetNextOccurrence(start, at, inclusive: true);     // 2026-01-12 09:00
DateTime? prevExclusive = mondays.GetPreviousOccurrence(start, at);                  // 2026-01-05 09:00
DateTime? prevInclusive = mondays.GetPreviousOccurrence(start, at, inclusive: true); // 2026-01-12 09:00

DateTime? nothing = mondays.GetPreviousOccurrence(start, new DateTime(2025, 1, 1));  // null — before the series
DateTime? never   = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30")
    .GetNextOccurrence(start, start);                                                // null — 30 February never exists
```

Both searches are bounded by the end of the representable calendar (year 9999), so a rule that can never match answers `null` rather than scanning forever.

The `DateTimeOffset` overloads expand the rule on the **wall-clock time of the start** and reattach the start's offset to every result; the library performs no offset conversion and never consults the machine time zone. A start of `2026-01-05 09:00 +10:00` with `FREQ=DAILY` answers `2026-01-06 09:00 +10:00` for any `after` on 5 January, whatever offset `after` carries:

```csharp
using Bodu.Globalization.Recurrence;

var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(10));
var afterUtc = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);

DateTimeOffset? next = RecurrenceRule.Parse("FREQ=DAILY").GetNextOccurrence(start, afterUtc);
// 2026-01-06 09:00 +10:00
```

Daylight-saving transitions are the caller's concern — see [Hosting schedules](scheduling-host.md) for converting at the host boundary.

## Pattern 6 — last working day of the month

`BYDAY` lists the weekdays; `BYSETPOS=-1` selects the last candidate in each monthly period:

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule lastWorkingDay = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1");

DateTime[] firstHalf2026 = lastWorkingDay.GetOccurrences(new DateTime(2026, 1, 1)).Take(6).ToArray();
// 2026-01-30 Fri, 2026-02-27 Fri, 2026-03-31 Tue, 2026-04-30 Thu, 2026-05-29 Fri, 2026-06-30 Tue
```

"Working day" here means Monday–Friday only. To also skip public holidays, filter the stream with `IsNonWorkingDay` from `Bodu.Globalization.Calendar` — see [Hosting schedules](scheduling-host.md#pattern-5--skip-non-working-days-with-the-calendar-package).

## Pattern 7 — every second Tuesday

`INTERVAL` multiplies the frequency period. Anchored on a Tuesday, `FREQ=WEEKLY;INTERVAL=2;BYDAY=TU` is every fourteenth day:

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule fortnightly = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU");

DateTime[] tuesdays = fortnightly.GetOccurrences(new DateTime(2026, 1, 6)).Take(5).ToArray();
// 2026-01-06, 2026-01-20, 2026-02-03, 2026-02-17, 2026-03-03
```

Which *weeks* count as "every second" depends on `WKST` — see [How `WKST` changes the answer](#how-wkst-changes-the-answer) below.

## Pattern 8 — the second-to-last Friday

Negative `BYSETPOS` values index from the end of the candidate set of each period:

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule penultimateFriday = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=FR;BYSETPOS=-2");

DateTime[] fridays = penultimateFriday.GetOccurrences(new DateTime(2026, 1, 1)).Take(4).ToArray();
// 2026-01-23, 2026-02-20, 2026-03-20, 2026-04-17
```

`FREQ=MONTHLY;BYDAY=-2FR` (an ordinal on the `BYDAY` entry) produces the same dates; `BYSETPOS` is the form to reach for when the candidate set mixes several weekdays.

## Pattern 9 — a `COUNT`-bounded series

`COUNT` caps the number of occurrences *after* deduplication and `BYSETPOS` selection, so it counts emitted instants, not candidates. A bounded rule's enumeration terminates on its own:

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule twelveSessions = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
    .ByDay(DayOfWeek.Wednesday)
    .WithCount(12)
    .Build();

var start = new DateTime(2026, 2, 4, 18, 0, 0);
int total       = twelveSessions.GetOccurrences(start).Count();                       // 12
DateTime? last  = twelveSessions.GetPreviousOccurrence(start, DateTime.MaxValue);      // the twelfth Wednesday
DateTime? after = twelveSessions.GetNextOccurrence(start, last!.Value);                // null — the series is exhausted
```

## Pattern 10 — catching up after downtime

Because the library stores no last-run state, a catch-up is a windowed enumeration from the last recorded run to the resume instant. The window is inclusive at both ends, so exclude the last run itself if it was completed:

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule nightly = RecurrenceRule.Parse("FREQ=DAILY;BYHOUR=2;BYMINUTE=0");
var seriesStart = new DateTime(2026, 1, 1, 2, 0, 0);

var lastRun   = new DateTime(2026, 3, 9, 18, 0, 0);     // persisted before the host stopped
var resumedAt = new DateTime(2026, 3, 12, 14, 32, 0);   // supplied by the host on restart

DateTime[] missed = nightly
    .GetOccurrences(seriesStart, from: lastRun, to: resumedAt)
    .Where(occurrence => occurrence > lastRun)
    .ToArray();
// 2026-03-10 02:00, 2026-03-11 02:00, 2026-03-12 02:00

// Or coalesce the backlog into a single "is anything due?" decision:
bool isDue = lastRun < nightly.GetPreviousOccurrence(seriesStart, resumedAt, inclusive: true);   // true
```

The [Hosting schedules](scheduling-host.md) guide turns this into a reproducible loop over a `TimeProvider`.

## Where implementations disagree

Recurrence libraries diverge on a handful of `BY*` interactions. Each statement below is what *this* library does, with the output the sample produced.

**Invalid generated dates are skipped, never clamped.** `FREQ=MONTHLY` from 31 January yields 31 March, 31 May, 31 July, 31 August — months without a 31st are omitted rather than rolled back to the 30th. RFC 5545 requires this; it is also the single most common false bug report against recurrence libraries.

```csharp
using Bodu.Globalization.Recurrence;

DateTime[] thirtyFirsts = RecurrenceRule.Parse("FREQ=MONTHLY")
    .GetOccurrences(new DateTime(2026, 1, 31)).Take(5).ToArray();
// 2026-01-31, 2026-03-31, 2026-05-31, 2026-07-31, 2026-08-31
```

**The occurrence set is a set.** Two `BY` values resolving to the same date contribute one occurrence, and deduplication happens *before* `BYSETPOS` indexes the candidates and before `COUNT` counts them. `BYMONTHDAY=1,-31` yields a single 1 January in 2026, not two:

```csharp
using Bodu.Globalization.Recurrence;

DateTime[] firsts = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1,-31")
    .GetOccurrences(new DateTime(2026, 1, 1)).Take(3).ToArray();
// 2026-01-01, 2026-02-01, 2026-03-01
```

**`BYSETPOS` indexes the whole frequency period**, including candidates that precede the series start; those are dropped only afterwards. `FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1` anchored on Wednesday 7 January 2026 selects the *Monday* of each week — the first candidate of the period — so its first occurrence is Monday 12 January, not the Wednesday it was anchored on:

```csharp
using Bodu.Globalization.Recurrence;

DateTime[] firstWeekday = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1")
    .GetOccurrences(new DateTime(2026, 1, 7)).Take(3).ToArray();
// 2026-01-12 Mon, 2026-01-19 Mon, 2026-01-26 Mon
```

**A `BY` filter never re-anchors an interval.** `FREQ=DAILY;INTERVAL=14;BYMONTH=10,12` counts every fourteenth day from the start unconditionally and drops the ones outside October and December; the cadence is not restarted at 1 October:

```csharp
using Bodu.Globalization.Recurrence;

DateTime[] fortnightlyInQ4 = RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=14;BYMONTH=10,12")
    .GetOccurrences(new DateTime(2026, 9, 1)).Take(4).ToArray();
// 2026-10-13, 2026-10-27, 2026-12-08, 2026-12-22
```

**Ordinals in `BYDAY` count within the frequency period.** `FREQ=MONTHLY;BYDAY=2TU` is the second Tuesday of each month (13 January, 10 February, 10 March 2026); `FREQ=YEARLY;BYDAY=20MO` is the twentieth Monday of the year (18 May 2026). `FREQ=YEARLY;BYMONTH=11;BYDAY=4TH` narrows the yearly period to November first, giving the fourth Thursday of November (26 November 2026 — US Thanksgiving). `BYYEARDAY=1,100,-1` selects 1 January, 10 April, and 31 December.

## How `WKST` changes the answer

`WKST` (default Monday) reparameterises **week numbering**, not just weekly intervals: it decides which dates `BYWEEKNO` resolves to *and* which weeks an `INTERVAL` counts. Numbered weeks straddle the calendar year, so week 1 may begin in the preceding December.

```csharp
using Bodu.Globalization.Recurrence;

// BYWEEKNO=1 with Monday weeks: week 1 of 2026 begins Monday 29 December 2025 — before the
// series start — so the first hit is the Monday of week 1 in 2027.
DateTime[] isoWeekOne = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO")
    .GetOccurrences(new DateTime(2026, 1, 1)).Take(2).ToArray();
// 2027-01-04, 2028-01-03

// With Sunday weeks, week 1 of 2026 begins Sunday 4 January, so its Monday is 5 January 2026.
DateTime[] sundayWeekOne = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO;WKST=SU")
    .GetOccurrences(new DateTime(2026, 1, 1)).Take(2).ToArray();
// 2026-01-05, 2027-01-04

// The same fortnightly rule from Sunday 4 January 2026 pairs different Tuesdays with each Sunday:
DateTime[] mondayWeeks = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU")
    .GetOccurrences(new DateTime(2026, 1, 4)).Take(4).ToArray();
// 2026-01-04 Sun, 2026-01-13 Tue, 2026-01-18 Sun, 2026-01-27 Tue   (Sunday closes a Mon–Sun week)

DateTime[] sundayWeeks = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU;WKST=SU")
    .GetOccurrences(new DateTime(2026, 1, 4)).Take(4).ToArray();
// 2026-01-04 Sun, 2026-01-06 Tue, 2026-01-18 Sun, 2026-01-20 Tue   (Sunday opens a Sun–Sat week)
```

The builder's `WithWeekStart` sets the same part.

## Value equality

`RecurrenceRule` implements <xref:System.IEquatable`1>: two rules are equal when every component — frequency, interval, count, until, week start, and every `BY*` list *in source order* — is equal. `Parse(rule.ToString())` is always equal to `rule`, so persisting the canonical text and comparing on reload is a reliable change detector. Note that `BYDAY=MO,FR` and `BYDAY=FR,MO` produce identical occurrences but are **not** equal values; canonicalise the text with `ToString()` before comparing if source order should not matter. `GetHashCode` is consistent with `Equals`.

## API summary

| Member | Purpose |
|---|---|
| `Parse(string)` / `Parse(string, IFormatProvider?)` / `Parse(ReadOnlySpan<char>, IFormatProvider?)` | Parse; throws `FormatException` on a defect. Accepts an optional `RRULE:` prefix. |
| `TryParse(string?, out rule)` / `TryParse(string?, IFormatProvider?, out rule)` / `TryParse(ReadOnlySpan<char>, IFormatProvider?, out rule)` | Boolean parse. |
| `TryParse(string?, out rule, out string? failureMessage)` | Boolean parse that names the defect. |
| `Frequency`, `Interval`, `Count`, `Until`, `WeekStart` | The scalar parts. |
| `BySecond`, `ByMinute`, `ByHour`, `ByDay`, `ByMonthDay`, `ByYearDay`, `ByWeekNo`, `ByMonth`, `BySetPos` | The list parts, empty when absent. |
| `GetOccurrences(start)` / `GetOccurrences(start, from, to)` | Lazy ascending enumeration; `DateTime` and `DateTimeOffset` overloads. |
| `GetNextOccurrence(start, after, inclusive = false)` | First occurrence after (or at) `after`, or `null`. |
| `GetPreviousOccurrence(start, before, inclusive = false)` | Last occurrence before (or at) `before`, or `null`. |
| `ToString()` / `ToString(string?)` / `ToString(string?, IFormatProvider?)` | Canonical RFC 5545 text; only the `"G"` specifier is defined. |
| `Equals` / `GetHashCode` | Component-wise value equality. |

## Where to go next

- **[Recurrence sets](recurrence-sets.md)** — compose rules with `RDATE` additions and `EXDATE` exclusions, and round-trip the iCalendar property block.
- **[Cron expressions](cron.md)** — the operational alternative for "02:00 every weekday".
- **[Hosting schedules](scheduling-host.md)** — a reproducible catch-up loop over `TimeProvider`, and filtering occurrences with calendar working days.
- **[Runnable samples](../../samples/recurrence.md)** — `Bodu.Globalization.Recurrence.Samples.RecurrenceRules` walks every pattern above.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)** — `RecurrenceRule`, `RecurrenceRuleBuilder`, `WeekDayNum`, `RecurrenceFrequency`.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
