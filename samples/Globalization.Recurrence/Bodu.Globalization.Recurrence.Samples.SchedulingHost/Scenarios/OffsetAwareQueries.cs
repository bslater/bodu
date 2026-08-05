// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OffsetAwareQueries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.SchedulingHost.Scenarios;

/// <summary>
/// Demonstrates the <see cref="DateTimeOffset" /> query surface across all four schedule forms. A host normally holds
/// offset-bearing instants rather than bare <see cref="DateTime" /> values, and every form carries a parallel set of
/// overloads for them.
/// </summary>
public static class OffsetAwareQueries
{
    /// <summary>
    /// Runs each form's offset overloads, then shows what the offset does and does not affect.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Every form has a DateTimeOffset surface ---");

        // A single offset used throughout, so the difference between forms is the only variable.
        var offset = TimeSpan.FromHours(10);
        var origin = new DateTimeOffset(2026, 1, 5, 9, 0, 0, offset);   // a Monday
        var now = new DateTimeOffset(2026, 3, 10, 14, 32, 0, offset);   // a Tuesday

        Console.WriteLine($"origin : {Format(origin)}");
        Console.WriteLine($"query  : {Format(now)}");
        Console.WriteLine();

        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");
        CronExpression cron = CronExpression.Parse("0 2 * * *");
        AnchoredInterval interval = AnchoredInterval.Parse("PT6H");
        RecurrenceSet set = RecurrenceSet.Parse(
            "DTSTART:20260105T090000\r\nRRULE:FREQ=WEEKLY;BYDAY=MO\r\nEXDATE:20260316T090000");

        Console.WriteLine($"{"form",-10} {"previous",-26} {"next",-26}");
        Console.WriteLine($"{new string('-', 10)} {new string('-', 26)} {new string('-', 26)}");

        // The rule and the interval take the series origin (or anchor) first; the cron expression and
        // the set do not need one. That difference is the only thing an adapter has to absorb.
        Console.WriteLine($"{"rrule",-10} {Format(rule.GetPreviousOccurrence(origin, now)),-26} {Format(rule.GetNextOccurrence(origin, now)),-26}");
        Console.WriteLine($"{"cron",-10} {Format(cron.GetPreviousOccurrence(now)),-26} {Format(cron.GetNextOccurrence(now)),-26}");
        Console.WriteLine($"{"interval",-10} {Format(interval.GetPreviousOccurrence(origin, now)),-26} {Format(interval.GetNextOccurrence(origin, now)),-26}");
        Console.WriteLine($"{"set",-10} {Format(set.GetPreviousOccurrence(now)),-26} {Format(set.GetNextOccurrence(now)),-26}");

        Console.WriteLine();
        Console.WriteLine("--- Enumeration carries the offset too ---");

        // The enumeration overloads mirror the DateTime ones, unbounded and windowed.
        Console.WriteLine($"rule, next 3        : {Join(rule.GetOccurrences(origin).Take(3))}");
        Console.WriteLine($"interval, next 3    : {Join(interval.GetOccurrences(origin).Take(3))}");

        var windowFrom = new DateTimeOffset(2026, 2, 1, 0, 0, 0, offset);
        var windowTo = new DateTimeOffset(2026, 2, 28, 23, 59, 59, offset);
        Console.WriteLine($"rule, February      : {Join(rule.GetOccurrences(origin, windowFrom, windowTo))}");
        Console.WriteLine($"interval, 6 Jan     : {Join(interval.GetOccurrences(origin, new DateTimeOffset(2026, 1, 6, 0, 0, 0, offset), new DateTimeOffset(2026, 1, 6, 23, 59, 59, offset)))}");

        Console.WriteLine();
        Console.WriteLine("--- The offset rides along; it is never converted ---");

        // The library resolves no time zone, so it cannot convert an offset and does not try. Each
        // answer carries the offset its arguments carried.
        foreach (TimeSpan candidate in new[] { TimeSpan.Zero, TimeSpan.FromHours(10), TimeSpan.FromHours(-8) })
        {
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, candidate);
            DateTimeOffset? next = rule.GetNextOccurrence(start, new DateTimeOffset(2026, 1, 7, 0, 0, 0, candidate));
            Console.WriteLine($"  start {Format(start)} -> next {Format(next)}");
        }

        Console.WriteLine("  (the same wall clock in each case -- the offset selects no different dates)");

        Console.WriteLine();
        Console.WriteLine("--- Where the offset does matter: comparing across offsets ---");

        // An offset is not decoration: two DateTimeOffset values with different offsets are compared
        // as absolute instants. An anchor recorded in UTC therefore composes correctly with a query
        // instant recorded in +10:00 -- they name the same moment, so the answer is the same.
        var anchorUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var anchorPlusTen = new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.FromHours(10));
        Console.WriteLine($"  anchor A : {Format(anchorUtc)}");
        Console.WriteLine($"  anchor B : {Format(anchorPlusTen)}");
        Console.WriteLine($"  same instant : {anchorUtc == anchorPlusTen}");

        var queryUtc = new DateTimeOffset(2026, 4, 1, 13, 0, 0, TimeSpan.Zero);
        Console.WriteLine($"  PT6H from A, query {Format(queryUtc)} -> {Format(interval.GetNextOccurrence(anchorUtc, queryUtc))}");
        Console.WriteLine($"  PT6H from B, query {Format(queryUtc)} -> {Format(interval.GetNextOccurrence(anchorPlusTen, queryUtc))}");
        Console.WriteLine("  (the two anchors name one moment, so the grids coincide and both answers are the");
        Console.WriteLine("   same instant, rendered in the query's offset)");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an optional offset instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTimeOffset? value) =>
        value?.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture) ?? "(none)";

    /// <summary>
    /// Formats a sequence of offset occurrences as a comma-separated invariant-culture list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTimeOffset> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("MM-dd HH:mm zzz", CultureInfo.InvariantCulture)));
}
