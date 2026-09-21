// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnreachableAndDefects.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.CronExpressions.Scenarios;

/// <summary>
/// Demonstrates the two failure surfaces: a syntactically valid expression that can never fire
/// (reported as no occurrence rather than an endless search), and malformed text rejected by
/// <c>TryParse</c> with a defect message that names the offending field.
/// </summary>
public static class UnreachableAndDefects
{
    /// <summary>
    /// Shows unreachable schedules, then the defect-naming parse overload.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Unreachable schedules and defect-naming parse failures",
            what: "Parses expressions that are syntactically valid but can never fire, parses malformed text "
                + "through TryParse and prints the defect message for each, and validates a block of "
                + "configuration entries reporting which are usable.",
            why: "A cron expression has two ways of being wrong and only one of them is a syntax error. '30 4 31 "
                + "2 *' parses cleanly and describes the 31st of February, so a scheduler that only validates "
                + "syntax accepts it and then never fires - a silent failure that looks like a working "
                + "configuration. Detecting unreachability turns that into something an operator can see at "
                + "deploy time. The defect-naming TryParse overload exists for the same reason: a boolean false "
                + "tells a configuration validator that something is wrong but not what, which means the error "
                + "the operator eventually sees has to be written by the caller and is usually worse than the "
                + "one the parser could have given.",
            expect: "The unreachable expressions parse but report no occurrence, rather than appearing to work. "
                + "Each malformed input comes back with a message naming the specific defect and not merely a "
                + "failure, which is what makes a configuration validator's output actionable.");

        // These expressions parse -- every field is in range -- but select a date that does not
        // exist in any year. The engine reports no occurrence instead of searching forever.
        var from = new DateTime(2026, 1, 1);
        string[] unreachable =
        [
            "* * 30 2 *",     // 30 February
            "* * 31 2 *",     // 31 February
            "* * 31 4 *",     // 31 April
            "* * 31 6 *",     // 31 June
            "* * 31 9 *",     // 31 September
            "* * 31 11 *",    // 31 November
        ];

        foreach (string expression in unreachable)
        {
            CronExpression cron = CronExpression.Parse(expression);
            Console.WriteLine($"  {expression,-14} next: {Format(cron.GetNextOccurrence(from))}");
        }

        // The 29th of February is reachable -- just rarely.
        CronExpression leapDay = CronExpression.Parse("0 0 29 2 *");
        Console.WriteLine($"  {"0 0 29 2 *",-14} next: {Format(leapDay.GetNextOccurrence(from))} (reachable, but only in leap years)");

        Console.WriteLine();
        Console.WriteLine("--- Malformed text: TryParse names the defect ---");

        // Hosts surface configuration errors verbatim, so the parse overload that reports a defect
        // message avoids exception-driven control flow while still explaining what went wrong.
        string[] malformed =
        [
            "",                      // empty
            "* * * *",               // four fields matches neither layout
            "* * * * * * *",         // and neither does seven
            "60 * * * *",            // minute out of range
            "* 24 * * *",            // hour out of range
            "* * 32 * *",            // day-of-month out of range
            "* * * 13 *",            // month out of range
            "* * * * 8",             // weekday out of range
            "5-1 * * * *",           // reversed range
            "1/0 * * * *",           // zero step
            "* * * * MON#1",         // Quartz '#' -- a planned follow-on, not silently ignored
            "* * L * *",             // Quartz 'L'
            "* * * * ?",             // Quartz '?'
            "@every_minute",         // not a crontab(5) macro
        ];

        foreach (string expression in malformed)
        {
            bool parsed = CronExpression.TryParse(expression, out CronExpression? result, out string? defect);
            string label = expression.Length == 0 ? "(empty)" : $"'{expression}'";
            Console.WriteLine($"  {label,-18} parsed={parsed,-6} {defect}");

            // The out parameter is null whenever parsing failed, so a caller can branch on either.
            if (parsed && result is not null)
            {
                Console.WriteLine($"  {new string(' ', 18)} -> {result}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Validating a configuration block ---");

        // The shape a host actually uses: read schedules from configuration, keep the good ones,
        // and report the bad ones with enough detail for an operator to fix the file.
        (string Key, string Schedule)[] configuration =
        [
            ("nightly-backup", "0 2 * * *"),
            ("hourly-sync", "@hourly"),
            ("weekday-report", "0 8 * * 1-5"),
            ("typo", "0 8 * * MONDAY"),
            ("out-of-range", "0 25 * * *"),
        ];

        foreach ((string key, string schedule) in configuration)
        {
            if (CronExpression.TryParse(schedule, out CronExpression? cron, out string? defect))
            {
                Console.WriteLine($"  [ ok ] {key,-15} '{schedule}' -> next {Format(cron.GetNextOccurrence(from))}");
            }
            else
            {
                Console.WriteLine($"  [FAIL] {key,-15} '{schedule}' -> {defect}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an optional instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "(never)";
}
