// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DurationGrammar.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence.Samples.AnchoredIntervals.Scenarios;

/// <summary>
/// Demonstrates the boundary of the RFC 5545 §3.3.6 duration grammar — what parses, what does not,
/// and the defect message that names the offending token when text is rejected.
/// </summary>
public static class DurationGrammar
{
    /// <summary>
    /// Walks accepted and rejected duration text, showing the defect message for each rejection.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Accepted duration text ---");

        // The full production: [sign] "P" (weeks / days [time] / time), where time is
        // "T" followed by at least one of hours, minutes, seconds.
        (string Text, string Note)[] accepted =
        [
            ("P1W", "weeks"),
            ("P3D", "days"),
            ("PT2H", "hours"),
            ("PT30M", "minutes"),
            ("PT45S", "seconds"),
            ("P1DT2H", "days and hours"),
            ("P1DT2H3M4S", "every time component"),
            ("PT1H30M", "hours and minutes"),
            ("+PT4H", "an explicit positive sign"),
            ("p1w", "the grammar is case-insensitive"),
        ];

        foreach ((string text, string note) in accepted)
        {
            bool parsed = AnchoredInterval.TryParse(text, out AnchoredInterval? interval, out string? defect);
            Console.WriteLine(parsed
                ? $"  {text,-14} -> {interval,-12} ({note})"
                : $"  {text,-14} -> REJECTED: {defect}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Rejected duration text ---");

        // Each rejection names the offending token rather than reporting a generic failure, so a
        // host can put the operator straight onto the mistake.
        (string Text, string Why)[] rejected =
        [
            ("", "empty"),
            ("P", "no components"),
            ("PT", "a time designator with nothing after it"),
            ("P1DT", "the same, after a valid day component"),
            ("1H", "missing the leading P"),
            ("P1H", "hours must follow a T designator"),
            ("PT1D", "days must precede the T designator"),
            ("P1W2D", "weeks do not combine with other components"),
            ("PT0.5H", "components are whole numbers"),
            ("P1X", "unknown unit"),
            ("PT4X", "unknown time unit"),
            ("P0D", "an interval must be positive"),
            ("-PT1H", "a negative duration is not an interval"),
            ("PT1H30", "a trailing number with no unit"),
        ];

        foreach ((string text, string why) in rejected)
        {
            bool parsed = AnchoredInterval.TryParse(text, out AnchoredInterval? interval, out string? defect);
            string label = text.Length == 0 ? "(empty)" : text;
            Console.WriteLine(parsed
                ? $"  {label,-14} -> UNEXPECTEDLY ACCEPTED as {interval} ({why})"
                : $"  {label,-14} -> {defect}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Parse throws where TryParse reports ---");

        // The throwing overload carries the same message, so nothing is lost by using it where an
        // invalid value really is exceptional.
        try
        {
            _ = AnchoredInterval.Parse("P1DT");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  Parse(\"P1DT\") threw {ex.GetType().Name}");
            Console.WriteLine($"    {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Validating a configuration block ---");

        // The shape a host actually uses: keep the good schedules, report the bad ones with
        // enough detail to fix the file without reading the RFC.
        (string Key, string Interval)[] configuration =
        [
            ("health-check", "PT30S"),
            ("metrics-flush", "PT5M"),
            ("cache-sweep", "PT6H"),
            ("retention-scan", "P1W"),
            ("typo", "PT5"),
            ("negative", "-P1D"),
        ];

        foreach ((string key, string text) in configuration)
        {
            if (AnchoredInterval.TryParse(text, out AnchoredInterval? interval, out string? defect))
            {
                Console.WriteLine($"  [ ok ] {key,-15} '{text}' -> {interval.Interval}");
            }
            else
            {
                Console.WriteLine($"  [FAIL] {key,-15} '{text}' -> {defect}");
            }
        }

        Console.WriteLine();
    }
}
