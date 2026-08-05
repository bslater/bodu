---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Globalization.Recurrence`
under
[`samples/Globalization.Recurrence/`](https://github.com/bslater/bodu/tree/master/samples/Globalization.Recurrence).
All five samples are **offline and deterministic** — they format with the invariant culture and, more
to the point, never read a clock, so output does not vary by machine, locale, or the time of day they
run. Each is a member of `bodu.slnx`, built and executed by CI, so the code they show cannot drift
from the current API. Each sample's README documents every scenario individually: its intent, what
the code does, the output to expect, and the APIs demonstrated.

Run any sample from the repository root:

```bash
dotnet run --project samples/Globalization.Recurrence/<SampleName>
```

## The samples

### Bodu.Globalization.Recurrence.Samples.RecurrenceRules

The RFC 5545 `RRULE` form via <xref:Bodu.Globalization.Recurrence.RecurrenceRule>: parsing, typed
part properties, and the canonical text round trip; the four `BY*` semantics implementations most
often disagree on — invalid dates skipped rather than clamped, the occurrence set as a genuine set,
`BYSETPOS` indexing the whole frequency period, and a `BY` filter never re-anchoring its interval;
`WKST` week numbering with year-straddling `BYWEEKNO` and the fifty-third week; the fluent
<xref:Bodu.Globalization.Recurrence.RecurrenceRuleBuilder> with
<xref:Bodu.Globalization.Recurrence.WeekDayNum> ordinals; and bounding a stream by `COUNT`, `UNTIL`,
a window, or `Take`. *Package: `Bodu.Globalization.Recurrence`.*

### Bodu.Globalization.Recurrence.Samples.CronExpressions

The Vixie cron form via <xref:Bodu.Globalization.Recurrence.CronExpression>: the five-field layout,
field grammar, and the `crontab(5)` `@` macros; the optional-seconds six-field layout selected by
<xref:Bodu.Globalization.Recurrence.CronFormat>, contrasting the overloads that infer the layout
against those that enforce it; canonical text and equality by schedule rather than spelling; the two
Vixie semantics that separate this dialect from Quartz — the day-of-month / day-of-week union rule
decided by a field's leading character, and a step wider than its range collapsing to the range
start; and the two failure surfaces, unreachable schedules and defect-named rejections.
*Package: `Bodu.Globalization.Recurrence`.*

### Bodu.Globalization.Recurrence.Samples.AnchoredIntervals

The calendar-free form via <xref:Bodu.Globalization.Recurrence.AnchoredInterval>: the RFC 5545
§3.3.6 duration grammar with its canonical normalization (`PT60M` → `PT1H`, `P7D` → `P1W`); the
anchor-supplied-per-query design that lets one instance serve many series, with the anchor itself
deliberately not an occurrence; arithmetic positioning so a query five years from its anchor costs
what a near one does; and the grammar's exact boundary, with a defect message naming the offending
token for each of fourteen rejections. *Package: `Bodu.Globalization.Recurrence`.*

### Bodu.Globalization.Recurrence.Samples.RecurrenceSets

The composition layer via <xref:Bodu.Globalization.Recurrence.RecurrenceSet>: rules unioned with
`RDATE` additions minus `EXDATE` removals, built up one part at a time; collisions across rules
emitted once; the iCalendar `DTSTART` / `RRULE` / `RDATE` / `EXDATE` property block as a storage
format that round-trips by value; and windowed and point queries applied across the whole
composition, so exception dates are never re-applied by the caller.
*Package: `Bodu.Globalization.Recurrence`.*

### Bodu.Globalization.Recurrence.Samples.SchedulingHost

The integrating view: all four forms behind one host-written adapter, exploiting the uniform
next/previous surface so nothing downstream branches on form; a mixed-form configuration block
validated with defect messages an operator can act on, plus the second-tier reachability probe that
catches schedules which parse cleanly and never fire; and a missed-run catch-up loop that is exactly
reproducible because the host supplies both ends of the window. Includes the recommended pattern for
zone-correct firing at the host boundary. *Package: `Bodu.Globalization.Recurrence`.*

## Conformance

The semantics these samples demonstrate are reconciled row by row against three committed corpora —
RFC 5545's worked examples, libical's occurrence counts, and a cron vector table derived from
Cronos's test suite — currently 830 in-scope rows with zero differences. See the
[recurrence guide](../guides/recurrence/index.md) for the contract, and
`corpus/recurrence/README.md` for each table's provenance and every recorded divergence.

## Related

- [Globalization.Calendar samples](calendar.md) — the notable-date engine and working-day arithmetic,
  a natural sibling of the recurrence package.
- [Core samples](core.md) — `WeekPattern` and the date extensions the calendar package builds on.
