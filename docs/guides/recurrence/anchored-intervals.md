---
title: Anchored intervals
---

# Anchored intervals

<xref:Bodu.Globalization.Recurrence.AnchoredInterval> models a schedule that repeats a fixed elapsed time after a caller-supplied instant: the occurrences are `anchor + k·interval` for `k ≥ 1`. It is the instant-aligned form — "every four hours after the last completed run", "ninety minutes after enrolment" — as opposed to the calendar-aligned rule and cron forms. The interval is the whole value; the anchor is passed to every query and never stored.

For the "which form" decision and the shared due-ness recipe, start at the [recurrence overview](index.md).

## Pattern 1 — construct from a `TimeSpan` or parse duration text

The constructor takes any positive <xref:System.TimeSpan> that is a whole number of seconds. The textual form is the RFC 5545 §3.3.6 duration grammar:

```csharp
using Bodu.Globalization.Recurrence;

var fourHourly = new AnchoredInterval(TimeSpan.FromHours(4));      // renders "PT4H"
var ninetyMin  = new AnchoredInterval(TimeSpan.FromMinutes(90));   // renders "PT1H30M"
var weekly     = new AnchoredInterval(TimeSpan.FromDays(7));       // renders "P1W"

AnchoredInterval parsed = AnchoredInterval.Parse("P1DT2H30M");
TimeSpan span = parsed.Interval;                                    // 1.02:30:00

bool same = AnchoredInterval.Parse("PT90M").Equals(ninetyMin);      // true — value equality on the TimeSpan
bool alsoSame = AnchoredInterval.Parse("P1W").Equals(AnchoredInterval.Parse("P7D"));   // true
```

A zero or negative `TimeSpan` throws <xref:System.ArgumentOutOfRangeException>; a value with sub-second ticks throws <xref:System.ArgumentException> ("The interval must be a whole number of seconds; iCalendar durations carry no sub-second precision."), because the duration text could not round-trip it.

## The duration grammar

The parser accepts exactly the positive subset of RFC 5545 `dur-value`:

| Form | Example | Meaning |
|---|---|---|
| `P<n>W` | `P2W` | weeks — must appear **alone** |
| `P<n>D` | `P14D` | days |
| `PT<n>H[<n>M][<n>S]` | `PT4H`, `PT1H30M`, `PT90M`, `PT45S` | time components, after the `T` designator |
| `P<n>DT…` | `P1DT2H30M` | days then time |
| `+P…` | `+PT1H` | an explicit plus sign is accepted |

Rules, each with the message `TryParse` reports:

- The text must start with `P` ("An iCalendar duration must begin with the 'P' designator."), is case-insensitive (`pt4h` parses), and must contain at least one component (`P`, `PT`, and `P1DT` are rejected).
- Time units must follow `T`: `P1M` is rejected with "The time units 'H', 'M', and 'S' must be preceded by the 'T' designator." — there is no months or years unit, and `P1Y` fails as an unknown unit ("…the unit must be W, D, H, M, or S.").
- Components appear at most once, in order — weeks, or days then `T` then hours, minutes, seconds — so `PT4H1D` is rejected.
- Weeks cannot be combined: `P1W1D` fails with "The weeks unit 'W' cannot be combined with any other duration component."
- Values are unsigned integers (`PT1.5H` is rejected) and the total must be positive: `-PT1H`, `PT0S`, and `P0D` fail with "The duration must be greater than zero; signed and zero durations are not valid intervals."

```csharp
using Bodu.Globalization.Recurrence;

if (!AnchoredInterval.TryParse("P1W1D", out AnchoredInterval? interval, out string? failureMessage))
{
    Console.WriteLine(failureMessage);
    // "The weeks unit 'W' cannot be combined with any other duration component."
}

// Boolean-only overloads, string and span:
bool ok1 = AnchoredInterval.TryParse("PT4H", out AnchoredInterval? a);
bool ok2 = AnchoredInterval.TryParse("PT4H".AsSpan(), provider: null, out AnchoredInterval? b);
```

## Canonical `ToString` form

`ToString()` normalises rather than echoing the input: an interval that is an exact multiple of seven days renders in the weeks form, otherwise the days and time components render with zero components omitted. So `P14D` → `P2W`, `PT90M` → `PT1H30M`, `PT36H` → `P1DT12H`, and eight days → `P8D`. The canonical text re-parses to an equal value. Only the `"G"` / `null` specifier is defined on the <xref:System.IFormattable> overloads.

## Pattern 2 — occurrences and the anchor boundary

The anchor itself is **never** an occurrence. `GetNextOccurrence(anchor, after)` answers the first `anchor + k·interval` strictly after `after` (or equal, with `inclusive: true`); when `after` precedes the whole series, the first occurrence is returned. `GetPreviousOccurrence` answers `null` until the first occurrence has passed:

```csharp
using Bodu.Globalization.Recurrence;

AnchoredInterval sixHourly = AnchoredInterval.Parse("PT6H");
var anchor = new DateTime(2026, 3, 8, 0, 0, 0);

DateTime? next     = sixHourly.GetNextOccurrence(anchor, anchor);                       // 06:00 — anchor excluded
DateTime? nextIncl = sixHourly.GetNextOccurrence(anchor, anchor, inclusive: true);      // 06:00 — still not the anchor
DateTime? early    = sixHourly.GetNextOccurrence(anchor, anchor.AddDays(-3));           // 06:00 — the first occurrence

DateTime? prevAtAnchor = sixHourly.GetPreviousOccurrence(anchor, anchor, inclusive: true);          // null
DateTime? prevAtFirst  = sixHourly.GetPreviousOccurrence(anchor, anchor.AddHours(6));               // null (exclusive)
DateTime? prevAtFirstI = sixHourly.GetPreviousOccurrence(anchor, anchor.AddHours(6), inclusive: true); // 06:00

DateTime[] firstFour = sixHourly.GetOccurrences(anchor).Take(4).ToArray();
// 2026-03-08 06:00, 12:00, 18:00, 2026-03-09 00:00

DateTime[] window = sixHourly.GetOccurrences(anchor, from: anchor.AddHours(12), to: anchor.AddHours(24)).ToArray();
// 12:00, 18:00, 2026-03-09 00:00 — inclusive at both ends
```

Every query is O(1) arithmetic — no scanning — and the unbounded enumeration ends at the last representable occurrence; `GetNextOccurrence` answers `null` once `anchor + (k+1)·interval` would exceed `DateTime.MaxValue`.

## Why the anchor is passed per query

The interval is *configuration* ("every six hours"); the anchor is *state* ("the last completed run finished at 14:32"). Keeping them apart means the same parsed interval serves every job, and the host — which already persists the last-run instant — is the only party that knows the anchor. It also fixes the due-ness rule: because the anchor is not an occurrence, a run that completed at `now` is not immediately due again, and "is a run due?" stays the one-line comparison every form shares:

```csharp
using Bodu.Globalization.Recurrence;

AnchoredInterval sixHourly = AnchoredInterval.Parse("PT6H");
var lastCompleted = new DateTime(2026, 3, 8, 0, 0, 0);
var now = new DateTime(2026, 3, 8, 5, 59, 0);

bool due = lastCompleted < sixHourly.GetPreviousOccurrence(anchor: lastCompleted, before: now, inclusive: true);   // false
bool dueLater = lastCompleted < sixHourly.GetPreviousOccurrence(lastCompleted, now.AddMinutes(1), inclusive: true); // true
```

Re-anchoring on each completion turns the interval into a *gap* schedule (six hours after the previous run finished); keeping a fixed anchor turns it into a *cadence* (every six hours from the original start, regardless of how long runs take). Both are host decisions the library does not make for you.

## Pattern 3 — heartbeat schedules

A heartbeat anchored on the moment a service came up, evaluated with the host's clock:

```csharp
using Bodu.Globalization.Recurrence;

AnchoredInterval heartbeat = AnchoredInterval.Parse("PT30S");
DateTimeOffset startedAt = new(2026, 3, 10, 14, 32, 0, TimeSpan.Zero);

DateTimeOffset? NextBeat(DateTimeOffset now) =>
    heartbeat.GetNextOccurrence(startedAt, now);

DateTimeOffset? first = NextBeat(startedAt);                    // 14:32:30 +00:00
DateTimeOffset? later = NextBeat(startedAt.AddMinutes(10));     // 14:42:30 +00:00 — no drift, still on the :00/:30 grid
```

Because the answer is computed from the anchor, not from the previous beat, the grid never drifts even when the host wakes late.

## Pattern 4 — retry back-off windows

Anchor on the failure instant. A fixed interval gives evenly spaced retries; for exponential back-off, parse one interval per attempt and anchor each on the previous attempt:

```csharp
using Bodu.Globalization.Recurrence;

var failedAt = new DateTime(2026, 3, 10, 14, 32, 0);

// Fixed: three retries two minutes apart.
DateTime[] fixedRetries = AnchoredInterval.Parse("PT2M").GetOccurrences(failedAt).Take(3).ToArray();
// 14:34, 14:36, 14:38

// Exponential: 1, 2, 4, 8 minutes — each step anchored on the previous attempt.
string[] ladder = ["PT1M", "PT2M", "PT4M", "PT8M"];
var attempts = new List<DateTime>();
DateTime anchor = failedAt;

foreach (string step in ladder)
{
    DateTime attempt = AnchoredInterval.Parse(step).GetNextOccurrence(anchor, anchor)!.Value;
    attempts.Add(attempt);
    anchor = attempt;
}
// 14:33, 14:35, 14:39, 14:47
```

## Pattern 5 — aligning to an anchor across daylight saving

An anchored interval is exact elapsed time, so it is immune to daylight-saving changes: `PT6H` is six hours whatever the local calendar does. The `DateTimeOffset` overloads compare the anchor and the query instant as absolute instants, so an anchor recorded in UTC composes correctly with a `now` in any offset, and each answer carries the offset of the instant you asked with:

```csharp
using Bodu.Globalization.Recurrence;

AnchoredInterval sixHourly = AnchoredInterval.Parse("PT6H");

var anchorUtc = new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero);                 // US spring-forward day
var nowSydney = new DateTimeOffset(2026, 3, 8, 14, 0, 0, TimeSpan.FromHours(10));       // = 04:00 UTC

DateTimeOffset? next = sixHourly.GetNextOccurrence(anchorUtc, nowSydney);
// 2026-03-08 16:00 +10:00  — that is 06:00 UTC, the first occurrence after 04:00 UTC

DateTimeOffset firstLocal = sixHourly.GetOccurrences(new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.FromHours(-5))).First();
// 2026-03-08 06:00 -05:00 — enumeration keeps the anchor's offset
```

Contrast a cron or rule schedule, which names a *wall-clock* time and therefore needs the host to decide what "02:00" means on a transition day — see [Hosting schedules](scheduling-host.md#pattern-4--daylight-saving-and-time-zones-at-the-boundary).

## API summary

| Member | Purpose |
|---|---|
| `AnchoredInterval(TimeSpan)` | Construct from a positive whole-second span. |
| `Interval` | The spacing between occurrences. |
| `Parse(string)` / `Parse(string, IFormatProvider?)` / `Parse(ReadOnlySpan<char>, IFormatProvider?)` | Parse RFC 5545 duration text; `FormatException` names the defect. |
| `TryParse(string?, out result)` / `TryParse(string?, IFormatProvider?, out result)` / `TryParse(ReadOnlySpan<char>, IFormatProvider?, out result)` | Boolean parse. |
| `TryParse(string?, out result, out failureMessage)` | Boolean parse that names the defect. |
| `GetNextOccurrence(anchor, after, inclusive = false)` | First `anchor + k·interval` after `after`, or `null`; `DateTime` and `DateTimeOffset`. |
| `GetPreviousOccurrence(anchor, before, inclusive = false)` | Last occurrence before `before`, or `null` (never the anchor). |
| `GetOccurrences(anchor)` / `GetOccurrences(anchor, from, to)` | Lazy ascending enumeration. |
| `ToString()` / `ToString(string?)` / `ToString(string?, IFormatProvider?)` | Canonical duration text (`P2W`, `PT1H30M`, …). |
| `Equals` / `GetHashCode` | Value equality on `Interval`. |

## Where to go next

- **[Cron expressions](cron.md)** and **[RFC 5545 recurrence rules](rrule.md)** — the calendar-aligned forms.
- **[Hosting schedules](scheduling-host.md)** — persisting the anchor as the last-run instant and driving all four forms from one loop.
- **[Runnable samples](../../samples/recurrence.md)** — `Bodu.Globalization.Recurrence.Samples.AnchoredIntervals`.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)** — `AnchoredInterval`.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
