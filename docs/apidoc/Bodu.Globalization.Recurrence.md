---
uid: Bodu.Globalization.Recurrence
---

![Bodu.Globalization.Recurrence](~/images/hero-recurrence.svg)

## Purpose

**Bodu.Globalization.Recurrence** models recurring schedules in industry-standard textual grammars: RFC 5545 (iCalendar) recurrence rules, Vixie-style cron expressions, and instant-anchored intervals in the RFC 5545 duration grammar. Every surface parses, formats, and enumerates occurrences without any calendar-data dependency — the package depends only on `Bodu.Core` — and every answer is a pure function of the arguments: no API reads the wall clock or consults the machine time zone.

Reach for this library when you need to answer "when does this schedule fire next?", "when was the previous occurrence?" (the due-ness comparison), or "which instants does this rule produce inside this window?" for schedules expressed as `RRULE` text, composed rule sets with extra and exception dates, cron expressions, or fixed intervals anchored to an instant such as a last completed run.

## Key types

- <xref:Bodu.Globalization.Recurrence.RecurrenceRule> — an immutable RFC 5545 recurrence rule (`RRULE`): a base frequency refined by an interval, an optional bound (`COUNT` or `UNTIL`), and the `BY` rule parts, with parse/format round-tripping and occurrence enumeration relative to a start date.
- <xref:Bodu.Globalization.Recurrence.RecurrenceRuleBuilder> — fluent construction of a <xref:Bodu.Globalization.Recurrence.RecurrenceRule> as an alternative to parsing its textual form.
- <xref:Bodu.Globalization.Recurrence.RecurrenceSet> — a composed set of recurring instants: one or more rule streams anchored at a common start, merged with explicit recurrence dates (`RDATE`) and with exception dates (`EXDATE`) removed.
- <xref:Bodu.Globalization.Recurrence.CronExpression> — a parsed Vixie-style cron expression with next/previous occurrence computation, under a <xref:Bodu.Globalization.Recurrence.CronFormat> selecting the five- or six-field grammar.
- <xref:Bodu.Globalization.Recurrence.AnchoredInterval> — a recurrence whose occurrences repeat at a fixed interval from a caller-supplied anchor instant ("every 4 hours after the last completed run"), with the RFC 5545 §3.3.6 duration grammar (`PT4H`, `P1DT2H30M`) as its canonical text.
- <xref:Bodu.Globalization.Recurrence.RecurrenceFrequency> and <xref:Bodu.Globalization.Recurrence.WeekDayNum> — the frequency scale (secondly through yearly) and the ordinal-qualified weekday (`BYDAY`) building block.

## Samples

Five runnable sample projects live under `samples/Globalization.Recurrence/` — one per schedule form
(`RecurrenceRules`, `CronExpressions`, `AnchoredIntervals`, `RecurrenceSets`) plus `SchedulingHost`,
which puts all four behind a single adapter and shows configuration validation and missed-run
catch-up. See the [samples catalogue](~/samples/recurrence.md).

## Notes

- **Preview.** The package is published for early evaluation; the public API surface is still taking shape and may change between releases without a major-version bump.
- **Complements the calendar engine.** `Bodu.Globalization.Calendar` consumes recurrence strategies for notable-date rules; this package is the standalone, data-free scheduling grammar underneath. See the [package matrix](~/docs/package-matrix.md) for how the globalization family fits together.
