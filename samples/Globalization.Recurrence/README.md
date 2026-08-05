# Globalization.Recurrence Samples

Console applications demonstrating the `Bodu.Globalization.Recurrence` package — the four schedule
forms it models and the uniform query surface they share. Each sample is a standalone project; run
one with:

```bash
dotnet run --project samples/Globalization.Recurrence/<SampleName>
```

Every sample is offline and deterministic: fixed instants formatted with the invariant culture, so
output does not vary by machine, locale, or the time of day the sample runs. That last point is not
incidental — the package reads no wall clock and resolves no time zone, which is exactly what lets
its samples double as CI smoke tests.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Globalization.Recurrence.Samples.RecurrenceRules` | The RFC 5545 `RRULE` form: parsing and canonical text, the `BY*` semantics implementations disagree on (invalid dates skipped not clamped, the occurrence set as a genuine set, `BYSETPOS` over the whole period, a `BY` filter never re-anchoring), `WKST` week numbering and year-straddling `BYWEEKNO`, the fluent `RecurrenceRuleBuilder`, and the four ways a stream gets bounded | `Bodu.Globalization.Recurrence` |
| `Bodu.Globalization.Recurrence.Samples.CronExpressions` | The Vixie cron form: the five-field layout and `@` macros, the optional-seconds six-field layout with format inference versus enforcement, canonical text and schedule equality, the day-field union rule and oversized-step handling that separate Vixie from Quartz, and both failure surfaces (unreachable schedules, defect-named rejections) | `Bodu.Globalization.Recurrence` |
| `Bodu.Globalization.Recurrence.Samples.AnchoredIntervals` | The calendar-free form: the RFC 5545 §3.3.6 duration grammar and its canonical normalization, the anchor-per-query design that lets one instance serve many series, arithmetic positioning for distant queries, and the grammar's exact boundary with a defect message per rejection | `Bodu.Globalization.Recurrence` |
| `Bodu.Globalization.Recurrence.Samples.RecurrenceSets` | The composition layer: rules unioned with `RDATE` additions minus `EXDATE` removals, collision handling across rules, the iCalendar property-block round trip as a storage format, and windowed and point queries across the whole composition | `Bodu.Globalization.Recurrence` |
| `Bodu.Globalization.Recurrence.Samples.SchedulingHost` | The integrating view: all four forms behind one host-written adapter, mixed-form configuration validated with actionable defect messages plus a reachability probe, the offset-bearing query surface every form carries, and a reproducible missed-run catch-up loop that shows what the purity contract buys | `Bodu.Globalization.Recurrence` |

Each sample project has its own README with the four-part per-scenario breakdown (Intent / What it
does / What to expect / APIs demonstrated).

## Where to start

Read `SchedulingHost` first if you want to know what consuming the package looks like; read one of
the four form-specific samples first if you already know which form you need.

## Conformance

The semantics these samples demonstrate are not asserted by the samples alone. They are reconciled
row by row against three committed corpora — the RFC's own worked examples, libical's occurrence
counts, and a cron vector table derived from Cronos's test suite — currently 830 in-scope rows with
zero differences. `corpus/recurrence/README.md` records each table's provenance and every deliberate
divergence, including the ones these samples call out (the Vixie day-field union rule and
oversized-step handling).

## API coverage

The samples exercise every public type and, with two deliberate exceptions, every member that has
observable behaviour worth showing. Not demonstrated, on purpose:

- **`RecurrenceFrequency.Hourly` / `.Minutely` / `.Secondly`** — these parse and round-trip but do
  not enumerate, because this library expands dates rather than intra-day times. The
  `RecurrenceRules` sample states that scope limit rather than exercising the values.
- **The `IParsable<T>` / `ISpanParsable<T>` / `IFormattable` overloads** — `Parse`/`TryParse` taking
  an `IFormatProvider` or a `ReadOnlySpan<char>`, `ToString(format[, provider])`, and
  `Equals(object)`. They exist to satisfy the BCL interface contracts; cron and `RRULE` text is
  culture-invariant by definition, which is precisely why the provider is ignored, so a sample
  passing `CultureInfo.InvariantCulture` would demonstrate nothing.

## Known wrinkle

`WeekDayNum.ToString()` currently emits the compiler-generated record form
(`WeekDayNum { Ordinal = 1, Day = Friday, … }`) rather than its iCalendar token (`1FR`), which is
inconsistent with `RecurrenceRule`, `RecurrenceSet`, `CronExpression`, and `AnchoredInterval` — all
of which render canonical text. The samples therefore read the ordinal and day as properties, and
show the canonical token via the rule that carries it. This is recorded rather than worked around
silently.
