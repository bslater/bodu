// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoundedEnumeration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceRules.Scenarios;

/// <summary>
/// Demonstrates how an occurrence stream is bounded: by the rule's own <c>COUNT</c> or <c>UNTIL</c>,
/// by an explicit window passed to <c>GetOccurrences</c>, or by the caller taking a prefix of an
/// unbounded stream — plus the point queries <c>GetNextOccurrence</c> and
/// <c>GetPreviousOccurrence</c>.
/// </summary>
public static class BoundedEnumeration
{
    /// <summary>
    /// Shows each way of bounding a stream, then the two point queries.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Bounding a stream: COUNT, UNTIL, window, Take ---");

        var start = new DateTime(2026, 1, 1, 8, 0, 0);

        // COUNT stops the stream after n occurrences.
        RecurrenceRule byCount = RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=10;COUNT=4");
        Console.WriteLine($"COUNT=4        : {Join(byCount.GetOccurrences(start))}");

        // UNTIL stops it at an inclusive date-time bound.
        RecurrenceRule byUntil = RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=10;UNTIL=20260210T080000");
        Console.WriteLine($"UNTIL=20260210 : {Join(byUntil.GetOccurrences(start))}");

        // An unbounded rule yields an infinite stream, so the caller must bound it. Take is the
        // idiomatic way; the stream is lazy, so nothing beyond the prefix is computed.
        RecurrenceRule unbounded = RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=10");
        Console.WriteLine($"unbounded+Take : {Join(unbounded.GetOccurrences(start).Take(4))}");

        // The windowed overload takes the series start plus an inclusive from/to pair, and yields
        // only the occurrences inside the window -- useful for rendering one page of a calendar
        // without walking the series from the beginning.
        var windowFrom = new DateTime(2026, 2, 1);
        var windowTo = new DateTime(2026, 3, 31);
        Console.WriteLine($"window Feb-Mar : {Join(unbounded.GetOccurrences(start, windowFrom, windowTo))}");

        Console.WriteLine();
        Console.WriteLine("--- Point queries: next and previous ---");

        // GetNextOccurrence answers a single question without enumerating the series. It takes the
        // series start, the query instant, and whether an exact hit counts.
        RecurrenceRule weekly = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");
        var seriesStart = new DateTime(2026, 1, 5, 9, 0, 0);   // a Monday
        var query = new DateTime(2026, 2, 2, 9, 0, 0);         // also a Monday -- an exact hit

        Console.WriteLine($"series start   : {Format(seriesStart)}");
        Console.WriteLine($"query          : {Format(query)}");
        Console.WriteLine($"next (exclusive): {Format(weekly.GetNextOccurrence(seriesStart, query, inclusive: false))}");
        Console.WriteLine($"next (inclusive): {Format(weekly.GetNextOccurrence(seriesStart, query, inclusive: true))}");

        // Every form in this package answers previous as well as next, with the same inclusive
        // flag -- so "when was the last run?" needs no series walk either.
        Console.WriteLine($"prev (exclusive): {Format(weekly.GetPreviousOccurrence(seriesStart, query, inclusive: false))}");
        Console.WriteLine($"prev (inclusive): {Format(weekly.GetPreviousOccurrence(seriesStart, query, inclusive: true))}");

        Console.WriteLine();
        Console.WriteLine("--- Point queries past the end of a bounded rule ---");

        // A bounded rule reports null once the series is exhausted, rather than throwing or
        // searching forever.
        RecurrenceRule bounded = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
        var afterTheEnd = new DateTime(2026, 6, 1);
        Console.WriteLine($"occurrences     : {Join(bounded.GetOccurrences(start))}");
        Console.WriteLine($"next after Jun 1: {Format(bounded.GetNextOccurrence(start, afterTheEnd))}");
        Console.WriteLine($"prev before Jun1: {Format(bounded.GetPreviousOccurrence(start, afterTheEnd))}");

        // Likewise, asking for something before the series start has no answer looking backwards.
        var beforeTheStart = new DateTime(2025, 6, 1);
        Console.WriteLine($"prev before 2025: {Format(bounded.GetPreviousOccurrence(start, beforeTheStart))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats a sequence of occurrences as a comma-separated invariant-culture date list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTime> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

    /// <summary>
    /// Formats an optional instant, rendering the absent case as <c>(none)</c>.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "(none)";
}
