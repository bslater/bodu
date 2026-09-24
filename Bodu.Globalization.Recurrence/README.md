# Bodu.Globalization.Recurrence

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Recurrence-rule evaluation for .NET: four ways to express "when does this happen again?", each
answering next- **and** previous-occurrence queries over `DateTime` and `DateTimeOffset`.

```csharp
using Bodu.Globalization.Recurrence;

// RFC 5545 RRULE — the second Tuesday of every month
var rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=2TU", null);
DateTime? next = rule.GetNextOccurrence(start: new DateTime(2026, 1, 1),
                                        after: new DateTime(2026, 3, 5),
                                        inclusive: false);

// Vixie cron — 06:30 on weekdays
var cron = CronExpression.Parse("30 6 * * 1-5");

// "every 4 hours after the last completed run"
var every4h = AnchoredInterval.Parse("PT4H", null);
```

## Installation

```shell
dotnet add package Bodu.Globalization.Recurrence
```

Targets `net8.0`. Depends only on
[`Bodu.Core`](https://www.nuget.org/packages/Bodu.Core) — **not** on
`Bodu.Globalization.Calendar`, which it is a sibling of rather than a layer on.

## The four forms

| Type | Grammar | For |
|---|---|---|
| `RecurrenceRule` | RFC 5545 `RRULE` | Calendar-style recurrence: `FREQ`/`INTERVAL`/`COUNT`/`UNTIL`/`WKST` and the full `BY*` model incl. `BYSETPOS` |
| `RecurrenceSet` | iCalendar property block | One or more rules composed with explicit `RDATE` additions and `EXDATE` exclusions |
| `CronExpression` | Vixie cron | Five-field, optional-seconds six-field, and the `@yearly`…`@hourly` macros |
| `AnchoredInterval` | RFC 5545 §3.3.6 duration | `anchor + k·interval` for `k ≥ 1`, with the anchor passed per query |

`RecurrenceRule` implements `IParsable`/`ISpanParsable`/`IFormattable` and round-trips its canonical
text. Every form carries a defect-naming `TryParse(s, out result, out failureMessage)` overload that
says *what* was wrong rather than just returning `false`, and compares by value.

## Purity

Every answer is a pure function of its arguments — **no wall clock, no machine time zone**. A query
takes the instant to search from, so the same inputs always give the same answer, which makes
schedules testable without freezing time. This is enforced, not merely intended: a metadata-scan
guard (`PurityTests`) fails the build if a banned wall-clock or local-time-zone API appears anywhere
in the assembly.

## Correctness

Validated against an RFC 5545 §3.8.5.3 worked-example corpus, then hardened against defects reported
to other implementations — python-dateutil, rrule.js, ical4j, ical.net, ical.js, libical, lib-recur,
ice_cube, Cronos, NCrontab, cronie, croniter, robfig/cron and Quartz — with that corpus kept as
regression tests. Fixes it produced include Vixie's leading-character rule for cron day-field
restriction, candidate-set deduplication before `BYSETPOS`/`COUNT`, the `BYDAY` ordinal when `BYDAY`
limits alongside `BYMONTHDAY`, and `BYWEEKNO` numbering generalized from the ISO rule to `WKST`.

## Known limits

- **Sub-daily `RRULE` frequencies are not enumerated.** `HOURLY`, `MINUTELY` and `SECONDLY` parse and
  round-trip, but `GetNextOccurrence` / `GetPreviousOccurrence` throw `NotSupportedException` naming
  the frequency. For sub-daily schedules use `AnchoredInterval` (or `CronExpression`, whose
  seconds-field layout covers the common cases).
- **Quartz cron extensions** (`L`, `W`, `#`, `?`) are rejected with `NotSupportedException`; the
  parser is Vixie-compatible.
- `CronExpression` searches a documented 12-year horizon before reporting no occurrence.
- No `.ics` reader — this package evaluates rules, it does not parse calendar files.

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
