# Bodu.Globalization.Recurrence.Samples.RecurrenceRules

`RecurrenceRule` — the RFC 5545 `RRULE` form. Five scenarios cover parsing and canonical
formatting, the `BY*` expansion and limiting semantics that implementations most often disagree on,
week numbering under `WKST`, the fluent `RecurrenceRuleBuilder`, and the four ways an occurrence
stream gets bounded.

Everything runs offline with fixed inputs, formatted with the invariant culture — deterministic
output every run.

```bash
dotnet run --project samples/Globalization.Recurrence/Bodu.Globalization.Recurrence.Samples.RecurrenceRules
```

NuGet consumers: `dotnet add package Bodu.Globalization.Recurrence`

## Scenario 1 — RuleBasics

**Intent.** Show the shape of the type: a rule is *pure syntax*. It carries no start instant, so the
series origin is supplied per query — which is what keeps it free of hidden state and safe to cache
or share.

**What it does.** Parses `FREQ=MONTHLY;BYDAY=1FR;COUNT=5`, reads every part back as a typed property
(including the defaults `INTERVAL=1` and `WKST=MO` that the text never stated), enumerates the
bounded stream, and shows the occurrence time of day riding along from the start instant. It then
round-trips a verbosely written rule through `ToString` and compares a terse spelling of the same
rule for equality.

**What to expect.** `COUNT=5` bounds the stream so no `Take` is needed; occurrences carry the start's
09:30; and the canonical text drops the redundant `INTERVAL=1` and `WKST=MO`, so the verbose and
terse spellings compare equal:

```text
--- RecurrenceRule: parse, inspect, enumerate ---
rule       : FREQ=MONTHLY;COUNT=5;BYDAY=1FR
Frequency  : Monthly
Interval   : 1
Count      : 5
Until      : (none)
WeekStart  : Monday
ByDay[0]   : Ordinal=1, Day=Friday, IsEveryOccurrence=False
start      : 2026-01-01 09:30
occurrences: 2026-01-02, 2026-02-06, 2026-03-06, 2026-04-03, 2026-05-01
first      : 2026-01-02 09:30

--- RecurrenceRule: canonical text round trip ---
input      : INTERVAL=1;FREQ=WEEKLY;BYDAY=MO,WE,FR;WKST=MO
canonical  : FREQ=WEEKLY;BYDAY=MO,WE,FR
round trip : True
terse == verbose : True
```

**APIs demonstrated.** `RecurrenceRule.Parse`, `.Frequency` / `.Interval` / `.Count` / `.Until` /
`.WeekStart` / `.ByDay`, `WeekDayNum.Ordinal` / `.Day` / `.IsEveryOccurrence`,
`RecurrenceRule.GetOccurrences(DateTime)`, `.ToString()`, `.Equals`.

## Scenario 2 — ByPartSemantics

**Intent.** Pin the four `BY*` behaviours that recurrence libraries most often get wrong. Each one
has a documented upstream defect behind it, and each is the sort of thing that looks like a bug
until you read the RFC.

**What it does.** Four demonstrations in order: a monthly rule anchored on the 31st (and its explicit
`BYMONTHDAY=31` twin, and a 29 February yearly rule); two rules whose `BY` values collide on the same
date; a `BYSETPOS` rule queried from two different anchors in the same week; and an
`INTERVAL=14` daily rule filtered by `BYMONTH`, printing the gaps between survivors.

**What to expect.** Invalid dates are *skipped, never clamped* — February, April, June, September and
November simply have no 31st, so they are absent rather than rolled back. Colliding `BY` values
contribute one occurrence each, not two. `BYSETPOS=1` gives the same Monday for both anchors
(the Wednesday anchor does not promote Wednesday to position 1 — the period's position 1 is still
its Monday, which merely falls before the start). And the fourteen-day grid is never restarted by the
month filter, which the gaps prove: every gap is a multiple of 14.

```text
--- Invalid dates are skipped, never clamped ---
FREQ=MONTHLY from 2026-01-31 : 2026-01-31, 2026-03-31, 2026-05-31, 2026-07-31, 2026-08-31, 2026-10-31
BYMONTHDAY=31 from 2026-01-01: 2026-01-31, 2026-03-31, 2026-05-31, 2026-07-31, 2026-08-31, 2026-10-31
29 February yearly           : 2024-02-29, 2028-02-29, 2032-02-29

--- The occurrence set is a set ---
BYMONTHDAY=1,-31             : 2026-01-01, 2026-02-01, 2026-03-01, 2026-04-01
BYDAY=1MO,-4MO               : 2026-02-02, 2026-03-02, 2026-03-09, 2026-04-06

--- BYSETPOS indexes the whole frequency period ---
last weekday of the month    : 2026-01-30, 2026-02-27, 2026-03-31, 2026-04-30
BYSETPOS=1 anchored Monday   : 2026-03-02, 2026-03-09, 2026-03-16
BYSETPOS=1 anchored Wednesday: 2026-03-09, 2026-03-16, 2026-03-23

--- A BY filter never re-anchors the interval ---
every 14th day, Oct+Dec only : 2026-10-11, 2026-10-25, 2026-12-06, 2026-12-20, 2027-10-10, 2027-10-24
gaps in days                 : 14, 42, 14, 294, 14
```

The gap `42` is three fourteen-day steps across November, and `294` is twenty-one of them across the
rest of the year — both multiples of 14, which is the observable proof the grid survived the filter.

**APIs demonstrated.** `RecurrenceRule.Parse` with `BYMONTHDAY` (including negative day numbers),
`BYDAY` ordinals, `BYSETPOS`, `BYMONTH`, and `INTERVAL`; `RecurrenceRule.GetOccurrences(DateTime)`.

## Scenario 3 — WeekNumbering

**Intent.** Show that `WKST` reparameterises week *numbering*, not just weekly intervals. It changes
which dates `BYWEEKNO` resolves to and which years have a fifty-third week — and numbered weeks
straddle the calendar year, so week 1 can begin in December.

**What it does.** Runs the RFC's own `WKST=MO` / `WKST=SU` fortnightly pair; resolves `BYWEEKNO=1`
both with and without a `BYDAY` limit; resolves week 20 under two different week starts; and asks for
week 53 and week −1.

**What to expect.** The two week starts select genuinely different dates. `BYWEEKNO=1;BYDAY=MO` from
2025 yields **2025-12-29** — a date in the *previous* calendar year, because 2026's week 1 begins
there. Without a `BYDAY` limit, `BYWEEKNO` expands to the whole seven-day week. Week 53 exists only
in some years, so it skips 2021–2025 entirely:

```text
--- WKST shifts a weekly interval ---
WKST=MO : 1997-08-05, 1997-08-10, 1997-08-19, 1997-08-24
WKST=SU : 1997-08-05, 1997-08-17, 1997-08-19, 1997-08-31

--- BYWEEKNO: weeks straddle the calendar year ---
BYWEEKNO=1;BYDAY=MO      : 2025-12-29, 2027-01-04, 2028-01-03
BYWEEKNO=1 (whole week)  : 2026-01-01, 2026-01-02, 2026-01-03, 2026-01-04, 2027-01-04, 2027-01-05, 2027-01-06

--- BYWEEKNO honours WKST ---
week 20, WKST=MO         : 2026-05-11, 2027-05-17
week 20, WKST=SU         : 2026-05-18, 2027-05-17

--- The fifty-third week exists only in some years ---
BYWEEKNO=53;BYDAY=MO     : 2020-12-28, 2026-12-28, 2032-12-27
BYWEEKNO=-1;BYDAY=MO     : 2026-12-28, 2027-12-27, 2028-12-25
```

**APIs demonstrated.** `RecurrenceRule.Parse` with `WKST`, `BYWEEKNO` (positive and negative), and
`BYDAY`; `RecurrenceRule.GetOccurrences(DateTime)`.

## Scenario 4 — BuildingRules

**Intent.** Show `RecurrenceRuleBuilder` as the fluent alternative to hand-writing `RRULE` text, and
`WeekDayNum` as the value that carries an optional ordinal alongside a weekday.

**What it does.** Builds a fortnightly Monday/Wednesday/Friday rule and asserts it equals the parsed
text form; builds "third Thursday" and "last Friday" rules with ordinal and negative-ordinal
`WeekDayNum` values; shows the `IsEveryOccurrence` flag and deconstruction; and closes with two
realistic bounded rules — US Thanksgiving via `UNTIL`, and quarter-end via `BySetPos(-1)`.

**What to expect.** The builder is a spelling of the same grammar, so the built rule equals the
parsed one — the property that makes it safe in configuration code. Ordinals render as `3TH` and
`-1FR` in the rule's canonical text:

```text
--- RecurrenceRuleBuilder: fluent construction ---
built      : FREQ=WEEKLY;INTERVAL=2;COUNT=6;BYDAY=MO,WE,FR
occurrences: 2026-01-05, 2026-01-07, 2026-01-09, 2026-01-19, 2026-01-21, 2026-01-23
equals parsed text : True

--- WeekDayNum: ordinals inside BYDAY ---
third Thursday    : FREQ=MONTHLY;COUNT=4;BYDAY=3TH
occurrences       : 2026-01-15, 2026-02-19, 2026-03-19, 2026-04-16
last Friday       : FREQ=MONTHLY;COUNT=4;BYDAY=-1FR
occurrences       : 2026-01-30, 2026-02-27, 2026-03-27, 2026-04-24
WeekDayNum(0, Tue) : Ordinal=0, Day=Tuesday, IsEveryOccurrence=True
  ordinal  0 Tuesday   -> FREQ=MONTHLY;BYDAY=TU
  ordinal  3 Thursday  -> FREQ=MONTHLY;BYDAY=3TH
  ordinal -1 Friday    -> FREQ=MONTHLY;BYDAY=-1FR

--- Builder: a bounded yearly rule ---
rule       : FREQ=YEARLY;UNTIL=20301231T000000;BYDAY=4TH;BYMONTH=11
occurrences: 2026-11-26, 2027-11-25, 2028-11-23, 2029-11-22, 2030-11-28
quarter end: FREQ=MONTHLY;INTERVAL=3;COUNT=4;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1
occurrences: 2026-03-31, 2026-06-30, 2026-09-30, 2026-12-31
```

> The canonical token for a `WeekDayNum` is read from the rule that carries it, because
> `WeekDayNum.ToString()` currently emits the compiler-generated record form rather than its
> iCalendar token — see the *Known wrinkle* note in the domain README.

**APIs demonstrated.** `RecurrenceRuleBuilder` (`.WithInterval` / `.WithCount` / `.WithUntil` /
`.ByDay` / `.ByMonth` / `.BySetPos` / `.Build`), `WeekDayNum` construction and `Deconstruct`,
`RecurrenceFrequency`.

## Scenario 5 — BoundedEnumeration

**Intent.** Show the four ways an occurrence stream gets bounded, and the two point queries that
answer a scheduling question without enumerating anything.

**What it does.** Bounds the same daily rule by `COUNT`, by `UNTIL`, by the caller's `Take`, and by
the windowed `GetOccurrences` overload. Then queries a weekly rule at an instant that is itself an
occurrence, in both directions and with the inclusive flag both ways. It ends by querying a bounded
rule from beyond its end and from before its start.

**What to expect.** All four bounding mechanisms agree on the same underlying series; the window
overload yields only what falls inside it. The inclusive flag is what decides whether an exact hit
is returned. Past the end of a bounded rule, `next` reports nothing while `previous` still answers:

```text
--- Bounding a stream: COUNT, UNTIL, window, Take ---
COUNT=4        : 2026-01-01, 2026-01-11, 2026-01-21, 2026-01-31
UNTIL=20260210 : 2026-01-01, 2026-01-11, 2026-01-21, 2026-01-31, 2026-02-10
unbounded+Take : 2026-01-01, 2026-01-11, 2026-01-21, 2026-01-31
window Feb-Mar : 2026-02-10, 2026-02-20, 2026-03-02, 2026-03-12, 2026-03-22

--- Point queries: next and previous ---
series start   : 2026-01-05 09:00
query          : 2026-02-02 09:00
next (exclusive): 2026-02-09 09:00
next (inclusive): 2026-02-02 09:00
prev (exclusive): 2026-01-26 09:00
prev (inclusive): 2026-02-02 09:00

--- Point queries past the end of a bounded rule ---
occurrences     : 2026-01-01, 2026-01-02, 2026-01-03
next after Jun 1: (none)
prev before Jun1: 2026-01-03 08:00
prev before 2025: (none)
```

**APIs demonstrated.** `RecurrenceRule.GetOccurrences(DateTime)` and
`GetOccurrences(DateTime, DateTime, DateTime)`, `GetNextOccurrence(DateTime, DateTime, bool)`,
`GetPreviousOccurrence(DateTime, DateTime, bool)`, `COUNT` / `UNTIL` bounding.

## Layout

```text
Bodu.Globalization.Recurrence.Samples.RecurrenceRules/
  Program.cs                          # runs the scenarios in order
  Scenarios/RuleBasics.cs
  Scenarios/ByPartSemantics.cs
  Scenarios/WeekNumbering.cs
  Scenarios/BuildingRules.cs
  Scenarios/BoundedEnumeration.cs
```

## Related

- `Bodu.Globalization.Recurrence.Samples.RecurrenceSets` — composing rules with `RDATE` / `EXDATE`.
- `Bodu.Globalization.Recurrence.Samples.CronExpressions` — the cron form of the same query surface.
- `Bodu.Globalization.Recurrence.Samples.SchedulingHost` — all four forms behind one adapter.
