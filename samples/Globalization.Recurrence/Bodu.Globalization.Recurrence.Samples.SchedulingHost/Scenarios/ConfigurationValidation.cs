// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.SchedulingHost.Scenarios;

/// <summary>
/// Demonstrates validating a block of configured schedules at startup: every form exposes a
/// <c>TryParse</c> overload that reports a defect message naming the offending token, so a host can
/// fail fast with diagnostics an operator can act on without exception-driven control flow.
/// </summary>
public static class ConfigurationValidation
{
    /// <summary>
    /// Validates a mixed configuration block and reports each rejection.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Validating a mixed configuration block ---");

        var origin = new DateTime(2026, 1, 1, 0, 0, 0);
        var now = new DateTime(2026, 3, 10, 14, 32, 0);

        // A realistic configuration file: several forms, several mistakes. The point is that every
        // rejection explains itself well enough to fix without reading a specification.
        (string Job, string Kind, string Text)[] configuration =
        [
            ("backup.nightly", "cron", "0 2 * * *"),
            ("backup.weekly", "cron", "0 3 * * SUN"),
            ("probe.health", "interval", "PT30S"),
            ("report.weekly", "rrule", "FREQ=WEEKLY;BYDAY=MO;BYHOUR=8"),
            ("close.monthly", "rrule", "FREQ=MONTHLY;BYDAY=-1FR"),
            ("standup", "set", "DTSTART:20260105T090000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR"),
            ("bad.cronRange", "cron", "0 25 * * *"),
            ("bad.cronName", "cron", "0 8 * * MONDAY"),
            ("bad.quartz", "cron", "0 0 L * *"),
            ("bad.duration", "interval", "PT5"),
            ("bad.negative", "interval", "-P1D"),
            ("bad.freq", "rrule", "FREQ=FORTNIGHTLY"),
            ("bad.setNoStart", "set", "RRULE:FREQ=DAILY"),
            ("bad.kind", "quartz", "0 0 12 * * ?"),
        ];

        var accepted = new List<(string Job, Schedule Schedule)>();
        var rejected = new List<(string Job, string Kind, string Text, string Defect)>();

        foreach ((string job, string kind, string text) in configuration)
        {
            if (Schedule.TryCreate(kind, text, origin, out Schedule? schedule, out string? defect))
            {
                accepted.Add((job, schedule!));
            }
            else
            {
                rejected.Add((job, kind, text, defect!));
            }
        }

        Console.WriteLine($"accepted : {accepted.Count}");
        foreach ((string job, Schedule schedule) in accepted)
        {
            Console.WriteLine($"  [ ok ] {job,-18} {schedule.Kind,-9} next {Format(schedule.Next(now))}");
        }

        Console.WriteLine();
        Console.WriteLine($"rejected : {rejected.Count}");
        foreach ((string job, string kind, string text, string defect) in rejected)
        {
            Console.WriteLine($"  [FAIL] {job,-18} {kind,-9} '{Flatten(text)}'");
            Console.WriteLine($"         {defect}");
        }

        Console.WriteLine();
        Console.WriteLine("--- A schedule that parses but can never fire ---");

        // Validation has a second tier: syntax is not the only way a schedule can be useless. An
        // expression selecting a date that does not exist parses cleanly and then never fires, so
        // a host that cares should probe for a first occurrence as part of startup validation.
        (string Job, string Kind, string Text)[] parseable =
        [
            ("bad.feb30", "cron", "0 0 30 2 *"),
            ("bad.sep31", "cron", "0 0 31 9 *"),
            ("expired", "rrule", "FREQ=DAILY;UNTIL=20200101T000000"),
            ("fine", "cron", "0 0 29 2 *"),
        ];

        foreach ((string job, string kind, string text) in parseable)
        {
            Schedule.TryCreate(kind, text, origin, out Schedule? schedule, out _);
            DateTime? first = schedule!.Next(now);

            Console.WriteLine(first is null
                ? $"  [WARN] {job,-15} '{text}' parses, but has no occurrence after {Format(now)}"
                : $"  [ ok ] {job,-15} '{text}' next {Format(first)}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Collapses a multi-line property block onto one line for compact display.
    /// </summary>
    /// <param name="text">The text to flatten.</param>
    /// <returns>The single-line form.</returns>
    private static string Flatten(string text) =>
        text.Replace("\r\n", " | ", StringComparison.Ordinal);

    /// <summary>
    /// Formats an optional instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "(none)";
}
