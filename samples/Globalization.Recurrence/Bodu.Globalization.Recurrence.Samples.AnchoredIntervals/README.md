# Bodu.Globalization.Recurrence.Samples.AnchoredIntervals

`AnchoredInterval` — the calendar-free recurrence form: a fixed spacing with no calendar semantics
at all. Three scenarios cover construction and canonical duration text, the anchor-per-query design
that lets one instance serve many series, and the boundary of the RFC 5545 §3.3.6 duration grammar.

Everything runs offline with fixed inputs, formatted with the invariant culture — deterministic
output every run.

```bash
dotnet run --project samples/Globalization.Recurrence/Bodu.Globalization.Recurrence.Samples.AnchoredIntervals
```

NuGet consumers: `dotnet add package Bodu.Globalization.Recurrence`

## Scenario 1 — IntervalBasics

**Intent.** Show what the type stores — only the interval — and the canonical duration text it
renders back to, which is not always the spelling it was given.

**What it does.** Builds the same interval from a `TimeSpan` and from `PT6H` text and compares them;
parses nine duration forms; renders eight inputs to canonical text; and compares five pairs for
equality and hash agreement before a round trip.

**What to expect.** Canonical rendering normalizes: sixty minutes and 3600 seconds both become
`PT1H`, and a whole number of weeks renders in the weeks form (`P7D` → `P1W`) while `P10D` does not.
Equality is by elapsed time, so every spelling of one duration compares equal:

```text
--- AnchoredInterval: construction ---
new AnchoredInterval(TimeSpan.FromHours(6)) : PT6H
AnchoredInterval.Parse("PT6H")              : PT6H
Interval property                           : 06:00:00
equal                                       : True

--- The RFC 5545 duration grammar ---
  PT30S      00:00:30         thirty seconds
  PT15M      00:15:00         fifteen minutes
  PT1H       01:00:00         one hour
  PT1H30M    01:30:00         ninety minutes
  P1D        1.00:00:00       one day
  P1DT12H    1.12:00:00       a day and a half
  P1W        7.00:00:00       one week
  P2W        14.00:00:00      a fortnight
  PT0.5H     (rejected)       invalid -- components are whole numbers

--- Canonical duration text ---
  PT60M        -> PT1H         (sixty minutes normalizes to an hour)
  PT3600S      -> PT1H         (and so does 3600 seconds)
  PT90M        -> PT1H30M      (ninety minutes is an hour and a half)
  P7D          -> P1W          (seven days is a week)
  P14D         -> P2W          (and fourteen is a fortnight)
  P10D         -> P10D         (ten days is not a whole number of weeks)
  P1DT2H3M4S   -> P1DT2H3M4S   (mixed components are preserved)
  +PT4H        -> PT4H         (an explicit plus sign is the positive form)

--- Equality and round-tripping ---
  PT1H       == PT60M      : True  (hash match: True)
  PT1H       == PT3600S    : True  (hash match: True)
  P1W        == P7D        : True  (hash match: True)
  P1DT12H    == PT36H      : True  (hash match: True)
  PT1H       == PT2H       : False (hash match: False)
  round trip 'P1DT2H3M4S' : True
```

**APIs demonstrated.** `AnchoredInterval` constructor, `.Parse`, `.TryParse`, `.Interval`,
`.ToString()`, `.Equals`, `.GetHashCode()`.

## Scenario 2 — AnchoredQueries

**Intent.** Show the design decision the type is named for: the anchor that positions the grid is
supplied **per query**, not captured by the instance. That is what lets one configured interval
answer for every job that uses it.

**What it does.** Enumerates the first five occurrences from an anchor; queries the same instance
against three different anchors; runs next and previous at an exact grid point with the inclusive
flag both ways; queries an instant five years from its anchor; enumerates a window; and shows the
`DateTimeOffset` overload preserving the anchor's offset.

**What to expect.** The anchor itself is **not** an occurrence — the series starts one interval later
— which is what makes an anchor a natural "last run" marker. Looking back before the first occurrence
therefore has no answer. A query five years from its anchor lands on the grid exactly, because the
position is computed arithmetically rather than by stepping:

```text
--- Occurrences fall at anchor + k x interval, for k >= 1 ---
interval : PT6H
anchor   : 2026-04-01 00:00
first 5  : 04-01 06:00, 04-01 12:00, 04-01 18:00, 04-02 00:00, 04-02 06:00

--- One interval, many anchors ---
  anchor 2026-04-01 00:00 -> 04-01 06:00, 04-01 12:00, 04-01 18:00
  anchor 2026-04-01 02:30 -> 04-01 08:30, 04-01 14:30, 04-01 20:30
  anchor 2026-04-01 17:45 -> 04-01 23:45, 04-02 05:45, 04-02 11:45

--- Next and previous, with the inclusive flag ---
anchor          : 2026-04-01 00:00
query           : 2026-04-01 12:00 (exactly anchor + 2 intervals)
next  exclusive : 2026-04-01 18:00
next  inclusive : 2026-04-01 12:00
prev  exclusive : 2026-04-01 06:00
prev  inclusive : 2026-04-01 12:00
prev before the first occurrence : (none)

--- A query far from the anchor ---
anchor          : 2026-04-01 00:00
query           : 2031-07-04 09:17 (over five years later)
next            : 2031-07-04 12:00
previous        : 2031-07-04 06:00

--- Windowed enumeration ---
P1D anchored 2026-04-01 00:00, window 2026-04-10 00:00 .. 2026-04-14 00:00:
  04-10 00:00, 04-11 00:00, 04-12 00:00, 04-13 00:00, 04-14 00:00

--- DateTimeOffset: the offset rides along unchanged ---
anchor   : 2026-04-01 00:00 +10:00
  2026-04-01 06:00 +10:00
  2026-04-01 12:00 +10:00
  2026-04-01 18:00 +10:00
```

**APIs demonstrated.** `AnchoredInterval.GetOccurrences(DateTime)` and
`GetOccurrences(DateTime, DateTime, DateTime)`, `GetNextOccurrence(DateTime, DateTime, bool)`,
`GetPreviousOccurrence(DateTime, DateTime, bool)`, and the `DateTimeOffset` overloads.

## Scenario 3 — DurationGrammar

**Intent.** Map the boundary of the duration grammar precisely, and show that every rejection names
the offending token rather than reporting a generic failure.

**What it does.** Parses ten accepted forms covering the whole production including the explicit `+`
sign and lowercase input; runs fourteen rejected forms and prints each defect message; shows `Parse`
throwing the same message `TryParse` reports; and closes with a configuration-validation loop.

**What to expect.** The rejections are the interesting half. `P1DT` is rejected because a time
designator must be followed by at least one component — a case several libraries accept. A negative
duration is rejected on *meaning* rather than syntax: it parses as a valid duration but is not a
valid interval:

```text
--- Accepted duration text ---
  P1W            -> P1W          (weeks)
  P3D            -> P3D          (days)
  PT2H           -> PT2H         (hours)
  PT30M          -> PT30M        (minutes)
  PT45S          -> PT45S        (seconds)
  P1DT2H         -> P1DT2H       (days and hours)
  P1DT2H3M4S     -> P1DT2H3M4S   (every time component)
  PT1H30M        -> PT1H30M      (hours and minutes)
  +PT4H          -> PT4H         (an explicit positive sign)
  p1w            -> P1W          (the grammar is case-insensitive)

--- Rejected duration text ---
  (empty)        -> The duration text is empty or contains only white space.
  P              -> An iCalendar duration must contain at least one component.
  PT             -> An iCalendar duration must contain at least one component.
  P1DT           -> An iCalendar duration must contain at least one component.
  1H             -> An iCalendar duration must begin with the 'P' designator.
  P1H            -> The time units 'H', 'M', and 'S' must be preceded by the 'T' designator.
  PT1D           -> The duration unit 'D' is repeated or out of order; components must appear as weeks, or days then 'T' then hours, minutes, seconds, each at most once.
  P1W2D          -> The weeks unit 'W' cannot be combined with any other duration component.
  PT0.5H         -> The duration component '0.' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S.
  P1X            -> The duration component '1X' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S.
  PT4X           -> The duration component '4X' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S.
  P0D            -> The duration must be greater than zero; signed and zero durations are not valid intervals.
  -PT1H          -> The duration must be greater than zero; signed and zero durations are not valid intervals.
  PT1H30         -> The duration component '30' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S.

--- Parse throws where TryParse reports ---
  Parse("P1DT") threw FormatException
    An iCalendar duration must contain at least one component.

--- Validating a configuration block ---
  [ ok ] health-check    'PT30S' -> 00:00:30
  [ ok ] metrics-flush   'PT5M' -> 00:05:00
  [ ok ] cache-sweep     'PT6H' -> 06:00:00
  [ ok ] retention-scan  'P1W' -> 7.00:00:00
  [FAIL] typo            'PT5' -> The duration component '5' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S.
  [FAIL] negative        '-P1D' -> The duration must be greater than zero; signed and zero durations are not valid intervals.
```

**APIs demonstrated.** `AnchoredInterval.TryParse(string, out AnchoredInterval, out string)`,
`.Parse(string)` and the `FormatException` it throws.

## Layout

```text
Bodu.Globalization.Recurrence.Samples.AnchoredIntervals/
  Program.cs                          # runs the scenarios in order
  Scenarios/IntervalBasics.cs
  Scenarios/AnchoredQueries.cs
  Scenarios/DurationGrammar.cs
```

## Related

- `Bodu.Globalization.Recurrence.Samples.SchedulingHost` — why an interval is immune to the
  daylight-saving question a cron expression raises.
- `Bodu.Globalization.Recurrence.Samples.CronExpressions` — the wall-clock form of the same surface.
