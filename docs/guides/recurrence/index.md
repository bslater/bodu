# Recurrence and scheduling

**Bodu.Globalization.Recurrence** answers two questions for a recurring schedule — "when is the next occurrence?" and "when was the previous one?" — deterministically, testably, and without ever touching the wall clock. It models four schedule forms behind one uniform query surface:

| Form | Type | Shape |
|---|---|---|
| RFC 5545 recurrence rule | <xref:Bodu.Globalization.Recurrence.RecurrenceRule> | Calendar-aligned: `FREQ=WEEKLY;BYDAY=MO,FR`, anchored to a caller-supplied series start |
| Composed rule set | <xref:Bodu.Globalization.Recurrence.RecurrenceSet> | One or more rules plus explicit `RDATE` additions and `EXDATE` exclusions |
| Cron expression | <xref:Bodu.Globalization.Recurrence.CronExpression> | Calendar-aligned: Vixie five-field, optional-seconds six-field, and the `@` macros |
| Anchored interval | <xref:Bodu.Globalization.Recurrence.AnchoredInterval> | Instant-aligned: occurrences at `anchor + k·interval` for `k ≥ 1`, e.g. "every 4 hours after the last completed run" |

Every form parses from a canonical text (`TryParse`, including an overload that reports the specific parse defect as a message), renders back to text that re-parses to an equal value, and compares by value — so a host can persist schedules as text and detect configuration changes by comparison.

## Choosing a form

- **"Daily at 02:00", "weekdays at 9", "last Friday of the month"** — calendar-aligned schedules. Use a cron expression for operational shapes, or an `RRULE` when you need the richer RFC 5545 grammar (`BYSETPOS`, `BYWEEKNO`, count/until bounds) or interoperability with iCalendar data.
- **"Every 4 hours after the previous run completed"** — an anchored interval. The anchor is passed to every query, never stored; its meaning ("last completed run", "enrolment", "contract start") is entirely yours.
- **"The daily rule, plus these extra dates, minus these excluded dates"** — a recurrence set.

## The due-ness recipe

The library never stores a last-run, marks an occurrence consumed, or owns a timer. Due-ness is a one-line comparison over library answers:

```csharp
bool IsDue(DateTimeOffset lastCompleted, DateTimeOffset now) =>
    lastCompleted < schedule.GetPreviousOccurrence(now, inclusive: true);

DateTimeOffset? NextRun(DateTimeOffset now) =>
    schedule.GetNextOccurrence(now);
```

Because the answer is an instant rather than a backlog, missed occurrences coalesce structurally: a host asleep through five occurrences owes exactly one catch-up run, and an evaluation five days late answers the same boolean as one evaluated a minute late. Every form answers both `GetNextOccurrence(after, inclusive)` and `GetPreviousOccurrence(before, inclusive)` over both `DateTime` and `DateTimeOffset`.

For an anchored interval the anchor itself is *not* an occurrence — a run completed at `now` is not immediately due again:

```csharp
var interval = AnchoredInterval.Parse("PT4H");

// Occurrences: anchor + 4h, anchor + 8h, ...
bool isDue = lastCompleted < interval.GetPreviousOccurrence(anchor: lastCompleted, before: now, inclusive: true);
```

## Offsets, purity, and daylight saving

Every answer is a pure function of the arguments. No API in the package reads the wall clock (`DateTime.Now`, `Stopwatch`, `Environment.TickCount`) or the machine time zone (`TimeZoneInfo`, `ToLocalTime`), and a metadata-level test in the repository fails the build if such a reference is ever introduced. The caller owns the clock: pass `DateTimeOffset.Now` (or a simulated or test-fixed instant) in.

The `DateTimeOffset` overloads interpret the wall-clock time in the argument's own offset and return occurrences carrying that offset. The library performs no offset conversion beyond normalising *between the arguments it was given* — an `AnchoredInterval` anchor supplied in UTC composes correctly with a `now` in +10:00, because those two are compared as absolute instants.

The library operates on offsets, not time zones, so daylight-saving transitions are the caller's concern: a host that wants "02:30 local" across a DST change re-derives the offset on each evaluation (for example by passing `DateTimeOffset.Now` each time). On a transition day two wall-clock cases arise, and the occurrence math is deliberately naive about both:

- **A local time that occurs twice** (clocks fall back): the schedule's wall-clock answer, interpreted in whichever offset the caller supplied, names one instant; the library does not know the time occurred twice.
- **A local time that never occurs** (clocks spring forward): the library still answers the wall-clock instant; it is the caller's decision whether to run at the shifted equivalent.

## Calendar-aware filtering is composition, not a feature

"Daily at 02:00, but not on public holidays" is a filter over an occurrence stream, applied from outside — the recurrence package carries no holiday, locale, or business-day data and takes no dependency on `Bodu.Globalization.Calendar`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Recurrence;

INotableDateService holidays = AmericasCalendarData.CreateService("US");
RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");

DateTime? nextWorkingRun = rule
    .GetOccurrences(start, from: now, to: now.AddDays(30))
    .FirstOrDefault(occurrence => !holidays.IsHoliday(occurrence.Date));
```

Any predicate works; the calendar package is just the in-repo source of holiday truth.

## Bounded searches

Occurrence enumeration over a window is lazy and terminates for every input, including rules that can never match:

- A `RecurrenceRule` such as `FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30` (30 February) enumerates empty and answers `null` from both point queries; the search bound is the end of the representable calendar (year 9999).
- A `CronExpression` search scans a twelve-year horizon in each direction — enough to cover the largest gap of any satisfiable expression (a 29 February schedule crossing a non-leap century year) — and answers `null` past it.
- An `AnchoredInterval` needs no scanning at all: its queries are O(1) arithmetic, and the sequence ends at the last representable occurrence.

## Parse defects are named

Hosts surface configuration errors verbatim, without exception-driven control flow:

```csharp
if (!AnchoredInterval.TryParse(text, out AnchoredInterval? interval, out string? defect))
{
    logger.LogError("Invalid schedule '{Text}': {Defect}", text, defect);
}
```

The message names the offending token — "The duration component '4X' is not valid; each component is an unsigned integer followed by a unit, and the unit must be W, D, H, M, or S." beats "invalid format" — and the same overload shape exists on all four forms.
