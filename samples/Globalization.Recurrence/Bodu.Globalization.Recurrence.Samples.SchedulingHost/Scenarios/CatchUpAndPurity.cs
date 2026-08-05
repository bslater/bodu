// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CatchUpAndPurity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.SchedulingHost.Scenarios;

/// <summary>
/// Demonstrates the consequence of the package's purity contract: because nothing here reads a wall
/// clock or resolves a time zone, the host owns time — which is what makes a missed-run catch-up
/// loop testable, and what fixes the meaning of an occurrence across a daylight-saving transition.
/// </summary>
public static class CatchUpAndPurity
{
    /// <summary>
    /// Replays a downtime window, then shows the offset and DST posture.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Catching up after downtime ---");

        // The host supplies both ends of the window. Nothing in the library asks what time it is,
        // so this replay is exactly reproducible -- the same reason the package ships no timer,
        // no poller, and no due-state tracking.
        var origin = new DateTime(2026, 1, 1, 0, 0, 0);
        var lastRun = new DateTime(2026, 3, 9, 18, 0, 0);    // recorded before the host stopped
        var resumedAt = new DateTime(2026, 3, 10, 14, 32, 0);  // supplied by the host on restart

        Console.WriteLine($"last recorded run : {Format(lastRun)}");
        Console.WriteLine($"resumed at        : {Format(resumedAt)}");
        Console.WriteLine();

        (string Job, string Kind, string Text)[] jobs =
        [
            ("backup.nightly", "cron", "0 2 * * *"),
            ("probe.health", "interval", "PT6H"),
            ("report.weekly", "rrule", "FREQ=WEEKLY;BYDAY=MO"),
        ];

        foreach ((string job, string kind, string text) in jobs)
        {
            Schedule.TryCreate(kind, text, origin, out Schedule? schedule, out _);

            // Walking forward from the last run to the resume instant enumerates exactly the runs
            // that were missed -- the catch-up list, with no guessing.
            var missed = new List<DateTime>();
            var cursor = lastRun;

            while (schedule!.Next(cursor) is DateTime next && next <= resumedAt)
            {
                missed.Add(next);
                cursor = next;
            }

            Console.WriteLine($"{job,-16} ({kind,-8} '{text}')");
            Console.WriteLine($"  missed {missed.Count} run(s): {(missed.Count == 0 ? "(none)" : Join(missed))}");
            Console.WriteLine($"  next after resume : {Format(schedule.Next(resumedAt))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Was a run due when we stopped? ---");

        // The backward query answers "what should have run most recently" without walking the
        // series, which is how a host decides whether to fire immediately on startup.
        Schedule.TryCreate("cron", "0 2 * * *", origin, out Schedule? backup, out _);
        DateTime? shouldHaveRun = backup!.Previous(resumedAt);
        Console.WriteLine($"most recent scheduled run : {Format(shouldHaveRun)}");
        Console.WriteLine($"already ran?              : {shouldHaveRun <= lastRun}");
        Console.WriteLine($"=> fire immediately       : {shouldHaveRun > lastRun}");

        Console.WriteLine();
        Console.WriteLine("--- The purity contract ---");
        Console.WriteLine("  No type in this package calls DateTime.Now / UtcNow / Today, resolves a");
        Console.WriteLine("  TimeZoneInfo, or reads Environment.TickCount. A metadata scan over the compiled");
        Console.WriteLine("  assembly enforces that (PurityTests), so it cannot regress silently.");
        Console.WriteLine();
        Console.WriteLine("  Consequences a host can rely on:");
        Console.WriteLine("    - every query is a pure function of its arguments, so schedules are testable");
        Console.WriteLine("      without a clock abstraction;");
        Console.WriteLine("    - the same inputs give the same answers on any machine, in any locale;");
        Console.WriteLine("    - and time zone policy stays where the host can see it.");

        Console.WriteLine();
        Console.WriteLine("--- Offsets ride along; the wall clock is what recurs ---");

        // The DateTimeOffset overloads carry the query instant's offset onto the answer. The
        // library does not shift it, because it has no zone database to shift it with.
        CronExpression daily = CronExpression.Parse("0 9 * * *");
        var atUtc = new DateTimeOffset(2026, 3, 10, 14, 32, 0, TimeSpan.Zero);
        var atPlusTen = new DateTimeOffset(2026, 3, 10, 14, 32, 0, TimeSpan.FromHours(10));

        Console.WriteLine($"  query {atUtc:yyyy-MM-dd HH:mm zzz} -> next {daily.GetNextOccurrence(atUtc):yyyy-MM-dd HH:mm zzz}");
        Console.WriteLine($"  query {atPlusTen:yyyy-MM-dd HH:mm zzz} -> next {daily.GetNextOccurrence(atPlusTen):yyyy-MM-dd HH:mm zzz}");
        Console.WriteLine("  (09:00 local in each case -- the offset is preserved, never converted)");

        Console.WriteLine();
        Console.WriteLine("--- Daylight saving is a hosting decision ---");
        Console.WriteLine("  A cron expression names a wall-clock time, so '0 2 * * *' means 02:00 local on");
        Console.WriteLine("  every calendar day -- including the day 02:00 does not exist, and the day it");
        Console.WriteLine("  happens twice. This library has no zone database and so takes no position on");
        Console.WriteLine("  either case.");
        Console.WriteLine();
        Console.WriteLine("  A host that needs zone-correct firing converts at the boundary:");
        Console.WriteLine("    1. ask for the next wall-clock occurrence here;");
        Console.WriteLine("    2. resolve it against its zone with TimeZoneInfo (deciding skipped and");
        Console.WriteLine("       ambiguous times to its own policy);");
        Console.WriteLine("    3. wait on the resulting instant.");
        Console.WriteLine();
        Console.WriteLine("  An anchored interval has no such problem: it is exact elapsed time, so 'PT6H'");
        Console.WriteLine("  is six hours regardless of what the local calendar does.");

        // The contrast, made concrete: across the 2026 US spring-forward date, a 6-hour interval
        // advances by exactly 6 hours of elapsed time on every step.
        AnchoredInterval sixHourly = AnchoredInterval.Parse("PT6H");
        var beforeTransition = new DateTime(2026, 3, 8, 0, 0, 0);
        Console.WriteLine();
        Console.WriteLine($"  PT6H anchored {Format(beforeTransition)} (US spring-forward day):");
        Console.WriteLine($"    {Join(sixHourly.GetOccurrences(beforeTransition).Take(5))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an optional instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "(none)";

    /// <summary>
    /// Formats a sequence of occurrences as a comma-separated invariant-culture list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTime> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture)));
}
