// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredQueries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.AnchoredIntervals.Scenarios;

/// <summary>
/// Demonstrates the anchor-per-query design: one <see cref="AnchoredInterval" /> instance serves any
/// number of series, because the anchor that positions the grid is passed to each call rather than
/// captured by the type.
/// </summary>
public static class AnchoredQueries
{
    /// <summary>
    /// Queries one interval against several anchors, in both directions and over a window.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Occurrences fall at anchor + k x interval, for k >= 1 ---");

        AnchoredInterval sixHourly = AnchoredInterval.Parse("PT6H");
        var anchor = new DateTime(2026, 4, 1, 0, 0, 0);

        // The anchor itself is NOT an occurrence: the series starts one interval later. That makes
        // the anchor a "last run" marker rather than a series member, which is what a scheduler
        // wants when it records when a job last completed.
        Console.WriteLine($"interval : {sixHourly}");
        Console.WriteLine($"anchor   : {Format(anchor)}");
        Console.WriteLine($"first 5  : {Join(sixHourly.GetOccurrences(anchor).Take(5))}");

        Console.WriteLine();
        Console.WriteLine("--- One interval, many anchors ---");

        // Because the interval carries no anchor, the same instance answers for every series --
        // so a host can hold one instance per configured schedule and query it per job.
        DateTime[] anchors =
        [
            new DateTime(2026, 4, 1, 0, 0, 0),
            new DateTime(2026, 4, 1, 2, 30, 0),
            new DateTime(2026, 4, 1, 17, 45, 0),
        ];

        foreach (DateTime each in anchors)
        {
            Console.WriteLine($"  anchor {Format(each)} -> {Join(sixHourly.GetOccurrences(each).Take(3))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Next and previous, with the inclusive flag ---");

        // Both directions take the anchor, the query instant, and whether an exact hit counts.
        var exactHit = new DateTime(2026, 4, 1, 12, 0, 0);   // anchor + 2 intervals
        Console.WriteLine($"anchor          : {Format(anchor)}");
        Console.WriteLine($"query           : {Format(exactHit)} (exactly anchor + 2 intervals)");
        Console.WriteLine($"next  exclusive : {Format(sixHourly.GetNextOccurrence(anchor, exactHit))}");
        Console.WriteLine($"next  inclusive : {Format(sixHourly.GetNextOccurrence(anchor, exactHit, inclusive: true))}");
        Console.WriteLine($"prev  exclusive : {Format(sixHourly.GetPreviousOccurrence(anchor, exactHit))}");
        Console.WriteLine($"prev  inclusive : {Format(sixHourly.GetPreviousOccurrence(anchor, exactHit, inclusive: true))}");

        // Looking back before the first occurrence has no answer: the anchor is not a member.
        var beforeFirst = new DateTime(2026, 4, 1, 3, 0, 0);
        Console.WriteLine($"prev before the first occurrence : {Format(sixHourly.GetPreviousOccurrence(anchor, beforeFirst))}");

        Console.WriteLine();
        Console.WriteLine("--- A query far from the anchor ---");

        // The grid position is computed arithmetically rather than by stepping, so a query years
        // away from its anchor costs the same as one moments away.
        var farFuture = new DateTime(2031, 7, 4, 9, 17, 0);
        Console.WriteLine($"anchor          : {Format(anchor)}");
        Console.WriteLine($"query           : {Format(farFuture)} (over five years later)");
        Console.WriteLine($"next            : {Format(sixHourly.GetNextOccurrence(anchor, farFuture))}");
        Console.WriteLine($"previous        : {Format(sixHourly.GetPreviousOccurrence(anchor, farFuture))}");

        Console.WriteLine();
        Console.WriteLine("--- Windowed enumeration ---");

        // The windowed overload takes the anchor plus an inclusive from/to pair. The stream is
        // otherwise infinite, so the window (or a Take) is what bounds it.
        AnchoredInterval daily = AnchoredInterval.Parse("P1D");
        var windowFrom = new DateTime(2026, 4, 10);
        var windowTo = new DateTime(2026, 4, 14);
        Console.WriteLine($"P1D anchored {Format(anchor)}, window {Format(windowFrom)} .. {Format(windowTo)}:");
        Console.WriteLine($"  {Join(daily.GetOccurrences(anchor, windowFrom, windowTo))}");

        Console.WriteLine();
        Console.WriteLine("--- DateTimeOffset: the offset rides along unchanged ---");

        // The offset overloads preserve the anchor's offset on every occurrence. This library
        // resolves no time zone identifiers and consults no machine clock, so an interval is exact
        // elapsed time -- it never gains or loses an hour at a daylight-saving transition.
        var offsetAnchor = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.FromHours(10));
        Console.WriteLine($"anchor   : {offsetAnchor:yyyy-MM-dd HH:mm zzz}");
        foreach (DateTimeOffset occurrence in sixHourly.GetOccurrences(offsetAnchor).Take(3))
        {
            Console.WriteLine($"  {occurrence:yyyy-MM-dd HH:mm zzz}");
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

    /// <summary>
    /// Formats a sequence of occurrences as a comma-separated invariant-culture list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTime> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture)));
}
