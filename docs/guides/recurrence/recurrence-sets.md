---
title: Recurrence sets
---

# Recurrence sets

<xref:Bodu.Globalization.Recurrence.RecurrenceSet> composes a schedule the way an iCalendar `VEVENT` does: one or more <xref:Bodu.Globalization.Recurrence.RecurrenceRule> streams anchored at a common `DTSTART`, plus explicit recurrence dates (`RDATE`), minus exception dates (`EXDATE`). The occurrence set is the ascending, duplicate-free **union** of every rule expansion and every explicit date, **less** any instant matching an exception date. Unlike a bare rule, a set carries its own start, so its queries take only the boundary instant.

For the "which form" decision and the shared due-ness recipe, start at the [recurrence overview](index.md).

## Pattern 1 — build a set in code

The constructor takes the start, the rules, and optional `dates` / `exceptionDates` sequences. At least one rule or one explicit date is required; an empty set throws <xref:System.ArgumentException> ("A recurrence set requires at least one rule or explicit recurrence date."). The date lists are sorted ascending on construction; the rules keep the order supplied.

```csharp
using Bodu.Globalization.Recurrence;

var start = new DateTime(2026, 1, 5, 9, 0, 0);   // Monday 09:00

var standUp = new RecurrenceSet(
    start,
    rules: new[] { RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;COUNT=8") },
    dates: new[] { new DateTime(2026, 1, 14, 9, 0, 0) },                                        // an extra Wednesday
    exceptionDates: new[] { new DateTime(2026, 1, 26, 9, 0, 0), new DateTime(2026, 2, 9, 9, 0, 0) });

DateTime startAgain                   = standUp.Start;            // 2026-01-05 09:00
IReadOnlyList<RecurrenceRule> rules   = standUp.Rules;            // one rule
IReadOnlyList<DateTime> extras        = standUp.Dates;            // [2026-01-14 09:00]
IReadOnlyList<DateTime> skipped       = standUp.ExceptionDates;   // [2026-01-26 09:00, 2026-02-09 09:00]
```

The start is emitted only when a rule or an explicit date produces it — it is not added implicitly. A set whose only content is explicit dates is valid (pass an empty rule sequence).

## Composition semantics

1. Each rule is expanded from `Start` exactly as `rule.GetOccurrences(Start)` would.
2. The expansions and `Dates` are merged in ascending order; an instant produced more than once contributes **one** occurrence.
3. Any instant equal to an entry in `ExceptionDates` is removed. Matching is exact — an exception must carry the same time of day as the occurrence it removes.
4. `COUNT` on a rule bounds that rule's own expansion; it is not reduced by exceptions, so `COUNT=8` with two `EXDATE` hits yields six rule occurrences, not eight.

```csharp
using Bodu.Globalization.Recurrence;

var standUp = RecurrenceSet.Parse(
    "DTSTART:20260105T090000\r\n" +
    "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=8\r\n" +
    "RDATE:20260114T090000\r\n" +
    "EXDATE:20260126T090000,20260209T090000");

DateTime[] occurrences = standUp.GetOccurrences().ToArray();
// 2026-01-05 Mon, 01-12 Mon, 01-14 Wed (RDATE), 01-19 Mon, 02-02 Mon, 02-16 Mon, 02-23 Mon
//   — 01-26 and 02-09 removed by EXDATE; 7 occurrences from COUNT=8 plus one RDATE minus two exceptions

var overlapping = new RecurrenceSet(
    new DateTime(2026, 1, 5),
    new[] { RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;COUNT=2"), RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,WE;COUNT=3") });

DateTime[] deduplicated = overlapping.GetOccurrences().ToArray();
// 2026-01-05, 2026-01-07, 2026-01-12 — Monday 5 January is produced by both rules but emitted once
```

`GetOccurrences()` is unbounded when any rule is unbounded; use the windowed `GetOccurrences(from, to)` or `Take`. A set whose rule uses a sub-daily frequency throws <xref:System.NotSupportedException> on enumeration, as the rule itself would.

## The property-block text format

`ToString()` renders the canonical iCalendar property block: a `DTSTART` line, one `RRULE` line per rule in the order supplied, then a single `RDATE` line and a single `EXDATE` line (each a comma-separated ascending list) when present. Lines are separated by CRLF per RFC 5545 with no trailing line break, and date-times use the iCalendar `yyyyMMddTHHmmss` form with a trailing `Z` only when the value's `Kind` is UTC.

```text
DTSTART:20260105T090000
RRULE:FREQ=WEEKLY;COUNT=8;BYDAY=MO
RDATE:20260114T090000
EXDATE:20260126T090000,20260209T090000
```

`Parse` is more permissive than `ToString` is strict — it is designed to accept a `VEVENT` fragment as-is:

- Lines other than `DTSTART`, `RRULE`, `RDATE`, and `EXDATE` are ignored, so `BEGIN:VEVENT`, `UID`, `SUMMARY`, and `END:VEVENT` may be present.
- Property parameters between the name and the colon are ignored: `DTSTART;TZID=Australia/Sydney:20260105T090000` parses as the floating wall-clock value `2026-01-05 09:00` (the zone is *not* applied — see [Hosting schedules](scheduling-host.md) for zone conversion at the host boundary), and `EXDATE;VALUE=DATE-TIME:…` is accepted.
- `RDATE` / `EXDATE` accept multiple comma-separated values, multiple lines (accumulated), and the `VALUE=DATE` form (`20260114`, read as midnight).
- Line endings may be CRLF or LF. Property names are case-insensitive.

The defect-naming `TryParse` reports: a missing start ("A recurrence set requires a DTSTART property line."), a repeated one ("The DTSTART property appears more than once."), a bad value ("The date-time value 'bogus' is not in a supported iCalendar DATE or DATE-TIME format."), any `RRULE` defect (with the rule's own message), and a start with nothing else ("A recurrence set requires at least one rule or explicit recurrence date.").

## Pattern 2 — a meeting series with two skipped dates and one moved date

iCalendar expresses "moved" as an exception on the original instant plus an explicit date for the replacement. The set below skips the Australia Day (26 January) and 9 February stand-ups, and moves the 12 January meeting to Wednesday 14 January:

```csharp
using Bodu.Globalization.Recurrence;

var series = new RecurrenceSet(
    start: new DateTime(2026, 1, 5, 9, 0, 0),
    rules: new[] { RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO") },
    dates: new[] { new DateTime(2026, 1, 14, 9, 0, 0) },                                        // moved-to instant
    exceptionDates: new[]
    {
        new DateTime(2026, 1, 12, 9, 0, 0),   // moved-from instant
        new DateTime(2026, 1, 26, 9, 0, 0),   // skipped
        new DateTime(2026, 2, 9, 9, 0, 0),    // skipped
    });

DateTime[] january = series.GetOccurrences(new DateTime(2026, 1, 1), new DateTime(2026, 2, 20)).ToArray();
// 2026-01-05 Mon, 01-14 Wed, 01-19 Mon, 02-02 Mon, 02-16 Mon

DateTime? nextAfterSkip = series.GetNextOccurrence(new DateTime(2026, 1, 26, 9, 0, 0), inclusive: true);   // 2026-02-02 09:00
DateTime? previous      = series.GetPreviousOccurrence(new DateTime(2026, 1, 26, 9, 0, 0), inclusive: true); // 2026-01-19 09:00
```

Point queries honour the same composition: with `inclusive: true` on an excluded instant, the answer moves past it in either direction.

## Pattern 3 — import from and export to a `VEVENT` fragment

Because unrelated lines are ignored on parse, a set can be read straight from the recurrence-bearing lines of a `VEVENT`. Export the canonical block into a `VEVENT` you assemble yourself:

```csharp
using Bodu.Globalization.Recurrence;

string vevent =
    "BEGIN:VEVENT\r\n" +
    "UID:standup-2026@example.com\r\n" +
    "SUMMARY:Weekly stand-up\r\n" +
    "DTSTART;TZID=Australia/Sydney:20260105T090000\r\n" +
    "RRULE:FREQ=WEEKLY;BYDAY=MO\r\n" +
    "EXDATE;VALUE=DATE-TIME:20260126T090000,20260209T090000\r\n" +
    "RDATE:20260114T090000\r\n" +
    "END:VEVENT";

RecurrenceSet imported = RecurrenceSet.Parse(vevent);
// imported.Start = 2026-01-05 09:00 (floating; the TZID parameter is ignored)

string canonical = imported.ToString();
// DTSTART:20260105T090000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO\r\nRDATE:20260114T090000\r\nEXDATE:20260126T090000,20260209T090000

string exported =
    "BEGIN:VEVENT\r\n" +
    "UID:standup-2026@example.com\r\n" +
    "SUMMARY:Weekly stand-up\r\n" +
    canonical + "\r\n" +
    "END:VEVENT\r\n";

bool roundTrips = RecurrenceSet.Parse(exported).Equals(imported);   // true
```

To preserve a UTC start, anchor the set on a `DateTimeKind.Utc` value: `DTSTART:20260105T090000Z` parses to a UTC `Start` and renders with the `Z` again.

## Next and previous over the set

`GetNextOccurrence(after, inclusive)` / `GetPreviousOccurrence(before, inclusive)` answer over the composed stream, so an exception date is never returned and an `RDATE` can be. The `DateTimeOffset` overloads interpret `Start`, `Dates`, and `ExceptionDates` — which are wall-clock values — in the offset of the argument and return an answer carrying that offset; no other conversion is performed:

```csharp
using Bodu.Globalization.Recurrence;

var series = RecurrenceSet.Parse("DTSTART:20260105T090000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=8\r\nEXDATE:20260126T090000");

DateTimeOffset? next = series.GetNextOccurrence(new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.FromHours(10)));
// 2026-02-02 09:00 +10:00 — 26 January is excluded
```

## Value equality

Two sets are equal when their `Start`, their rules (equal rules in the same order), their `Dates`, and their `ExceptionDates` are all equal. `Parse(set.ToString())` is always equal to `set`, so the canonical block is a reliable persisted form and change detector. `GetHashCode` is consistent with `Equals`.

## API summary

| Member | Purpose |
|---|---|
| `RecurrenceSet(DateTime start, IEnumerable<RecurrenceRule> rules, IEnumerable<DateTime>? dates = null, IEnumerable<DateTime>? exceptionDates = null)` | Construct; at least one rule or date is required. |
| `Start`, `Rules`, `Dates`, `ExceptionDates` | The components; date lists are ascending. |
| `Parse(string)` / `Parse(string, IFormatProvider?)` | Parse a property block or `VEVENT` fragment. |
| `TryParse(string?, out result)` / `TryParse(string?, IFormatProvider?, out result)` / `TryParse(string?, out result, out failureMessage)` | Boolean parse, optionally naming the defect. |
| `GetOccurrences()` / `GetOccurrences(from, to)` | Composed ascending enumeration (`DateTime`). |
| `GetNextOccurrence(after, inclusive = false)` / `GetPreviousOccurrence(before, inclusive = false)` | Point queries; `DateTime` and `DateTimeOffset`. |
| `ToString()` / `ToString(string?)` / `ToString(string?, IFormatProvider?)` | Canonical CRLF property block. |
| `Equals` / `GetHashCode` | Component-wise value equality. |

## Where to go next

- **[RFC 5545 recurrence rules](rrule.md)** — the grammar of each `RRULE` line.
- **[Hosting schedules](scheduling-host.md)** — driving a set alongside the other forms from one loop, and applying a time zone to a floating `DTSTART`.
- **[Runnable samples](../../samples/recurrence.md)** — `Bodu.Globalization.Recurrence.Samples.RecurrenceSets`.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)** — `RecurrenceSet`.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
