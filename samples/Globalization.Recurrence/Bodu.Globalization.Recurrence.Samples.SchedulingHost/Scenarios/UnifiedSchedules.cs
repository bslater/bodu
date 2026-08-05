// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnifiedSchedules.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.SchedulingHost.Scenarios;

/// <summary>
/// Demonstrates the uniform point-query surface: all four schedule forms answer next and previous
/// with the same signature shape and the same inclusive-flag meaning, so a host can hold them
/// behind one adapter and never branch on form again after configuration.
/// </summary>
public static class UnifiedSchedules
{
    /// <summary>
    /// Builds one schedule of each form and queries them all identically.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- One adapter over four schedule forms ---");

        // A host reads schedules from configuration, where the form is a field. After parsing,
        // nothing downstream needs to know which form it got.
        var origin = new DateTime(2026, 1, 1, 0, 0, 0);
        var now = new DateTime(2026, 3, 10, 14, 32, 0);   // a Tuesday

        (string Kind, string Text, string Purpose)[] configured =
        [
            ("cron", "0 2 * * *", "nightly backup"),
            ("cron", "*/15 * * * *", "health probe"),
            ("rrule", "FREQ=WEEKLY;BYDAY=MO", "weekly report"),
            ("rrule", "FREQ=MONTHLY;BYDAY=-1FR", "month-end close"),
            ("interval", "PT6H", "cache sweep"),
            ("interval", "P1W", "retention scan"),
            ("set", "DTSTART:20260105T090000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO\r\nEXDATE:20260316T090000", "standup (one skipped)"),
        ];

        Console.WriteLine($"origin : {Format(origin)}   query instant : {Format(now)}");
        Console.WriteLine();
        Console.WriteLine($"{"kind",-9} {"purpose",-22} {"previous",-17} {"next",-17}");
        Console.WriteLine($"{new string('-', 9)} {new string('-', 22)} {new string('-', 17)} {new string('-', 17)}");

        var schedules = new List<(Schedule Schedule, string Purpose)>();

        foreach ((string kind, string text, string purpose) in configured)
        {
            if (!Schedule.TryCreate(kind, text, origin, out Schedule? schedule, out string? defect))
            {
                Console.WriteLine($"{kind,-9} {purpose,-22} REJECTED: {defect}");
                continue;
            }

            schedules.Add((schedule!, purpose));

            // The same two calls answer for every form -- this is the whole point of the uniform
            // surface required of the package.
            Console.WriteLine($"{kind,-9} {purpose,-22} {Format(schedule!.Previous(now)),-17} {Format(schedule.Next(now)),-17}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Which schedule fires first? ---");

        // With a uniform surface, "what runs next across every configured job" is an ordinary
        // minimum over the adapters -- no per-form special casing.
        var due = schedules
            .Select(entry => (entry.Purpose, entry.Schedule.Kind, Next: entry.Schedule.Next(now)))
            .Where(entry => entry.Next is not null)
            .OrderBy(entry => entry.Next!.Value)
            .ToArray();

        foreach ((string purpose, string kind, DateTime? next) in due)
        {
            Console.WriteLine($"  {Format(next),-17} {kind,-9} {purpose}");
        }

        Console.WriteLine();
        Console.WriteLine("--- The inclusive flag means the same thing everywhere ---");

        // Each form is queried at an instant it actually fires. Exclusive skips past it, inclusive
        // returns it -- identical semantics across all four.
        (string Kind, string Text, DateTime Hit)[] exactHits =
        [
            ("cron", "0 2 * * *", new DateTime(2026, 3, 11, 2, 0, 0)),
            ("rrule", "FREQ=WEEKLY;BYDAY=MO", new DateTime(2026, 3, 16, 0, 0, 0)),
            ("interval", "PT6H", new DateTime(2026, 3, 10, 12, 0, 0)),
            ("set", "DTSTART:20260105T090000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO", new DateTime(2026, 3, 16, 9, 0, 0)),
        ];

        Console.WriteLine($"{"kind",-9} {"query",-17} {"next(excl)",-17} {"next(incl)",-17}");
        Console.WriteLine($"{new string('-', 9)} {new string('-', 17)} {new string('-', 17)} {new string('-', 17)}");

        foreach ((string kind, string text, DateTime hit) in exactHits)
        {
            Schedule.TryCreate(kind, text, origin, out Schedule? schedule, out _);
            Console.WriteLine($"{kind,-9} {Format(hit),-17} {Format(schedule!.Next(hit)),-17} {Format(schedule.Next(hit, inclusive: true)),-17}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an optional instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "(none)";
}
