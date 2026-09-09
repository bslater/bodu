---
title: Hosting schedules
---

# Hosting schedules

`Bodu.Globalization.Recurrence` deliberately ships no timer, no poller, no last-run store, and no clock: every query is a pure function of the instants you pass in. That leaves a host with four small jobs — normalise the four schedule forms behind one query surface, decide *when* to ask, persist what has already run, and convert between wall-clock and zone at its own boundary. This guide shows each one in a shape that stays reproducible in a test, mirroring the repository's `Bodu.Globalization.Recurrence.Samples.SchedulingHost` sample.

For the library's own statement of the contract — purity, offsets, and the due-ness recipe — see the [recurrence overview](index.md).

## Pattern 1 — one adapter over the four forms

The forms differ in what they need to answer a query: a rule and an interval need a series origin, a cron expression and a set do not. Capturing the origin at configuration time reduces every form to `Next(after, inclusive)` / `Previous(before, inclusive)`, which is exactly the pair every form guarantees with identical flag semantics. The adapter is a hosting decision, so the package does not ship one:

```csharp
using Bodu.Globalization.Recurrence;

/// <summary>A host-side view of any schedule form as two point queries.</summary>
public interface ISchedule
{
    string Kind { get; }
    string Text { get; }
    DateTime? Next(DateTime after, bool inclusive = false);
    DateTime? Previous(DateTime before, bool inclusive = false);
}

public sealed class Schedule : ISchedule
{
    private readonly Func<DateTime, bool, DateTime?> _next;
    private readonly Func<DateTime, bool, DateTime?> _previous;

    private Schedule(string kind, string text, Func<DateTime, bool, DateTime?> next, Func<DateTime, bool, DateTime?> previous)
    {
        Kind = kind;
        Text = text;
        _next = next;
        _previous = previous;
    }

    public string Kind { get; }
    public string Text { get; }

    public DateTime? Next(DateTime after, bool inclusive = false) => _next(after, inclusive);
    public DateTime? Previous(DateTime before, bool inclusive = false) => _previous(before, inclusive);

    /// <summary>Parses <paramref name="text" /> as the named form, binding the origin where the form needs one.</summary>
    public static bool TryCreate(string kind, string text, DateTime origin, out Schedule? schedule, out string? defect)
    {
        schedule = null;

        switch (kind)
        {
            case "rrule":
                if (!RecurrenceRule.TryParse(text, out RecurrenceRule? rule, out defect)) return false;
                schedule = new Schedule(kind, text,
                    (from, inclusive) => rule.GetNextOccurrence(origin, from, inclusive),
                    (from, inclusive) => rule.GetPreviousOccurrence(origin, from, inclusive));
                return true;

            case "cron":
                if (!CronExpression.TryParse(text, out CronExpression? cron, out defect)) return false;
                schedule = new Schedule(kind, text,
                    (from, inclusive) => cron.GetNextOccurrence(from, inclusive),
                    (from, inclusive) => cron.GetPreviousOccurrence(from, inclusive));
                return true;

            case "interval":
                if (!AnchoredInterval.TryParse(text, out AnchoredInterval? interval, out defect)) return false;
                schedule = new Schedule(kind, text,
                    (from, inclusive) => interval.GetNextOccurrence(origin, from, inclusive),
                    (from, inclusive) => interval.GetPreviousOccurrence(origin, from, inclusive));
                return true;

            case "set":
                if (!RecurrenceSet.TryParse(text, out RecurrenceSet? set, out defect)) return false;
                schedule = new Schedule(kind, text,
                    (from, inclusive) => set.GetNextOccurrence(from, inclusive),
                    (from, inclusive) => set.GetPreviousOccurrence(from, inclusive));
                return true;

            default:
                defect = $"Unknown schedule kind '{kind}'.";
                return false;
        }
    }
}
```

Every `TryParse` above is the defect-naming overload, so a bad configuration line surfaces its reason verbatim ("The cron field '60' is not valid.") without exception-driven control flow. Validate all entries and report every defect at start-up rather than failing on the first.

## Pattern 2 — a reproducible catch-up loop over `TimeProvider`

Because nothing in the library asks what time it is, the host owns the clock. Take a <xref:System.TimeProvider> (or simply the `now` instant) as a parameter and the whole loop becomes a pure function you can drive from a test with `FakeTimeProvider` — no sleeping, no flakiness. Walking `Next` forward from the last recorded run to `now` enumerates exactly the missed occurrences; the backward query answers "should something have run?" without walking at all:

```csharp
using Bodu.Globalization.Recurrence;

public sealed record CatchUpDecision(IReadOnlyList<DateTime> Missed, DateTime? NextDue);

public static class CatchUp
{
    /// <summary>Lists the occurrences between the last completed run and now, and the next one after now.</summary>
    public static CatchUpDecision Evaluate(ISchedule schedule, DateTime lastCompleted, DateTime now)
    {
        var missed = new List<DateTime>();
        DateTime cursor = lastCompleted;

        while (schedule.Next(cursor) is DateTime next && next <= now)
        {
            missed.Add(next);
            cursor = next;
        }

        return new CatchUpDecision(missed, schedule.Next(now));
    }

    /// <summary>The coalesced form: one boolean, regardless of how many occurrences were missed.</summary>
    public static bool IsDue(ISchedule schedule, DateTime lastCompleted, DateTime now) =>
        lastCompleted < schedule.Previous(now, inclusive: true);
}
```

```csharp
using Bodu.Globalization.Recurrence;

var origin    = new DateTime(2026, 1, 1, 0, 0, 0);
var lastRun   = new DateTime(2026, 3, 9, 18, 0, 0);     // recorded before the host stopped
var resumedAt = new DateTime(2026, 3, 10, 14, 32, 0);   // TimeProvider.GetUtcNow() on restart, as a DateTime

Schedule.TryCreate("cron", "0 2 * * *", origin, out Schedule? nightly, out _);
Schedule.TryCreate("interval", "PT6H", origin, out Schedule? probe, out _);
Schedule.TryCreate("rrule", "FREQ=WEEKLY;BYDAY=MO", origin, out Schedule? weekly, out _);

CatchUpDecision backup = CatchUp.Evaluate(nightly!, lastRun, resumedAt);
// Missed: [2026-03-10 02:00]                 NextDue: 2026-03-11 02:00
CatchUpDecision health = CatchUp.Evaluate(probe!, lastRun, resumedAt);
// Missed: [03-10 00:00, 03-10 06:00, 03-10 12:00]   NextDue: 2026-03-10 18:00
CatchUpDecision report = CatchUp.Evaluate(weekly!, lastRun, resumedAt);
// Missed: []                                 NextDue: 2026-03-16 00:00

bool fireNow = CatchUp.IsDue(nightly!, lastRun, resumedAt);   // true — one catch-up run, not a backlog
```

Whether to replay every missed occurrence (the `Missed` list) or run once (the due-ness comparison) is a per-job policy: a report that must exist for every period replays; a cache refresh runs once. The library's answer is an instant in both cases, so the choice is yours and is made in code you can test.

Wire the evaluation into a hosted service by injecting the `TimeProvider` and polling at a coarse interval. The poll cadence only bounds latency; it never changes *which* occurrences are due:

```csharp
using Microsoft.Extensions.Hosting;

public sealed class ScheduleRunner : BackgroundService
{
    private readonly IReadOnlyList<(ISchedule Schedule, Func<CancellationToken, Task> Job)> _jobs;
    private readonly ILastRunStore _lastRuns;
    private readonly TimeProvider _time;

    public ScheduleRunner(
        IReadOnlyList<(ISchedule Schedule, Func<CancellationToken, Task> Job)> jobs,
        ILastRunStore lastRuns,
        TimeProvider time)
    {
        _jobs = jobs;
        _lastRuns = lastRuns;
        _time = time;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var poll = new PeriodicTimer(TimeSpan.FromSeconds(30), _time);

        do
        {
            DateTime now = _time.GetUtcNow().UtcDateTime;

            foreach ((ISchedule schedule, Func<CancellationToken, Task> job) in _jobs)
            {
                DateTime lastCompleted = _lastRuns.Get(schedule.Text) ?? DateTime.MinValue;
                if (!CatchUp.IsDue(schedule, lastCompleted, now))
                    continue;

                await job(stoppingToken);
                _lastRuns.Set(schedule.Text, now);
            }
        }
        while (await poll.WaitForNextTickAsync(stoppingToken));
    }
}
```

The periodic timer takes the same `TimeProvider`, so a test can advance a `FakeTimeProvider` past the tick and the run instant together and assert exactly which jobs fired.

## Pattern 3 — persisting the last-run instant

The only state a host needs per schedule is the instant its job last **completed** — the `lastCompleted` in the due-ness comparison and, for an anchored interval, potentially the anchor itself. Store it as UTC and treat its absence as "never ran":

```csharp
/// <summary>The one piece of scheduling state the host owns: when each job last completed.</summary>
public interface ILastRunStore
{
    DateTime? Get(string scheduleKey);
    void Set(string scheduleKey, DateTime completedAtUtc);
}

public sealed class InMemoryLastRunStore : ILastRunStore
{
    private readonly Dictionary<string, DateTime> _completed = new(StringComparer.Ordinal);

    public DateTime? Get(string scheduleKey) =>
        _completed.TryGetValue(scheduleKey, out DateTime value) ? value : null;

    public void Set(string scheduleKey, DateTime completedAtUtc) =>
        _completed[scheduleKey] = DateTime.SpecifyKind(completedAtUtc, DateTimeKind.Utc);
}
```

Two rules keep this robust:

- **Record completion, not start.** A job that crashes mid-run leaves the previous instant in place, so the next evaluation sees it as still due.
- **Record `now`, not the occurrence.** Setting the last-run instant to the evaluation time (as the runner above does) collapses any missed backlog into the single run just performed; setting it to the *occurrence* instant would replay the backlog one poll at a time. Pick deliberately per job.

Because every form round-trips through canonical text and compares by value, the schedule text is a good store key: a changed configuration line is a new key and starts fresh, which is usually what you want.

## Pattern 4 — daylight saving and time zones at the boundary

Rule, cron, and set schedules name a **wall-clock** time. The library evaluates them in whatever offset you pass and never consults `TimeZoneInfo`, so "02:00 every day in Sydney" needs the host to (1) ask for the next wall-clock occurrence, (2) resolve it against the zone, deciding skipped and ambiguous times to its own policy, and (3) wait on the resulting instant. Doing the conversion per evaluation is what keeps the schedule correct across a transition:

```csharp
using Bodu.Globalization.Recurrence;

public static class ZonedSchedule
{
    /// <summary>Next occurrence of a wall-clock schedule in <paramref name="zone" />, as an absolute instant.</summary>
    public static DateTimeOffset? NextInZone(ISchedule schedule, TimeZoneInfo zone, DateTimeOffset nowUtc)
    {
        // 1. Ask in the zone's *current* wall-clock time.
        DateTime localNow = TimeZoneInfo.ConvertTime(nowUtc, zone).DateTime;
        DateTime? localNext = schedule.Next(localNow);
        if (localNext is null)
            return null;

        DateTime wall = DateTime.SpecifyKind(localNext.Value, DateTimeKind.Unspecified);

        // 2. Decide the transition cases explicitly. Policy here: a skipped time runs at the shifted
        //    equivalent; an ambiguous time takes the first (standard-time) instant.
        if (zone.IsInvalidTime(wall))
            wall = wall.Add(zone.GetAdjustmentRules().Length > 0 ? TimeSpan.FromHours(1) : TimeSpan.Zero);

        TimeSpan offset = zone.IsAmbiguousTime(wall)
            ? zone.GetAmbiguousTimeOffsets(wall).Max()
            : zone.GetUtcOffset(wall);

        // 3. The absolute instant to wait on.
        return new DateTimeOffset(wall, offset);
    }
}
```

An anchored interval has no such problem: it is exact elapsed time, so `PT6H` is six hours on the spring-forward day too — anchor it in UTC and pass `nowUtc` as-is. Keep the zone policy in one place like this so it is visible and testable; the library will never silently pick one for you.

## Pattern 5 — skip non-working days with the calendar package

"Every night at 02:00, except on public holidays" is a filter over the occurrence stream, applied from outside: the recurrence package carries no holiday data and takes no dependency on the calendar package. `IsNonWorkingDay` — the `DateTime` / `DateTimeOffset` / `DateOnly` extension in the `Bodu.Extensions` namespace of `Bodu.Globalization.Calendar` — answers `true` for weekends and for the territory's resolved non-working notable dates, so it is the one predicate to apply. Walk `Next` until it passes:

```csharp
using Bodu.Extensions;                    // IsNonWorkingDay
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Recurrence;

INotableDateService holidays = AmericasCalendarData.CreateService("US");
CronExpression nightly = CronExpression.Parse("0 2 * * *");

DateTime? NextWorkingRun(DateTime after)
{
    DateTime cursor = after;
    while (nightly.GetNextOccurrence(cursor) is DateTime next)
    {
        if (!next.IsNonWorkingDay(holidays, "US"))
            return next;
        cursor = next;
    }

    return null;
}

DateTime? first = NextWorkingRun(new DateTime(2026, 7, 1, 12, 0, 0));   // 2026-07-02 Thu 02:00
DateTime? second = NextWorkingRun(first!.Value);                        // 2026-07-06 Mon 02:00
// 3 July 2026 is the observed Independence Day (4 July falls on a Saturday) and is skipped
// along with the weekend; the observed date comes from the data pack's adjustment rules.
```

The same predicate works over a windowed `GetOccurrences` with LINQ's `Where`, as the [overview](index.md#calendar-aware-filtering-is-composition-not-a-feature) shows. Pass a `WeekPattern` as the optional trailing argument to change the working week, or use `IsNotableDate` to test the notable dates alone — see [Working-day arithmetic](../calendar/working-days.md).

## Where to go next

- **[RFC 5545 recurrence rules](rrule.md)**, **[Cron expressions](cron.md)**, **[Anchored intervals](anchored-intervals.md)**, **[Recurrence sets](recurrence-sets.md)** — the per-form references.
- **[Runnable samples](../../samples/recurrence.md)** — `Bodu.Globalization.Recurrence.Samples.SchedulingHost` runs the adapter, configuration validation, offset-aware queries, and the catch-up loop offline.
- **[Working-day arithmetic](../calendar/working-days.md)** — everything `IsNonWorkingDay` and its siblings can do.
- **[Bodu.Globalization.Recurrence API reference](xref:Bodu.Globalization.Recurrence)**
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
