# Bodu.Globalization.Recurrence.Samples.SchedulingHost

The integrating sample: how a host actually consumes `Bodu.Globalization.Recurrence`. Three
scenarios cover putting all four schedule forms behind one adapter, validating a configuration block
with defect messages an operator can act on, and the consequence of the package's purity contract —
a missed-run catch-up loop that is exactly reproducible because the host owns time.

Everything runs offline with fixed inputs, formatted with the invariant culture — deterministic
output every run.

```bash
dotnet run --project samples/Globalization.Recurrence/Bodu.Globalization.Recurrence.Samples.SchedulingHost
```

NuGet consumers: `dotnet add package Bodu.Globalization.Recurrence`

## The adapter

`Schedule.cs` is the sample's own type, not part of the package. The library deliberately does not
ship this abstraction: the four forms differ in what they need to answer a query — a `RecurrenceRule`
and an `AnchoredInterval` need a series origin, a `CronExpression` and a `RecurrenceSet` do not — and
collapsing that difference is a hosting decision rather than a library one.

What the package *does* guarantee is that every form answers `GetNextOccurrence` and
`GetPreviousOccurrence` with the same inclusive-flag semantics. That is what makes an adapter this
small possible: it captures the origin once at configuration time, and the query surface reduces to
a single instant regardless of form.

## Scenario 1 — UnifiedSchedules

**Intent.** Show the payoff of the uniform point-query surface: after configuration, nothing
downstream branches on which form a schedule came from.

**What it does.** Configures seven schedules across all four forms, then queries them all
identically — previous and next from one instant — and tabulates the result. It orders every
configured job by next occurrence, which is an ordinary `OrderBy` once the forms are uniform. It
closes by querying each form at an instant it actually fires, with the inclusive flag both ways.

**What to expect.** One table across four forms, with no per-form special casing in the calling code.
The inclusive flag means the same thing everywhere: exclusive steps past an exact hit, inclusive
returns it:

```text
--- One adapter over four schedule forms ---
origin : 2026-01-01 00:00   query instant : 2026-03-10 14:32

kind      purpose                previous          next             
--------- ---------------------- ----------------- -----------------
cron      nightly backup         2026-03-10 02:00  2026-03-11 02:00 
cron      health probe           2026-03-10 14:30  2026-03-10 14:45 
rrule     weekly report          2026-03-09 00:00  2026-03-16 00:00 
rrule     month-end close        2026-02-27 00:00  2026-03-27 00:00 
interval  cache sweep            2026-03-10 12:00  2026-03-10 18:00 
interval  retention scan         2026-03-05 00:00  2026-03-12 00:00 
set       standup (one skipped)  2026-03-09 09:00  2026-03-23 09:00 

--- Which schedule fires first? ---
  2026-03-10 14:45  cron      health probe
  2026-03-10 18:00  interval  cache sweep
  2026-03-11 02:00  cron      nightly backup
  2026-03-12 00:00  interval  retention scan
  2026-03-16 00:00  rrule     weekly report
  2026-03-23 09:00  set       standup (one skipped)
  2026-03-27 00:00  rrule     month-end close

--- The inclusive flag means the same thing everywhere ---
kind      query             next(excl)        next(incl)       
--------- ----------------- ----------------- -----------------
cron      2026-03-11 02:00  2026-03-12 02:00  2026-03-11 02:00 
rrule     2026-03-16 00:00  2026-03-23 00:00  2026-03-16 00:00 
interval  2026-03-10 12:00  2026-03-10 18:00  2026-03-10 12:00 
set       2026-03-16 09:00  2026-03-23 09:00  2026-03-16 09:00 
```

The `set` row's next occurrence jumps a fortnight because its `EXDATE` removes 16 March — the
composition is applied by the set, not by the caller.

**APIs demonstrated.** `RecurrenceRule` / `CronExpression` / `AnchoredInterval` / `RecurrenceSet`
`TryParse` and their `GetNextOccurrence` / `GetPreviousOccurrence` overloads.

## Scenario 2 — ConfigurationValidation

**Intent.** Show startup validation across mixed forms, and then the second tier of validation most
hosts forget: a schedule can parse cleanly and still never fire.

**What it does.** Runs fourteen configured jobs — six valid, eight invalid across every form plus an
unknown kind — through the adapter, collecting accepted and rejected separately. It then takes four
schedules that all parse and probes each for a first occurrence.

**What to expect.** Every rejection names the offending token, so an operator can fix the file
without reading a specification. The Quartz `L` is refused explicitly rather than silently ignored.
And the second tier catches what syntax checking cannot — an unreachable date and an expired `UNTIL`
both parse, then never fire:

```text
accepted : 6
  [ ok ] backup.nightly     cron      next 2026-03-11 02:00
  [ ok ] backup.weekly      cron      next 2026-03-15 03:00
  [ ok ] probe.health       interval  next 2026-03-10 14:32
  [ ok ] report.weekly      rrule     next 2026-03-16 08:00
  [ ok ] close.monthly      rrule     next 2026-03-27 00:00
  [ ok ] standup            set       next 2026-03-11 09:00

rejected : 8
  [FAIL] bad.cronRange      cron      '0 25 * * *'
         The cron field '25' is not valid.
  [FAIL] bad.cronName       cron      '0 8 * * MONDAY'
         The cron field 'MONDAY' is not valid.
  [FAIL] bad.quartz         cron      '0 0 L * *'
         The cron token 'L' is not supported; the Quartz L, W, and # extensions are a planned follow-on.
  [FAIL] bad.duration       interval  'PT5'
         The duration component '5' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S.
  [FAIL] bad.negative       interval  '-P1D'
         The duration must be greater than zero; signed and zero durations are not valid intervals.
  [FAIL] bad.freq           rrule     'FREQ=FORTNIGHTLY'
         The recurrence-rule component 'FREQ=FORTNIGHTLY' is not valid.
  [FAIL] bad.setNoStart     set       'RRULE:FREQ=DAILY'
         A recurrence set requires a DTSTART property line.
  [FAIL] bad.kind           quartz    '0 0 12 * * ?'
         Unknown schedule kind 'quartz'.

--- A schedule that parses but can never fire ---
  [WARN] bad.feb30       '0 0 30 2 *' parses, but has no occurrence after 2026-03-10 14:32
  [WARN] bad.sep31       '0 0 31 9 *' parses, but has no occurrence after 2026-03-10 14:32
  [WARN] expired         'FREQ=DAILY;UNTIL=20200101T000000' parses, but has no occurrence after 2026-03-10 14:32
  [ ok ] fine            '0 0 29 2 *' next 2028-02-29 00:00
```

**APIs demonstrated.** The four `TryParse(…, out …, out string)` defect-reporting overloads, and
`GetNextOccurrence` returning `null` as a reachability probe.

## Scenario 3 — CatchUpAndPurity

**Intent.** Show what the purity contract buys a host. Because no type in the package reads a wall
clock or resolves a time zone, the host supplies both ends of every window — which is what makes a
missed-run replay exactly reproducible, and what makes daylight saving a decision the host can see.

**What it does.** Replays a downtime window across three jobs of different forms, listing exactly
which runs were missed; uses the backward query to decide whether to fire immediately on startup;
states the purity contract and what it implies; shows the `DateTimeOffset` overloads preserving their
offset; and contrasts a wall-clock cron schedule against an elapsed-time interval across a
daylight-saving transition.

**What to expect.** The catch-up list is derived, not guessed: walking forward from the last recorded
run to the resume instant enumerates precisely the missed occurrences. The "fire immediately"
decision is one backward query. The offset is preserved rather than converted, because the library
has no zone database to convert with:

```text
--- Catching up after downtime ---
last recorded run : 2026-03-09 18:00
resumed at        : 2026-03-10 14:32

backup.nightly   (cron     '0 2 * * *')
  missed 1 run(s): 03-10 02:00
  next after resume : 2026-03-11 02:00
probe.health     (interval 'PT6H')
  missed 3 run(s): 03-10 00:00, 03-10 06:00, 03-10 12:00
  next after resume : 2026-03-10 18:00
report.weekly    (rrule    'FREQ=WEEKLY;BYDAY=MO')
  missed 0 run(s): (none)
  next after resume : 2026-03-16 00:00

--- Was a run due when we stopped? ---
most recent scheduled run : 2026-03-10 02:00
already ran?              : False
=> fire immediately       : True

--- Offsets ride along; the wall clock is what recurs ---
  query 2026-03-10 14:32 +00:00 -> next 2026-03-11 09:00 +00:00
  query 2026-03-10 14:32 +10:00 -> next 2026-03-11 09:00 +10:00
  (09:00 local in each case -- the offset is preserved, never converted)
```

The scenario also prints the contract itself and the recommended daylight-saving pattern:

```text
--- Daylight saving is a hosting decision ---
  A cron expression names a wall-clock time, so '0 2 * * *' means 02:00 local on
  every calendar day -- including the day 02:00 does not exist, and the day it
  happens twice. This library has no zone database and so takes no position on
  either case.

  A host that needs zone-correct firing converts at the boundary:
    1. ask for the next wall-clock occurrence here;
    2. resolve it against its zone with TimeZoneInfo (deciding skipped and
       ambiguous times to its own policy);
    3. wait on the resulting instant.

  An anchored interval has no such problem: it is exact elapsed time, so 'PT6H'
  is six hours regardless of what the local calendar does.
```

**APIs demonstrated.** `GetNextOccurrence` / `GetPreviousOccurrence` across forms,
`CronExpression.GetNextOccurrence(DateTimeOffset, bool)`,
`AnchoredInterval.GetOccurrences(DateTime)`.

## Layout

```text
Bodu.Globalization.Recurrence.Samples.SchedulingHost/
  Program.cs                          # runs the scenarios in order
  Schedule.cs                         # the host-side adapter over all four forms
  Scenarios/UnifiedSchedules.cs
  Scenarios/ConfigurationValidation.cs
  Scenarios/CatchUpAndPurity.cs
```

## Related

- `Bodu.Globalization.Recurrence.Samples.RecurrenceRules` / `.CronExpressions` /
  `.AnchoredIntervals` / `.RecurrenceSets` — each form in depth.
- `docs/guides/recurrence/index.md` — the offset and DST posture stated as a contract.

> The package ships **no** timer, poller, or due-state tracking, and that is deliberate: those need a
> clock, and a clock is what makes scheduling code untestable. This sample's catch-up loop is the
> shape a host supplies instead — and it produces the same output on every machine, in every locale,
> which is why it can be a CI smoke test.
