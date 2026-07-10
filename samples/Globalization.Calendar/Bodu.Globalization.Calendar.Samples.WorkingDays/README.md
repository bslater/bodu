# Bodu.Globalization.Calendar.Samples.WorkingDays

Business-day arithmetic over a notable-date service: the `Bodu.Extensions` date extensions turn
holiday knowledge into the answers payroll, settlement, SLA, and fiscal code actually needs. One
AU service from the embedded data pack feeds every scenario.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.WorkingDays
```

## Scenarios

### WorkingDayChecks (`Scenarios/WorkingDayChecks.cs`)

**Intent.** Establish the model: a *working day* is a day that is neither a weekend day (per the
working-week pattern) nor a non-working notable date (per the service's rules). Two sources, one
predicate.

**What it does.** Classifies every day of the 2024 Anzac Day week with `IsWorkingDay` (both
sources) and `IsWeekend` (week shape only).

**What to expect.**

```
  2024-04-22 (Monday   ) working
  2024-04-23 (Tuesday  ) working
  2024-04-24 (Wednesday) working
  2024-04-25 (Thursday ) public holiday
  2024-04-26 (Friday   ) working
  2024-04-27 (Saturday ) weekend
  2024-04-28 (Sunday   ) weekend
```

Thursday is excluded by the holiday rules, the weekend by the week pattern — the classification
shows which source did the excluding.

**APIs demonstrated.** `DateOnly.IsWorkingDay(service, territory)`,
`DateOnly.IsWeekend(WeekPattern)`.

### PaymentScheduling (`Scenarios/PaymentScheduling.cs`)

**Intent.** The arithmetic settlement systems live on: add N working days, find the next valid
banking day, and snap a contractual date that landed on a holiday — forward, backward, or
verify it is already valid.

**What it does.** Computes T+2 settlement from the day before Anzac Day, the strict next working
day, and three snaps (forward, backward, and a no-op on an already-valid date).

**What to expect.**

```
Trade 2024-04-24 (Wed), T+2 settle: 2024-04-29 (Monday)
Next working day after 04-24: 2024-04-26 (Friday)
Contractual 04-25 snap forward : 2024-04-26
Contractual 04-25 snap backward: 2024-04-24
Valid date snap (no-op)        : 2024-04-23
```

T+2 from Wednesday lands on *Monday* — the count skipped the Anzac Day Thursday and the
weekend, which is the whole point versus naive `AddDays(2)`. The snap pair is the classic
contract-clause split: "next banking day" versus "no later than".

**APIs demonstrated.** `AddWorkingDays`, `NextWorkingDay`, `SnapToWorkingDay`,
`SnapToWorkingDayBackward`.

### RangeCounting (`Scenarios/RangeCounting.cs`)

**Intent.** Duration and SLA questions: how many business days between two dates, and which
ones are they?

**What it does.** Counts April 2024's working days and lazily enumerates the working days of
Anzac week.

**What to expect.**

```
Working days in April 2024 (AU): 20
Anzac week working days: 04-22, 04-23, 04-24, 04-26
```

April 2024 has 30 days − 8 weekend days − 2 weekday holidays (Easter Monday 04-01 and Anzac Day
04-25) = 20. The enumeration shows the 25th missing from its week.

**APIs demonstrated.** `WorkingDaysBetween`, `EnumerateWorkingDays`.

### FiscalAndWeekPatterns (`Scenarios/FiscalAndWeekPatterns.cs`)

**Intent.** Two orthogonal knobs: fiscal-period boundaries (the first/last *working* day of the
fiscal year or quarter containing a date) and the `WeekPattern` override that re-bases every
working-day answer for jurisdictions or rosters whose week is not Monday–Friday.

**What it does.** Finds AU fiscal-year boundaries (July start) around 2024-08-15, then contrasts
a Sunday–Thursday working week against the default on the same dates and the same `+3 working
days` calculation.

**What to expect.**

```
For 2024-08-15, AU fiscal year (starts July):
  first working day of FY : 2024-07-01 (Monday)
  last working day of FY  : 2025-06-30 (Monday)
  first working day of Q  : 2024-07-01 (Monday)
Default week   : Friday working: True, Sunday working: False
Sun-Thu week   : Friday working: False, Sunday working: True
Thu 08-15 + 3 working days: default 2024-08-20 (Tuesday), Sun-Thu 2024-08-20 (Tuesday)
```

The week-pattern lines flip Friday and Sunday exactly as a Gulf-region roster would. The final
line is a genuine coincidence worth understanding: both weeks count three working days from
Thursday to the same Tuesday — via Fri→Mon→Tue in the default week and Sun→Mon→Tue in the
Sun–Thu week — different paths, same landing day.

**APIs demonstrated.** `FirstWorkingDayOfFiscalYear` / `LastWorkingDayOfFiscalYear` /
`FirstWorkingDayOfFiscalQuarter` (with `fiscalYearStartMonth`), the `WeekPattern` parameter on
`IsWorkingDay` / `AddWorkingDays`, `WeekPattern` construction from `DayOfWeek` values.

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar.AsiaPacific   # engine + extensions transitively
```
