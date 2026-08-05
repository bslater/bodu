# Bodu.Globalization.Recurrence.Samples.CronExpressions

`CronExpression` — the Vixie cron form. Four scenarios cover the five-field layout and the `@`
macros, the optional-seconds six-field layout and canonical text, the two Vixie semantics that
separate this dialect from the Quartz-flavoured cron most .NET libraries implement, and the two
failure surfaces: schedules that can never fire, and text that will not parse.

Everything runs offline with fixed inputs, formatted with the invariant culture — deterministic
output every run.

```bash
dotnet run --project samples/Globalization.Recurrence/Bodu.Globalization.Recurrence.Samples.CronExpressions
```

NuGet consumers: `dotnet add package Bodu.Globalization.Recurrence`

## Scenario 1 — CronBasics

**Intent.** Show the five-field layout and the field grammar, and establish the shape of the query
surface: unlike a recurrence rule, a cron expression has no series origin at all — it is a predicate
over instants, so the query instant is the only input `GetNextOccurrence` needs.

**What it does.** Parses a catalogue of ten expressions covering a literal, a list, a range, a
stepped star, and the three-letter month and weekday names, reporting each one's next occurrence
after a fixed Tuesday afternoon. It then parses all seven `crontab(5)` macros and asserts each equals
its long form, demonstrates the inclusive flag in both directions at an exact hit, and walks a
weekday schedule six occurrences forward.

**What to expect.** Every macro compares equal to its long form, because a macro is just a spelling.
`@weekly` is Sunday. The inclusive flag is what decides whether an exact hit is returned. The walk
skips the weekend, which is the `1-5` weekday range doing its job:

```text
--- CronExpression: the five-field Vixie layout ---
fields: minute hour day-of-month month day-of-week

query instant : 2026-03-10 14:32:00 (Tuesday)

* * * * *      every minute             next: 2026-03-10 14:33:00
0 * * * *      on the hour              next: 2026-03-10 15:00:00
30 9 * * *     09:30 daily              next: 2026-03-11 09:30:00
0 9,17 * * *   09:00 and 17:00          next: 2026-03-10 17:00:00
0 9-11 * * *   09:00, 10:00 and 11:00   next: 2026-03-11 09:00:00
*/15 * * * *   every quarter hour       next: 2026-03-10 14:45:00
0 0 1 * *      midnight on the 1st      next: 2026-04-01 00:00:00
0 8 * * MON    Mondays at 08:00         next: 2026-03-16 08:00:00
0 8 * * 1-5    weekdays at 08:00        next: 2026-03-11 08:00:00
0 0 1 JAN *    New Year's Day           next: 2027-01-01 00:00:00

--- The @ macros ---
@hourly     == 0 * * * *   : True  next: 2026-03-10 15:00:00
@daily      == 0 0 * * *   : True  next: 2026-03-11 00:00:00
@midnight   == 0 0 * * *   : True  next: 2026-03-11 00:00:00
@weekly     == 0 0 * * 0   : True  next: 2026-03-15 00:00:00
@monthly    == 0 0 1 * *   : True  next: 2026-04-01 00:00:00
@yearly     == 0 0 1 1 *   : True  next: 2027-01-01 00:00:00
@annually   == 0 0 1 1 *   : True  next: 2027-01-01 00:00:00

--- Next and previous, with the inclusive flag ---
query          : 2026-03-10 14:00:00 (an exact match for '0 * * * *')
next  exclusive: 2026-03-10 15:00:00
next  inclusive: 2026-03-10 14:00:00
prev  exclusive: 2026-03-10 13:00:00
prev  inclusive: 2026-03-10 14:00:00

--- Walking a schedule ---
'15 9 * * 1-5' (weekdays 09:15) from 2026-03-10 14:32:00:
  2026-03-11 09:15:00 Wednesday
  2026-03-12 09:15:00 Thursday
  2026-03-13 09:15:00 Friday
  2026-03-16 09:15:00 Monday
  2026-03-17 09:15:00 Tuesday
  2026-03-18 09:15:00 Wednesday
```

**APIs demonstrated.** `CronExpression.Parse(string)`, `.GetNextOccurrence(DateTime, bool)`,
`.GetPreviousOccurrence(DateTime, bool)`, `.Equals`.

## Scenario 2 — SecondsAndFormats

**Intent.** Show the six-field seconds layout selected by `CronFormat`, the difference between
inferring that layout and stating it, and the canonical text that makes equality decidable by
schedule rather than by spelling.

**What it does.** Parses six expressions under `CronFormat.WithSeconds`; contrasts the format-less
overloads (which infer the layout from the field count) against the format-taking ones (which enforce
it); renders seven expressions to canonical text; and compares seven pairs for equality and hash
agreement, closing with a canonical-text round trip.

**What to expect.** Inference is the convenient default for text a human typed; stating the format is
what stops a configuration value changing meaning by gaining or losing a field. Canonical rendering
expands ranges, steps and names into sorted value lists but leaves a whole-range field as `*`, so
different spellings of one schedule render identically and compare equal:

```text
--- Inferring the format, or stating it ---
  '0 12 * * MON'     inferred    -> Standard
  '0 0 12 * * MON'   inferred    -> WithSeconds

  '0 12 * * MON'     as Standard    : next 2026-03-16 12:00:00
  '0 12 * * MON'     as WithSeconds : A cron expression must contain the number of fields required by the specified format.
  '0 0 12 * * MON'   as Standard    : A cron expression must contain the number of fields required by the specified format.
  '0 0 12 * * MON'   as WithSeconds : next 2026-03-16 12:00:00
  Format property                : WithSeconds -> '0 0 12 * * 1'

--- Canonical text ---
* * * * *          -> * * * * *                    (already canonical)
0-3 * * * *        -> 0,1,2,3 * * * *              (a range expands to a list)
*/20 * * * *       -> 0,20,40 * * * *              (a step expands to the values it hits)
MON * * * *        -> (rejected)                   (not a name field -- minute 'MON' would be invalid)
0 * * * MON        -> 0 * * * 1                    (a weekday name becomes its number)
0 0 * JAN,JUL *    -> 0 0 * 1,7 *                  (month names become numbers)
0 0 * * 1-5        -> 0 0 * * 1,2,3,4,5            (a weekday range expands)

--- Equality is by schedule, not by spelling ---
* * * * *      == 0-59 * * * *     : True  (hash match: True)
* * * * *      == 0-59/1 * * * *   : True  (hash match: True)
0 8 * * 1-5    == 0 8 * * MON-FRI  : True  (hash match: True)
*/30 * * * *   == 0,30 * * * *     : True  (hash match: True)
@hourly        == 0 * * * *        : True  (hash match: True)
0 0 * * 0      == 0 0 * * 7        : True  (hash match: True)
1 1 1 1 1      == 2 1 1 1 1        : False (hash match: False)

'0 8-10 * JAN,JUL MON-WED' -> '0 8,9,10 * 1,7 1,2,3'
re-parses equal            : True
```

The last equality row is a *non*-match, included as the control: two different schedules must
compare unequal, and they hash differently too — a property this sample surfaced as a defect (the
hash used to mix only each field's cardinality, so every single-valued schedule collided) and which
is now guarded by `CronExpressionTests.GetHashCode_WhenSchedulesDifferOnlyInSelectedValues_ShouldDiffer`.

**APIs demonstrated.** `CronExpression.Parse(string, CronFormat)`,
`TryParse(string, CronFormat, out CronExpression, out string)`, `CronFormat.Standard` /
`.WithSeconds`, `.Format`, `.ToString()`, `.Equals`, `.GetHashCode()`.

## Scenario 3 — VixieSemantics

**Intent.** Pin the two behaviours where Vixie and Quartz genuinely disagree, so a reader porting a
schedule from another library knows exactly what changes.

**What it does.** Walks `0 0 1 * MON` to show the day-field union; contrasts `*/2` against the
set-equivalent `1-31/2` alongside a `sat` weekday field; shows what happens when only one day field
is restricted (including why "Friday the 13th" cannot be written in Vixie cron); proves six
oversized-step expressions equal their single-value equivalents; and shows Sunday accepted as 0, 7,
and `SUN`.

**What to expect.** When **both** day fields are restricted, Vixie takes their **union** — an instant
matches if *either* field matches. And "restricted" is decided by the field's **leading character**,
not by the set of days it denotes, so `*/2` and `1-31/2` select different branches despite denoting
the same days:

```text
--- The day-of-month / day-of-week union rule ---

'0 0 1 * MON' -- both day fields restricted, so UNION:
  2026-01-05, 2026-01-12, 2026-01-19, 2026-01-26, 2026-02-01
  (the 1st of each month OR any Monday -- an AND engine would give 2026-06-01)

'*/2' and '1-31/2' denote the same days, but select different branches:
  0 16 */2    * sat -> 2023-05-13, 2023-05-27, 2023-06-03, 2023-06-17
                       (leading '*' -> unrestricted -> INTERSECTION: odd-numbered Saturdays)
  0 16 1-31/2 * sat -> 2023-05-03, 2023-05-05, 2023-05-06, 2023-05-07
                       (leading digit -> restricted -> UNION: odd days and Saturdays)

  This is not a rationalizable rule -- it is what cronie's src/entry.c does:
      if (ch == '*') e->flags |= DOM_STAR;
  croniter carries the same pair as test_dom_dow_vixie_cron_bug.

--- Only one restricted day field: no union to take ---
  0 0 13 * *   (13th of the month) -> 2026-01-13, 2026-02-13, 2026-03-13, 2026-04-13
  0 0 *  * FRI (every Friday)      -> 2026-01-02, 2026-01-09, 2026-01-16, 2026-01-23
  0 0 13 * FRI (NOT Friday the 13th) -> 2026-01-02, 2026-01-09, 2026-01-13, 2026-01-16

--- A step wider than its range selects the range start ---
  */60 * * * *   -> 0 * * * *      == '0 * * * *' : True
  1/60 * * * *   -> 1 * * * *      == '1 * * * *' : True
  * 1/24 * * *   -> * 1 * * *      == '* 1 * * *' : True
  * * 1/32 * *   -> * * 1 * *      == '* * 1 * *' : True
  * * * 1/13 *   -> * * * 1 *      == '* * * 1 *' : True
  * * * * 1/8    -> * * * * 1      == '* * * * 1' : True

--- Sunday is both 0 and 7 ---
  '0 0 * * 0' == '0 0 * * 7'   : True
  '0 0 * * 0' == '0 0 * * SUN' : True
  canonical                    : 0 0 * * 0
```

The oversized-step rows are the second divergence: cronie only *warns* ("Step size %i higher than
possible maximum") and then runs `for (i = low; i <= high; i += step)`, which sets exactly one bit.
Libraries that reject the same input — Cronos among them — are stricter than the dialect this
implements.

**APIs demonstrated.** `CronExpression.Parse`, `.GetNextOccurrence(DateTime, bool)`, `.Equals`,
`.ToString()`.

## Scenario 4 — UnreachableAndDefects

**Intent.** Show the two failure surfaces separately, because they need different handling: an
expression that parses cleanly and can never fire, versus text that will not parse at all.

**What it does.** Parses six expressions selecting a date that exists in no year and asks each for a
next occurrence; contrasts 29 February, which is reachable but rare; runs fourteen malformed inputs
through the defect-reporting `TryParse`; and closes with the shape a host actually uses — validating
a small configuration block and reporting each rejection against its key.

**What to expect.** An unreachable schedule reports *no occurrence* rather than searching forever or
throwing. Every rejection names the offending token, and the Quartz extensions are refused
explicitly rather than silently ignored — which matters, because silently dropping an `L` would
change the schedule rather than reject it:

```text
--- Valid syntax, unreachable schedule ---
  * * 30 2 *     next: (never)
  * * 31 2 *     next: (never)
  * * 31 4 *     next: (never)
  * * 31 6 *     next: (never)
  * * 31 9 *     next: (never)
  * * 31 11 *    next: (never)
  0 0 29 2 *     next: 2028-02-29 00:00:00 (reachable, but only in leap years)

--- Malformed text: TryParse names the defect ---
  (empty)            parsed=False  The cron-expression text is empty or contains only white space.
  '* * * *'          parsed=False  A cron expression must contain the number of fields required by the specified format.
  '* * * * * * *'    parsed=False  A cron expression must contain the number of fields required by the specified format.
  '60 * * * *'       parsed=False  The cron field '60' is not valid.
  '* 24 * * *'       parsed=False  The cron field '24' is not valid.
  '* * 32 * *'       parsed=False  The cron field '32' is not valid.
  '* * * 13 *'       parsed=False  The cron field '13' is not valid.
  '* * * * 8'        parsed=False  The cron field '8' is not valid.
  '5-1 * * * *'      parsed=False  The cron field '5-1' is not valid.
  '1/0 * * * *'      parsed=False  The cron field '1/0' is not valid.
  '* * * * MON#1'    parsed=False  The cron token 'MON#1' is not supported; the Quartz L, W, and # extensions are a planned follow-on.
  '* * L * *'        parsed=False  The cron token 'L' is not supported; the Quartz L, W, and # extensions are a planned follow-on.
  '* * * * ?'        parsed=False  The cron token '?' is not supported; the Quartz L, W, and # extensions are a planned follow-on.
  '@every_minute'    parsed=False  The cron macro '@every_minute' is not recognized, or a macro was supplied where the six-field layout was required.

--- Validating a configuration block ---
  [ ok ] nightly-backup  '0 2 * * *' -> next 2026-01-01 02:00:00
  [ ok ] hourly-sync     '@hourly' -> next 2026-01-01 01:00:00
  [ ok ] weekday-report  '0 8 * * 1-5' -> next 2026-01-01 08:00:00
  [FAIL] typo            '0 8 * * MONDAY' -> The cron field 'MONDAY' is not valid.
  [FAIL] out-of-range    '0 25 * * *' -> The cron field '25' is not valid.
```

**APIs demonstrated.** `CronExpression.TryParse(string, out CronExpression, out string)`,
`.GetNextOccurrence(DateTime, bool)` returning `null`.

## Layout

```text
Bodu.Globalization.Recurrence.Samples.CronExpressions/
  Program.cs                          # runs the scenarios in order
  Scenarios/CronBasics.cs
  Scenarios/SecondsAndFormats.cs
  Scenarios/VixieSemantics.cs
  Scenarios/UnreachableAndDefects.cs
```

## Related

- `Bodu.Globalization.Recurrence.Samples.RecurrenceRules` — the RFC 5545 `RRULE` form.
- `Bodu.Globalization.Recurrence.Samples.SchedulingHost` — all four forms behind one adapter.
- `corpus/recurrence/README.md` — the Cronos-derived vector table these semantics are reconciled
  against, and every divergence it records.
