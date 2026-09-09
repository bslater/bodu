---
title: Bodu.Globalization.Recurrence — Getting started
---

# Bodu.Globalization.Recurrence — Getting started

Unfamiliar with terms like *occurrence*, *anchor*, *inclusive boundary*, or *`BYSETPOS`*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Globalization.Recurrence
```

Targets `net8.0`. **Depends on** `Bodu.Core` only — no calendar data, no time-zone database, and no dependency on `Bodu.Globalization.Calendar`.

Status: **Preview** (see the [package matrix](../package-matrix.md)).

Every sample below is a pure function of the instants it is handed: substitute your own "now" and the answers are reproducible in a test.

## Minimal samples

### RFC 5545 recurrence rule — parse, next / previous, enumerate a window

A rule carries no start of its own; the series start (`DTSTART`) is passed to every query.

```csharp
using Bodu.Globalization.Recurrence;

RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE,FR");

var start = new DateTime(2026, 1, 5, 9, 0, 0);                      // Monday 5 Jan 2026, 09:00

DateTime? next     = rule.GetNextOccurrence(start, after: new DateTime(2026, 1, 20));
DateTime? previous = rule.GetPreviousOccurrence(start, before: new DateTime(2026, 1, 20));
// next     = 2026-01-21 09:00 (Wednesday of the second fortnight)
// previous = 2026-01-19 09:00 (Monday)

// Every occurrence inside an inclusive window:
foreach (DateTime occurrence in rule.GetOccurrences(start, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)))
    Console.WriteLine(occurrence);                                  // 2 Feb, 4 Feb, 6 Feb, 16 Feb, 18 Feb, 20 Feb — all 09:00

Console.WriteLine(rule);                                            // FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE,FR
Console.WriteLine(rule.Frequency);                                  // Weekly
Console.WriteLine(rule.ByDay.Count);                                // 3
```

An unbounded rule enumerates to the end of the representable calendar, so bound the open-ended `GetOccurrences(start)` with `Take` or use the windowed overload. `BYSETPOS` turns a weekday list into "the last working day of each month":

```csharp
RecurrenceRule lastWeekday = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1");

foreach (DateTime occurrence in lastWeekday.GetOccurrences(new DateTime(2026, 1, 1)).Take(3))
    Console.WriteLine($"{occurrence:yyyy-MM-dd ddd}");              // 2026-01-30 Fri, 2026-02-27 Fri, 2026-03-31 Tue
```

### Build a rule fluently

<xref:Bodu.Globalization.Recurrence.RecurrenceRuleBuilder> is the alternative to parsing text. A `BYDAY` entry with an ordinal is a <xref:Bodu.Globalization.Recurrence.WeekDayNum>; plain weekdays can be passed as `DayOfWeek`.

```csharp
using Bodu.Globalization.Recurrence;

// The last Friday of the month, six times.
RecurrenceRule lastFriday = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
    .ByDay(new WeekDayNum(-1, DayOfWeek.Friday))
    .WithCount(6)
    .Build();

Console.WriteLine(lastFriday);                                      // FREQ=MONTHLY;COUNT=6;BYDAY=-1FR

foreach (DateTime occurrence in lastFriday.GetOccurrences(new DateTime(2026, 1, 1, 17, 0, 0)))
    Console.WriteLine($"{occurrence:yyyy-MM-dd HH:mm}");            // 2026-01-30 17:00 … 2026-06-26 17:00 (six dates)

// Value equality: the built rule equals its parsed canonical text.
Console.WriteLine(lastFriday.Equals(RecurrenceRule.Parse("FREQ=MONTHLY;COUNT=6;BYDAY=-1FR")));   // True

// Every other week on Monday and Thursday until the end of June. WithUntil clears any WithCount and vice versa.
RecurrenceRule fortnightly = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
    .WithInterval(2)
    .ByDay(DayOfWeek.Monday, DayOfWeek.Thursday)
    .WithUntil(new DateTime(2026, 6, 30))
    .Build();

Console.WriteLine(fortnightly);                                     // FREQ=WEEKLY;INTERVAL=2;UNTIL=20260630T000000;BYDAY=MO,TH
```

### Recurrence set — rules plus `RDATE` / `EXDATE`, and a round trip

A <xref:Bodu.Globalization.Recurrence.RecurrenceSet> owns its start and composes rules, explicit dates, and exception dates into one ascending, duplicate-free stream. Its canonical text is the iCalendar property block, which `Parse` reads back to an equal value.

```csharp
using Bodu.Globalization.Recurrence;

var set = new RecurrenceSet(
    start: new DateTime(2026, 3, 2, 10, 0, 0),                      // Monday
    rules: [RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;COUNT=4")],
    dates: [new DateTime(2026, 3, 12, 10, 0, 0)],                   // an extra Thursday
    exceptionDates: [new DateTime(2026, 3, 16, 10, 0, 0)]);         // one Monday cancelled

foreach (DateTime occurrence in set.GetOccurrences())
    Console.WriteLine($"{occurrence:yyyy-MM-dd ddd}");              // 03-02 Mon, 03-09 Mon, 03-12 Thu, 03-23 Mon

DateTime? next = set.GetNextOccurrence(new DateTime(2026, 3, 10));  // 2026-03-12 10:00 — the RDATE

// Round trip through the property block (CRLF-separated, no trailing line break):
string block = set.ToString();
// DTSTART:20260302T100000
// RRULE:FREQ=WEEKLY;COUNT=4;BYDAY=MO
// RDATE:20260312T100000
// EXDATE:20260316T100000

RecurrenceSet reparsed = RecurrenceSet.Parse(block);
Console.WriteLine(set.Equals(reparsed));                            // True
```

### Cron expression — parse, next / previous, enumerate

A <xref:Bodu.Globalization.Recurrence.CronExpression> needs no anchor. `Parse(string)` infers the five- or six-field layout from the field count; `Parse(string, CronFormat)` insists on one.

```csharp
using Bodu.Globalization.Recurrence;

CronExpression nightly = CronExpression.Parse("0 2 * * MON-FRI");     // 02:00 on weekdays
var now = new DateTime(2026, 1, 9, 14, 30, 0);                        // Friday afternoon

DateTime? next     = nightly.GetNextOccurrence(now);                  // 2026-01-12 02:00 (Monday)
DateTime? previous = nightly.GetPreviousOccurrence(now);              // 2026-01-09 02:00 (this morning)

Console.WriteLine(nightly);                                           // 0 2 * * 1,2,3,4,5  — canonical numeric lists
Console.WriteLine(nightly.Format);                                    // Standard

// Enumerate: cron has no GetOccurrences, so loop on the next occurrence.
DateTime? cursor = new DateTime(2026, 1, 12);
for (var i = 0; i < 3 && cursor is not null; i++)
{
    cursor = nightly.GetNextOccurrence(cursor.Value, inclusive: i == 0);
    Console.WriteLine(cursor);                                        // 12 Jan, 13 Jan, 14 Jan — 02:00
}

// Six-field layout with a leading seconds field, and the macros:
CronExpression halfMinute = CronExpression.Parse("*/30 * 9-17 * * *", CronFormat.WithSeconds);
Console.WriteLine(halfMinute.GetNextOccurrence(now));                 // 2026-01-09 14:30:30

CronExpression daily = CronExpression.Parse("@daily");
Console.WriteLine(daily);                                             // 0 0 * * *
Console.WriteLine(daily.Equals(CronExpression.Parse("0 0 * * *")));  // True — value equality

// DateTimeOffset: evaluated on the wall clock, answered in the argument's offset.
var nowInSydney = new DateTimeOffset(2026, 1, 9, 14, 30, 0, TimeSpan.FromHours(10));
DateTimeOffset? nextInSydney = nightly.GetNextOccurrence(nowInSydney); // 2026-01-12 02:00 +10:00
```

Remember the Vixie day-field rule: when both day fields are restricted an instant matches **either** — `0 0 13 * FRI` fires on every 13th *and* every Friday. See [Cron field semantics](concepts.md#cron-field-semantics).

### Anchored interval — "every 4 hours after the last completed run"

An <xref:Bodu.Globalization.Recurrence.AnchoredInterval> produces `anchor + k·interval` for `k ≥ 1`; the anchor is passed to every query and is never itself an occurrence.

```csharp
using Bodu.Globalization.Recurrence;

AnchoredInterval every4h = AnchoredInterval.Parse("PT4H");           // or: new AnchoredInterval(TimeSpan.FromHours(4))

var lastCompleted = new DateTime(2026, 1, 9, 6, 15, 0);
var now           = new DateTime(2026, 1, 9, 15, 0, 0);

DateTime? next     = every4h.GetNextOccurrence(lastCompleted, now);       // 2026-01-09 18:15
DateTime? previous = every4h.GetPreviousOccurrence(lastCompleted, now);   // 2026-01-09 14:15

// The due-ness recipe — a single comparison, no stored state:
bool isDue = lastCompleted < every4h.GetPreviousOccurrence(anchor: lastCompleted, before: now, inclusive: true);   // True

foreach (DateTime occurrence in every4h.GetOccurrences(lastCompleted, new DateTime(2026, 1, 9), new DateTime(2026, 1, 10)))
    Console.WriteLine($"{occurrence:HH:mm}");                             // 10:15, 14:15, 18:15, 22:15

Console.WriteLine(new AnchoredInterval(TimeSpan.FromDays(14)));           // P2W — canonical RFC 5545 duration text
Console.WriteLine(AnchoredInterval.Parse("P1DT2H30M").Interval);          // 1.02:30:00
```

### `TryParse` with a defect-naming failure message

Every form has the BCL-shaped `TryParse(s, out result)` and an overload that also reports *why* parsing failed, in a message suitable for surfacing to the user verbatim.

```csharp
using Bodu.Globalization.Recurrence;

if (!RecurrenceRule.TryParse("FREQ=WEEKLY;COUNT=3;UNTIL=20261231T000000Z", out RecurrenceRule? _, out string? failure))
    Console.WriteLine(failure);   // The COUNT and UNTIL rule parts cannot both appear in the same rule.

if (!RecurrenceRule.TryParse("FREQ=WEEKLY;BYDAY=XX", out RecurrenceRule? _, out failure))
    Console.WriteLine(failure);   // The recurrence-rule component 'BYDAY=XX' is not valid.

if (!CronExpression.TryParse("0 25 * * *", out CronExpression? _, out failure))
    Console.WriteLine(failure);   // The cron field '25' is not valid.

if (!AnchoredInterval.TryParse("4h", out AnchoredInterval? _, out failure))
    Console.WriteLine(failure);   // An iCalendar duration must begin with the 'P' designator.

if (!RecurrenceSet.TryParse("RRULE:FREQ=DAILY", out RecurrenceSet? _, out failure))
    Console.WriteLine(failure);   // A recurrence set requires a DTSTART property line.
```

`Parse` throws `FormatException` with the same message. Two cases are deliberately *not* format errors: a cron expression using a Quartz extension token (`L`, `W`, `#`, `?`) parses to `NotSupportedException`, and a rule with a sub-daily `FREQ` parses successfully but throws `NotSupportedException` when enumerated.

## Where to go next

- **[Core concepts](concepts.md)** — the vocabulary behind these samples: occurrence vs. due-ness, anchor / `DTSTART`, inclusive boundaries, `WKST`, the `BY*` parts, cron semantics, the duration grammar, offsets.
- **[Recurrence and scheduling guide](../../guides/recurrence/index.md)** — the due-ness recipe for a scheduling host, calendar-aware filtering by composition, bounded searches, and conformance.
- **[Runnable samples](../../samples/recurrence.md)** — the per-form console projects and the `SchedulingHost` adapter that puts all four forms behind one interface.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)** — full type-by-type docs.
