---
title: Cron expressions
---

# Cron expressions

<xref:Bodu.Globalization.Recurrence.CronExpression> is a parsed Vixie-style cron expression. It is a **predicate over instants** — an instant matches when its minute, hour, day-of-month, month, and day-of-week (and second, in the six-field layout) are each members of the corresponding field set — so, unlike a recurrence rule, it has no series start: `GetNextOccurrence(after)` and `GetPreviousOccurrence(before)` take only the boundary instant. It parses from text, renders back to canonical text, compares by value, and never reads the wall clock.

For the "which form" decision and the shared due-ness recipe, start at the [recurrence overview](index.md).

## Pattern 1 — parse and query

`Parse(string)` infers the layout from the field count: five fields is <xref:Bodu.Globalization.Recurrence.CronFormat>`.Standard` (minute, hour, day-of-month, month, day-of-week) and six is `CronFormat.WithSeconds` (a leading seconds field). The `Format` property reports which one was chosen.

```csharp
using Bodu.Globalization.Recurrence;

CronExpression nightly = CronExpression.Parse("0 2 * * *");           // Standard: 02:00 every day
CronExpression quarterHourly = CronExpression.Parse("0 */15 * * * *"); // WithSeconds (six fields)

CronFormat layout = quarterHourly.Format;                             // WithSeconds

var now = new DateTime(2026, 3, 10, 14, 32, 0);
DateTime? next = nightly.GetNextOccurrence(now);                      // 2026-03-11 02:00
DateTime? previous = nightly.GetPreviousOccurrence(now);              // 2026-03-10 02:00
```

Answers preserve the `Kind` of the argument and carry zero seconds in the five-field layout (an `after` of `01:59:30` still answers `02:00:00`). Both boundary flags work as on every other form: `inclusive: true` lets an occurrence exactly equal to the boundary be returned.

```csharp
using Bodu.Globalization.Recurrence;

CronExpression nightly = CronExpression.Parse("0 2 * * *");
var exactly = new DateTime(2026, 3, 10, 2, 0, 0);

DateTime? excl = nightly.GetNextOccurrence(exactly);                    // 2026-03-11 02:00
DateTime? incl = nightly.GetNextOccurrence(exactly, inclusive: true);   // 2026-03-10 02:00
```

Pass the layout explicitly with `Parse(string, CronFormat)` when the field count must not be inferred — a six-field string parsed as `Standard`, or a five-field string parsed as `WithSeconds`, fails with "A cron expression must contain the number of fields required by the specified format."

## Pattern 2 — validate user input with `TryParse`

Five `TryParse` overloads cover the same choices without exceptions. The two that add `out string? failureMessage` name the defect, which is the right shape for configuration validation:

```csharp
using Bodu.Globalization.Recurrence;

string[] candidates = ["0 2 * *", "60 * * * *", "0 0 ? * *", "0 2 * * FOO"];

foreach (string text in candidates)
{
    if (!CronExpression.TryParse(text, out CronExpression? expression, out string? failureMessage))
        Console.WriteLine($"'{text}': {failureMessage}");
}
// '0 2 * *':     A cron expression must contain the number of fields required by the specified format.
// '60 * * * *':  The cron field '60' is not valid.
// '0 0 ? * *':   The cron token '?' is not supported; the Quartz L, W, and # extensions are a planned follow-on.
// '0 2 * * FOO': The cron field 'FOO' is not valid.

// Layout-pinned variant, and the boolean-only overloads:
bool six = CronExpression.TryParse("0 */5 * * * *", CronFormat.WithSeconds, out CronExpression? withSeconds, out string? why);
bool ok  = CronExpression.TryParse("@daily", out CronExpression? daily);
bool ok2 = CronExpression.TryParse("@daily", CronFormat.Standard, out CronExpression? daily2);
```

`Parse` raises the same messages as <xref:System.FormatException>, or <xref:System.NotSupportedException> for a Quartz token (`?`, `L`, `W`, `#`). A seven-field Quartz string with a trailing year is rejected on field count.

## Field syntax

Every field accepts `*`, a single value, a range `a-b`, a step `*/n` or `a-b/n`, and comma-separated lists of those. Months take `JAN`–`DEC` and weekdays `SUN`–`SAT`, case-insensitively; weekday `7` is an alias for Sunday (`0`).

| Field | Range | Notes |
|---|---|---|
| second *(six-field only)* | 0–59 | |
| minute | 0–59 | |
| hour | 0–23 | |
| day-of-month | 1–31 | |
| month | 1–12 or `JAN`–`DEC` | |
| day-of-week | 0–7 or `SUN`–`SAT` | `0` and `7` are both Sunday |

Two behaviours are worth stating because libraries disagree on them:

- **A step wider than its range selects the range start** rather than being rejected: `*/60` and `*/90` in the minute field both mean minute `0` (cronie warns about this; some libraries throw). A step of `0` is rejected ("The cron field '1-5/0' is not valid.").
- **Day-of-month and day-of-week combine by union only when both are restricted.** Following Vixie cron, an instant matches when it satisfies *either* field if both are restricted, and *both* fields otherwise — and "restricted" is decided by the field's leading character, so `*/2` (leading `*`) is unrestricted while `1-31/2` is restricted even though the two select the same days:

```csharp
using Bodu.Globalization.Recurrence;

var now = new DateTime(2026, 3, 10, 14, 32, 0);

// Both restricted → union: the 13th of any month OR any Friday.
DateTime? unionNext = CronExpression.Parse("0 0 13 * FRI").GetNextOccurrence(now);   // 2026-03-13 (a Friday)

// Day-of-month starts with '*' → not restricted → intersection: odd days that are Mondays.
DateTime? starStep = CronExpression.Parse("0 0 */2 * MON").GetNextOccurrence(now);    // 2026-03-23 Mon
// Same days written as a restricted range → union: odd days OR Mondays.
DateTime? rangeStep = CronExpression.Parse("0 0 1-31/2 * MON").GetNextOccurrence(now); // 2026-03-11 Wed
```

> [!WARNING]
> `ToString()` renders every field as `*` or an explicit numeric list, so `0 0 */2 * MON` renders as `0 0 1,3,5,…,31 * 1` — and that text re-parses with a *restricted* day-of-month, which flips the combination from intersection to union (23 March above becomes 11 March). `Equals` compares field sets only, so the two expressions still compare equal. Persist the **original** text for a `*`-leading day step that is combined with a restricted day-of-week (or the reverse); the canonical form is safe for every other expression.

## The `@` macros

Exactly seven macros are recognised, each expanding to a five-field expression. `@reboot` and Go-style `@every` are **not** supported and fail parsing with "The cron macro '@reboot' is not recognized, or a macro was supplied where the six-field layout was required."

| Macro | Equivalent | Canonical `ToString()` |
|---|---|---|
| `@yearly`, `@annually` | 00:00 on 1 January | `0 0 1 1 *` |
| `@monthly` | 00:00 on the 1st | `0 0 1 * *` |
| `@weekly` | 00:00 every Sunday | `0 0 * * 0` |
| `@daily`, `@midnight` | 00:00 every day | `0 0 * * *` |
| `@hourly` | minute 0 of every hour | `0 * * * *` |

A macro always yields `CronFormat.Standard`; parsing one with `CronFormat.WithSeconds` fails.

## Pattern 3 — business-hours polling

`*/15 9-17 * * MON-FRI` fires every quarter hour from 09:00 through 17:45 on weekdays. Because the hour field is a *set*, the last slot of the day is 17:45 and the next after that is 09:00 on the next weekday:

```csharp
using Bodu.Globalization.Recurrence;

CronExpression polling = CronExpression.Parse("*/15 9-17 * * MON-FRI");

DateTime? next = polling.GetNextOccurrence(new DateTime(2026, 3, 13, 17, 50, 0));       // Fri 17:50 → 2026-03-16 Mon 09:00
DateTime? last = polling.GetPreviousOccurrence(new DateTime(2026, 3, 14, 10, 0, 0));    // Sat 10:00 → 2026-03-13 Fri 17:45

string canonical = polling.ToString();
// "0,15,30,45 9,10,11,12,13,14,15,16,17 * * 1,2,3,4,5"
```

To stop at 17:00 rather than 17:45, list the fields explicitly: `*/15 9-16 * * MON-FRI` plus `0 17 * * MON-FRI` as a second expression, or a `RecurrenceRule` with `BYHOUR`/`BYMINUTE`.

## Pattern 4 — last day of the month via next-then-previous

Vixie cron has no "last day" token (`L` is a Quartz extension the parser rejects). Derive it from the first of next month:

```csharp
using Bodu.Globalization.Recurrence;

CronExpression firstOfMonth = CronExpression.Parse("0 0 1 * *");
var now = new DateTime(2026, 3, 10, 14, 32, 0);

DateTime nextFirst = firstOfMonth.GetNextOccurrence(now)!.Value;      // 2026-04-01 00:00
DateTime lastDay   = nextFirst.AddDays(-1);                           // 2026-03-31 00:00

// For a job "at 23:00 on the last day of the month":
DateTime lastDayAtEleven = lastDay.AddHours(23);
```

Equivalently, `GetPreviousOccurrence` on `0 0 1 * *` from a point in the *next* month gives the same boundary. For a calendar-aligned "last Friday" or "last working day", use a `RecurrenceRule` with `BYSETPOS=-1` — see [RFC 5545 recurrence rules](rrule.md#pattern-6--last-working-day-of-the-month).

## The search horizon

Every search scans **twelve years** in the requested direction and answers `null` past it. Twelve covers the largest gap between two consecutive occurrences of any satisfiable expression — a 29 February schedule crossing a non-leap century year (2096 → 2104, eight years) — with margin, while still bounding the search for an expression that can never match:

```csharp
using Bodu.Globalization.Recurrence;

DateTime? leapDay = CronExpression.Parse("0 0 29 2 *").GetNextOccurrence(new DateTime(2096, 3, 1));   // 2104-02-29
DateTime? never   = CronExpression.Parse("0 0 30 2 *").GetNextOccurrence(new DateTime(2026, 3, 10)); // null
DateTime? neverBack = CronExpression.Parse("0 0 30 2 *").GetPreviousOccurrence(new DateTime(2026, 3, 10)); // null
```

`null` therefore means "no occurrence within twelve years", which for any real schedule means "never".

## `DateTimeOffset` handling

The `DateTimeOffset` overloads interpret the argument's wall-clock time in its own offset and return an occurrence carrying that offset — no conversion, no time-zone lookup. `0 9 * * *` queried at `2026-03-10 14:32 +10:00` answers `2026-03-11 09:00 +10:00`; queried at `14:32 +00:00` it answers `09:00 +00:00`. A host that wants "09:00 local" across a daylight-saving change re-derives the offset on every evaluation; see [Hosting schedules](scheduling-host.md#pattern-4--daylight-saving-and-time-zones-at-the-boundary).

```csharp
using Bodu.Globalization.Recurrence;

CronExpression nine = CronExpression.Parse("0 9 * * *");
var sydney = new DateTimeOffset(2026, 3, 10, 14, 32, 0, TimeSpan.FromHours(10));

DateTimeOffset? next = nine.GetNextOccurrence(sydney);   // 2026-03-11 09:00 +10:00
```

## Canonical text and equality

`ToString()` renders a field as `*` when every value is selected and otherwise as an ascending comma-separated numeric list — names, ranges, and steps are expanded, and `7` becomes `0`. `Equals` compares the six field sets and the layout, so `0 9-17 * * MON-FRI` equals `0 9,10,11,12,13,14,15,16,17 * * 1-5`. Only the `"G"` / `null` format specifier is defined on `ToString(string?, IFormatProvider?)`; anything else throws <xref:System.FormatException>. See the warning under [Field syntax](#field-syntax) for the one case where the canonical text is not a faithful round-trip.

## API summary

| Member | Purpose |
|---|---|
| `Parse(string)` / `Parse(string, IFormatProvider?)` | Parse, inferring the layout from the field count. |
| `Parse(string, CronFormat)` | Parse with a required layout. |
| `TryParse(string?, out result)` / `TryParse(string?, IFormatProvider?, out result)` / `TryParse(string?, CronFormat, out result)` | Boolean parse. |
| `TryParse(string?, out result, out failureMessage)` / `TryParse(string?, CronFormat, out result, out failureMessage)` | Boolean parse that names the defect. |
| `Format` | The layout the expression was parsed as. |
| `GetNextOccurrence(after, inclusive = false)` | Next matching instant within twelve years, or `null`; `DateTime` and `DateTimeOffset`. |
| `GetPreviousOccurrence(before, inclusive = false)` | Previous matching instant within twelve years, or `null`. |
| `ToString()` / `ToString(string?, IFormatProvider?)` | Canonical numeric-list text. |
| `Equals` / `GetHashCode` | Field-set equality. |

## Where to go next

- **[RFC 5545 recurrence rules](rrule.md)** — the richer grammar for `BYSETPOS`, `COUNT`, and iCalendar interoperability.
- **[Anchored intervals](anchored-intervals.md)** — exact elapsed-time schedules that are immune to calendar and daylight-saving effects.
- **[Hosting schedules](scheduling-host.md)** — one adapter over every form, catch-up, persistence, and time zones.
- **[Runnable samples](../../samples/recurrence.md)** — `Bodu.Globalization.Recurrence.Samples.CronExpressions`.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)** — `CronExpression`, `CronFormat`.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
