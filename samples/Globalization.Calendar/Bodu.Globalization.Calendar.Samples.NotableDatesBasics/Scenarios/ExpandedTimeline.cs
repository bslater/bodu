// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExpandedTimeline.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates expanding observed-only results into a full timeline: the AU pack emits each weekend-substituted
/// holiday once, on its observed day, so the nominal day survives only as <c>ActualDate</c>.
/// <c>WithActualOccurrences()</c> synthesizes the missing actual occurrences, turning the observed-only result into
/// the sequential actual + observed story.
/// </summary>
public static class ExpandedTimeline
{
    /// <summary>
    /// Resolves Christmas/Boxing Day 2021 for Australia and expands the observed-only result into the 25–28 December
    /// timeline.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Expanded timeline: WithActualOccurrences ---");

        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        // 2021 again: the pack emits ObservedOnly, so the raw result holds Christmas on Mon 27 and
        // Boxing Day on Tue 28 - the weekend days 25 and 26 appear only in ActualDate.
        var resolved = service.Resolve(
            new DateRange(new DateOnly(2021, 12, 25), new DateOnly(2021, 12, 28)), "AU");

        Console.WriteLine("  Raw observed-only result:");
        foreach (NotableDate date in resolved)
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-18} actual {date.ActualDate:yyyy-MM-dd}");

        // Expand: each observed occurrence whose actual day is absent gains a synthesized actual
        // occurrence, and the result is re-sorted - the consumer-side ActualAndObserved view.
        var timeline = resolved.WithActualOccurrences();

        Console.WriteLine("  Expanded timeline:");
        foreach (NotableDate date in timeline)
        {
            var detail = date.IsObserved
                ? $"observed (in lieu of {date.ActualDate:yyyy-MM-dd})"
                : "actual";
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-18} {detail}");
        }

        // The expansion is idempotent, and a no-op when nothing was substituted.
        Console.WriteLine($"  Expanding again adds nothing: {timeline.WithActualOccurrences().Count} occurrences either way.");

        Console.WriteLine();
    }
}
