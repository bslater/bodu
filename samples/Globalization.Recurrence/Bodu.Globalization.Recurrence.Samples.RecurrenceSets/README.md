# Bodu.Globalization.Recurrence.Samples.RecurrenceSets

`RecurrenceSet` — the composition layer that folds one or more `RRULE`s together with explicit
`RDATE` additions and `EXDATE` removals into a single occurrence stream. Three scenarios cover how
the parts combine, the iCalendar property-block round trip, and querying a composed set.

Everything runs offline with fixed inputs, formatted with the invariant culture — deterministic
output every run.

```bash
dotnet run --project samples/Globalization.Recurrence/Bodu.Globalization.Recurrence.Samples.RecurrenceSets
```

NuGet consumers: `dotnet add package Bodu.Globalization.Recurrence`

## Scenario 1 — SetComposition

**Intent.** Show the composition rule — the union of every rule and every explicit date, minus every
exception date, emitted once each in ascending order — one part at a time, so each part's
contribution is visible.

**What it does.** Builds the same weekly rule into four sets of increasing complexity: rule only,
plus two `RDATE`s, plus two `EXDATE`s (one targeting a rule-produced date, one targeting an `RDATE`),
and finally two rules at once. It then builds a set whose two rules genuinely collide on one date,
and reads every component back through the exposed properties.

**What to expect.** Unlike a bare rule, a set *owns* its start instant, so `GetOccurrences` takes no
arguments. `RDATE`s merge into the stream in order rather than appending. `EXDATE` wins over both
rules and `RDATE`s. And where two rules produce the same instant it appears once, because the result
is a set rather than a concatenation:

```text
--- A set from a single rule ---
start        : 2026-01-05 09:00
rule         : FREQ=WEEKLY;COUNT=6;BYDAY=MO
occurrences  : 2026-01-05, 2026-01-12, 2026-01-19, 2026-01-26, 2026-02-02, 2026-02-09

--- Adding explicit dates (RDATE) ---
RDATEs       : 2026-01-08, 2026-01-21
occurrences  : 2026-01-05, 2026-01-08, 2026-01-12, 2026-01-19, 2026-01-21, 2026-01-26, 2026-02-02, 2026-02-09

--- Removing dates (EXDATE) ---
EXDATEs      : 2026-01-19, 2026-01-21
occurrences  : 2026-01-05, 2026-01-08, 2026-01-12, 2026-01-26, 2026-02-02, 2026-02-09
               (the 19th came from the rule, the 21st from an RDATE -- both removed)

--- Several rules at once ---
rule 1       : FREQ=WEEKLY;COUNT=4;BYDAY=MO
rule 2       : FREQ=MONTHLY;COUNT=3;BYMONTHDAY=1
occurrences  : 2026-01-05, 2026-01-12, 2026-01-19, 2026-01-26, 2026-02-01, 2026-03-01, 2026-04-01

overlapping  : 2026-06-01, 2026-06-08, 2026-06-15, 2026-07-01, 2026-08-01
distinct     : True (5 occurrences, 5 distinct)

--- The parts are readable back ---
Start          : 2026-01-05 09:00
Rules          : 1 (FREQ=WEEKLY;COUNT=6;BYDAY=MO)
Dates          : 2 (2026-01-08, 2026-01-21)
ExceptionDates : 2 (2026-01-19, 2026-01-21)
```

The `overlapping` row is the load-bearing one: 2026-06-01 is both a Monday and the first of the
month, so a weekly-Monday rule and a monthly-first rule both produce it — and the stream carries it
once, which the distinct count confirms.

**APIs demonstrated.** `RecurrenceSet` constructor (start, rules, `dates`, `exceptionDates`),
`.GetOccurrences()`, `.Start` / `.Rules` / `.Dates` / `.ExceptionDates`.

## Scenario 2 — PropertyBlocks

**Intent.** Show that the text form is a *storage* format, not a display form: a set renders to the
`DTSTART` / `RRULE` / `RDATE` / `EXDATE` lines a calendar file carries, and parses back to an equal
value.

**What it does.** Renders a composed set to its property block and prints each line; parses it back
and compares by value, hash, and produced occurrences; parses a block written by hand with a
multi-valued `EXDATE`; compares a constructed set against a parsed one; and runs five malformed
blocks through the defect-reporting `TryParse`.

**What to expect.** `ToString` emits CRLF-separated property lines in the order a calendar file uses.
The parser accepts multi-valued `RDATE`/`EXDATE` lines, which is how files in the wild are written.
Equality is by value, so a constructed set equals a parsed one denoting the same schedule:

```text
--- Rendering a set to an iCalendar property block ---
ToString():
  DTSTART:20260302T143000
  RRULE:FREQ=WEEKLY;COUNT=8;BYDAY=MO,WE
  RDATE:20260320T143000
  EXDATE:20260311T143000

occurrences : 2026-03-02, 2026-03-04, 2026-03-09, 2026-03-16, 2026-03-18, 2026-03-20, 2026-03-23, 2026-03-25

--- Parsing it back ---
round trip equal    : True
hash codes match    : True
same occurrences    : True

--- Parsing a block written by hand ---
input:
  DTSTART:20260601T080000
  RRULE:FREQ=DAILY;COUNT=10
  EXDATE:20260603T080000,20260604T080000
  RDATE:20260615T080000

Rules          : 1
Dates          : 2026-06-15
ExceptionDates : 2026-06-03, 2026-06-04
occurrences    : 2026-06-01, 2026-06-02, 2026-06-05, 2026-06-06, 2026-06-07, 2026-06-08, 2026-06-09, 2026-06-10, 2026-06-15
                 (the 3rd and 4th are excluded; the 15th is added)

--- Equality is by value, not by text ---
constructed == parsed : True
occurrences           : 2026-05-04, 2026-05-06

--- Malformed text names its defect ---
  (empty)
    parsed=False A recurrence set requires a DTSTART property line.
  RRULE:FREQ=DAILY
    parsed=False A recurrence set requires a DTSTART property line.
  DTSTART:20260504T100000
    parsed=False A recurrence set requires at least one rule or explicit recurrence date.
  DTSTART:not-a-date | RRULE:FREQ=DAILY
    parsed=False The date-time value 'not-a-date' is not in a supported iCalendar DATE or DATE-TIME format.
  DTSTART:20260504T100000 | RRULE:FREQ=FORTNIGHTLY
    parsed=False The recurrence-rule component 'FREQ=FORTNIGHTLY' is not valid.
```

**APIs demonstrated.** `RecurrenceSet.ToString()`, `.Parse(string)`,
`.TryParse(string, out RecurrenceSet, out string)`, `.Equals`, `.GetHashCode()`.

## Scenario 3 — SetQueries

**Intent.** Show a realistic composed set answering the questions a calendar UI and a scheduler each
ask: "what falls in this month?" and "when is the next one?" — with exception dates handled by the
set rather than re-applied by the caller.

**What it does.** Builds a term timetable — a weekly class bounded by `UNTIL`, plus a Saturday make-up
session, minus a mid-term break and a public holiday. It enumerates the whole term, then three
one-month windows, then queries around both an excluded date and the make-up session, runs both
directions at an exact hit, and finally queries past the end of term.

**What to expect.** The windowed overload yields only what falls inside its inclusive bounds, so a
calendar view never enumerates the series and filters. The point queries operate across the whole
composition: the cancelled 2 March is skipped, and the `RDATE` make-up session on 7 March is returned
by `Next` just like a rule-produced occurrence:

```text
--- A term timetable with holidays removed ---
start        : 2026-02-02 10:00
rule         : FREQ=WEEKLY;UNTIL=20260427T100000;BYDAY=MO
make-up      : 2026-03-07
cancelled    : 2026-03-02, 2026-04-06

all sessions : 2026-02-02, 2026-02-09, 2026-02-16, 2026-02-23, 2026-03-07, 2026-03-09, 2026-03-16, 2026-03-23, 2026-03-30, 2026-04-13, 2026-04-20, 2026-04-27

--- Windowed enumeration: one month at a time ---
  February : 2026-02-02, 2026-02-09, 2026-02-16, 2026-02-23
  March    : 2026-03-07, 2026-03-09, 2026-03-16, 2026-03-23, 2026-03-30
  April    : 2026-04-13, 2026-04-20, 2026-04-27

--- Point queries skip exception dates ---
query        : 2026-02-24 10:00
next         : 2026-03-07 10:00
               (2026-03-02 is an EXDATE, so it is skipped)
query        : 2026-03-05 10:00
next         : 2026-03-07 10:00 (the RDATE make-up session)

--- Both directions, with the inclusive flag ---
query           : 2026-03-16 10:00 (an occurrence)
next  exclusive : 2026-03-23 10:00
next  inclusive : 2026-03-16 10:00
prev  exclusive : 2026-03-09 10:00
prev  inclusive : 2026-03-16 10:00

--- Past the end of the term ---
query        : 2026-09-01 10:00
next         : (none)
previous     : 2026-04-27 10:00 (the last session of term)
```

**APIs demonstrated.** `RecurrenceSet.GetOccurrences()` and `GetOccurrences(DateTime, DateTime)`,
`.GetNextOccurrence(DateTime, bool)`, `.GetPreviousOccurrence(DateTime, bool)`.

## Layout

```text
Bodu.Globalization.Recurrence.Samples.RecurrenceSets/
  Program.cs                          # runs the scenarios in order
  Scenarios/SetComposition.cs
  Scenarios/PropertyBlocks.cs
  Scenarios/SetQueries.cs
```

## Related

- `Bodu.Globalization.Recurrence.Samples.RecurrenceRules` — the `RRULE` form a set composes.
- `Bodu.Globalization.Recurrence.Samples.SchedulingHost` — a set behind the same adapter as the
  other three forms.

> `EXRULE` is not modelled: a set composes `RRULE`, `RDATE`, and `EXDATE` only. RFC 5545 deprecated
> `EXRULE`, and the exclusions it expressed are reachable through `EXDATE`.
