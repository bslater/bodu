---
title: Bodu.Globalization.Recurrence — Core concepts
---

# Bodu.Globalization.Recurrence — Core concepts

This page is the vocabulary the rest of the recurrence documentation assumes. Read it once before the [getting-started samples](getting-started.md) or the [recurrence guide](../../guides/recurrence/index.md), and refer back to it whenever a term feels imprecise.

Part of the **[Globalization & Calendars](../topics/globalization-and-calendars.md)** topic.

For the high-level shape of the library and the "which form" table, start with the [introduction](index.md).

## Occurrence vs. due-ness

An **occurrence** is an instant a schedule produces. A `FREQ=WEEKLY;BYDAY=MO` rule anchored at a Monday produces every subsequent Monday at the anchor's time of day; a `0 2 * * *` cron expression produces 02:00 every day; a `PT4H` interval anchored at 06:15 produces 10:15, 14:15, 18:15, and so on. Occurrences are what `GetOccurrences`, `GetNextOccurrence`, and `GetPreviousOccurrence` return.

**Due-ness** is a decision the *caller* makes over those answers, and the library deliberately does not model it. It never stores a last-run instant, marks an occurrence as consumed, or owns a timer. The canonical recipe is a single comparison against the previous occurrence:

```csharp
bool isDue = lastCompleted < schedule.GetPreviousOccurrence(now, inclusive: true);
```

Because the answer is an instant rather than a backlog, missed occurrences **coalesce**: a host that slept through five occurrences owes exactly one catch-up run, and an evaluation five days late answers the same boolean as one evaluated a minute late. The [guide](../../guides/recurrence/index.md#the-due-ness-recipe) works the recipe through for each form.

## Anchor and `DTSTART`

Neither a <xref:Bodu.Globalization.Recurrence.RecurrenceRule> nor an <xref:Bodu.Globalization.Recurrence.AnchoredInterval> carries a start of its own. The reference instant is supplied to every occurrence query, so one parsed rule can be applied to many series:

- For a **rule**, the reference is the series start — iCalendar's `DTSTART`. Expansion begins at the start's frequency period, occurrences inherit the start's time of day (unless `BYHOUR` / `BYMINUTE` / `BYSECOND` override it), and the start itself is emitted **only when it satisfies the rule**: a `BYDAY=MO` rule anchored on a Wednesday does not produce that Wednesday.
- For an **interval**, the reference is the anchor. The occurrence series is `anchor + k·interval` for `k ≥ 1`, so the anchor is **never** an occurrence — a run completed at `now` is not immediately due again. What the anchor *means* ("the last completed run", "first enrolment", "contract start") is entirely the caller's; the type never interprets it.

A <xref:Bodu.Globalization.Recurrence.RecurrenceSet> is the exception: it owns its `Start`, because the `DTSTART` line is part of the iCalendar property block it parses and formats, and every rule in the set is anchored at that one start. A <xref:Bodu.Globalization.Recurrence.CronExpression> needs no anchor at all — cron is defined purely by wall-clock field membership.

## Inclusive vs. exclusive boundaries

Every next / previous query takes an `inclusive` flag, defaulting to `false`:

| Call | Returns |
|---|---|
| `GetNextOccurrence(after)` | the first occurrence **strictly after** `after` |
| `GetNextOccurrence(after, inclusive: true)` | the first occurrence **at or after** `after` |
| `GetPreviousOccurrence(before)` | the last occurrence **strictly before** `before` |
| `GetPreviousOccurrence(before, inclusive: true)` | the last occurrence **at or before** `before` |

The default (exclusive) is what a loop wants — pass the occurrence you just handled as `after` and the next call cannot return it again. The inclusive form is what the due-ness comparison wants — an occurrence falling exactly at `now` is due now.

The windowed enumerators `GetOccurrences(start, from, to)` are inclusive at **both** ends: they yield every occurrence in `[from, to]`. Whether an instant is an occurrence depends only on the schedule and its anchor, never on the query window, so the windowed and open-ended enumerators always agree on membership.

## `COUNT` and `UNTIL`

A rule is bounded in one of two mutually exclusive ways, mirroring RFC 5545 §3.3.10:

- **`COUNT=n`** — the series stops after *n* occurrences counted from the start. `RecurrenceRule.Count` is `null` when unbounded.
- **`UNTIL=<date-time>`** — the series stops at the last occurrence **at or before** the bound (the bound is inclusive). The value uses the RFC 5545 `DATE-TIME` form `YYYYMMDDTHHMMSS`, with a trailing `Z` marking UTC (`Until.Kind` is then `DateTimeKind.Utc`), or the `DATE` form `YYYYMMDD`. `RecurrenceRule.Until` is `null` when unbounded.

Text carrying both parts is rejected — `TryParse` reports *The COUNT and UNTIL rule parts cannot both appear in the same rule.* — and on <xref:Bodu.Globalization.Recurrence.RecurrenceRuleBuilder> calling `WithCount` clears any `WithUntil` and vice versa. An unbounded rule enumerates to the end of the representable calendar (year 9999); bound it with `Take`, the windowed overload, or a next-occurrence loop.

## `WKST` and week numbering

`WKST` names the day the week starts on and governs two calculations: which week a date belongs to for `INTERVAL` arithmetic on a `WEEKLY` rule, and ISO-style week numbering for `BYWEEKNO`. RFC 5545 defaults it to Monday, and so does the library — `RecurrenceRule.WeekStart` is `DayOfWeek.Monday` when the part is absent, and a `WKST=MO` part is omitted from the canonical text.

The effect is visible only on rules with `INTERVAL > 1`: the classic RFC example `FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO` and the same rule with `WKST=SU` group the Tuesdays and Sundays into different fortnights and therefore produce different dates. Set it with `WithWeekStart(DayOfWeek)` on the builder.

## The `BY*` parts and `BYSETPOS`

The `BY*` rule parts refine the set of instants a frequency period produces. Each is exposed as an `IReadOnlyList<T>` on the rule (empty when absent) and has a matching `By…(params …)` setter on the builder:

| Part | Values | Role |
|---|---|---|
| `BYMONTH` | 1–12 | Limits (or, on a `YEARLY` rule, expands to) the listed months. |
| `BYWEEKNO` | ±1–53 | ISO week numbers within the year (`YEARLY` only). |
| `BYYEARDAY` | ±1–366 | Days of the year, negative counting from the end. |
| `BYMONTHDAY` | ±1–31 | Days of the month; `-1` is the last day. |
| `BYDAY` | `MO` … `SU`, optionally with an ordinal (`1MO`, `-1FR`) | Weekdays. The ordinal selects the *n*th (or *n*th-from-last) occurrence within the month or year; `0` — the plain `MO` form — selects every occurrence. Represented by <xref:Bodu.Globalization.Recurrence.WeekDayNum>`(Ordinal, Day)`. |
| `BYHOUR` / `BYMINUTE` / `BYSECOND` | 0–23 / 0–59 / 0–60 | Time-of-day expansion over each occurrence day; absent parts inherit the start's time components. |
| `BYSETPOS` | ±1–366 | Applied **last**: after all other parts have produced the period's candidate instants in ascending order, keep only the listed positions. `-1` keeps the last, `1` the first. |

`BYSETPOS` is what turns a weekday list into "the last working day of the month": `FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1` generates every weekday of the month and keeps the final one. Whether a part *limits* (filters the period's default instants) or *expands* (multiplies them) follows the RFC 5545 §3.3.10 table for the rule's frequency.

Occurrence enumeration is implemented for the `DAILY`, `WEEKLY`, `MONTHLY`, and `YEARLY` frequencies. Rules with a sub-daily `FREQ` (`SECONDLY`, `MINUTELY`, `HOURLY`) parse and round-trip so that stored text is never rejected, but enumerating them throws `NotSupportedException` until that follow-on lands.

## Cron field semantics

A <xref:Bodu.Globalization.Recurrence.CronExpression> matches an instant when each field — second (six-field layout only), minute, hour, day-of-month, month, day-of-week — contains the instant's corresponding component. Fields accept `*`, single values, ranges (`a-b`), steps (`*/n`, `a-b/n`, `a/n`), comma-separated lists, and the three-letter `JAN`–`DEC` / `SUN`–`SAT` names (case-insensitive). Day-of-week runs 0–7 with both `0` and `7` meaning Sunday.

**The day-field union rule.** Vixie cron combines the two day fields by looking at their first character: when *both* day-of-month and day-of-week are restricted (neither starts with `*`), an instant matches if it satisfies **either** field — so `0 0 13 * FRI` fires on the 13th of every month *and* on every Friday, not only on Friday the 13th. When at least one begins with `*`, both must match. A stepped star such as `*/2` still narrows the days it matches, but it does not count as "restricted" for this rule.

**Steps.** `a-b/n` selects `a`, `a+n`, … up to `b`; `a/n` runs from `a` to the field maximum; a step that does not divide the range stops at the range end rather than wrapping. A step larger than the field's range is accepted and collapses to the range's first value (`*/100` in the minute field is minute `0`).

**Macros.** `@yearly` / `@annually` (`0 0 1 1 *`), `@monthly` (`0 0 1 * *`), `@weekly` (`0 0 * * 0`), `@daily` / `@midnight` (`0 0 * * *`), and `@hourly` (`0 * * * *`) expand to the five-field layout; requesting `CronFormat.WithSeconds` for a macro is a parse failure. `@reboot` and `@every` are not cron schedules and are not recognized.

**Layout.** `Parse(string)` infers `Standard` or `WithSeconds` from the field count; `Parse(string, CronFormat)` insists on one. The Quartz extensions `L`, `W`, `#`, and `?` are not yet supported and throw `NotSupportedException` rather than parsing to something else.

**Canonical text.** `ToString()` renders each field as `*` or an ascending comma-separated numeric list — `0 2 * * MON-FRI` becomes `0 2 * * 1,2,3,4,5` and `@daily` becomes `0 0 * * *` — and two expressions that select the same field sets are equal regardless of how they were spelled.

## The twelve-year search horizon

A cron search walks forward (or backward) field by field and stops at a horizon of **twelve years** from the query instant. The largest gap between consecutive occurrences of any *satisfiable* expression is eight years — a February 29th schedule crossing a non-leap century year such as 2100 — so twelve years covers every real schedule with margin, while an expression that can never match (`0 0 30 2 *`, February 30th) answers `null` at the horizon instead of scanning unboundedly.

A rule search is bounded differently: an unbounded `RecurrenceRule` enumerates to the end of the representable calendar, so `GetNextOccurrence` on a rule that can never match (30 February yearly) also answers `null` rather than looping, just later.

## Duration grammar

An <xref:Bodu.Globalization.Recurrence.AnchoredInterval> is constructed from a `TimeSpan` or parsed from RFC 5545 §3.3.6 duration text, and renders back to that grammar:

| Text | Interval |
|---|---|
| `PT4H` | 4 hours |
| `PT90M` | 90 minutes (renders as `PT1H30M`) |
| `P1D` | 1 day |
| `P1DT2H30M` | 1 day 2 hours 30 minutes |
| `P2W` | 14 days — the weeks form is used whenever the interval is an exact multiple of seven days |

The `P` designator is mandatory, `T` separates date from time components, components appear in `W` / `D` / `H` / `M` / `S` order without repetition, and `W` cannot be combined with other components. The interval must be a positive whole number of seconds: the grammar carries no sign and no sub-second precision, so a negative, zero, or fractional-second `TimeSpan` is rejected by the constructor and by `TryParse` (the failure message names which rule was broken). Normalization means the canonical text is unique — `PT1440M`, `PT24H`, and `P1D` all render as `P1D` and compare equal.

## Offsets and daylight saving

Each form answers over both `DateTime` and `DateTimeOffset`, and the offset model is the same throughout:

- **`DateTime`** overloads preserve the argument's `Kind` (`Utc`, `Local`, or `Unspecified`) in the answer and never interpret it. For an anchored interval, instants are compared by tick value, so the caller is responsible for supplying commensurable arguments (do not mix a UTC anchor with a local `after`).
- **`DateTimeOffset`** overloads expand on the argument's **wall-clock time** and reattach the argument's **fixed offset** to each answer. A rule anchored at `2026-01-05T09:00-05:00` produces occurrences at 09:00 `-05:00`; a cron query at `+10:00` answers at `+10:00`.

No form carries a time zone, so a daylight-saving transition is not modeled: a `-05:00` series stays at `-05:00` when the zone moves to `-04:00`. A host that wants a *local-time* schedule across transitions re-derives the offset from its `TimeZoneInfo` on each evaluation and passes that offset in; the library stays pure and the policy stays in the host. The [guide](../../guides/recurrence/index.md#offsets-purity-and-daylight-saving) shows the pattern.

## Value equality and canonical text

Every form implements `IEquatable<T>` over its **components**, not its source text: two rules are equal when frequency, interval, bound, week start, and every `BY*` list match; two cron expressions when every field set matches; two intervals when the normalized `TimeSpan` matches; two sets when start, rules (in order), dates, and exception dates match. `ToString()` renders the canonical text — RFC 5545 part order for a rule, numeric lists for cron, normalized duration for an interval, a CRLF-separated `DTSTART` / `RRULE` / `RDATE` / `EXDATE` block for a set — and the canonical text re-parses to an equal value.

This is what lets a host persist schedules as text and detect a configuration change by comparing parsed objects: `CronExpression.Parse("@daily").Equals(CronExpression.Parse("0 0 * * *"))` is `true`.

## Where to go next

- **[Getting started](getting-started.md)** — install plus a parse → next / previous → enumerate sample for each form, the builder, a set round trip, and defect-naming `TryParse`.
- **[Introduction](index.md)** — the four forms side by side, the common contract, and the "which form" table.
- **[Recurrence and scheduling guide](../../guides/recurrence/index.md)** — the due-ness recipe, calendar-aware filtering by composition, bounded searches, and conformance.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)** — full type-by-type docs.
