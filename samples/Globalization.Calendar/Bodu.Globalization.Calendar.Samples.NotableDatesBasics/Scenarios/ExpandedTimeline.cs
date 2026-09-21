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
        SampleConsole.Scenario(
            "Expanding an observed-only result into a full timeline",
            what: "Resolves the 2021 Christmas window, prints the raw observed-only result, expands it with "
                + "WithActualOccurrences, prints the expanded timeline, and expands once more to show the "
                + "operation is idempotent.",
            why: "A pack has to choose what a substituted holiday emits, and the AU pack emits it once, on the "
                + "observed day. That is the right default, because that is the day that matters operationally "
                + "and emitting both would double-count every substituted holiday in a payroll sum. But a "
                + "calendar rendering December needs the nominal day visible too - 25 December is Christmas even "
                + "in a year when nobody gets it off then. This expansion is the consumer-side answer, "
                + "synthesizing the missing actual occurrences rather than making the pack emit a shape that is "
                + "wrong for the other half of its callers.",
            expect: "The raw result has two occurrences on the observed weekdays, with the weekend days present "
                + "only as ActualDate. After expanding there are four, in date order, each labelled as the "
                + "nominal day or the day in lieu. Expanding again changes nothing, so it is safe to apply "
                + "without tracking whether it has already run.");

        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        // 2021 again: the pack emits ObservedOnly, so the raw result holds Christmas on Mon 27 and
        // Boxing Day on Tue 28 - the weekend days 25 and 26 appear only in ActualDate.
        var resolved = service.Resolve(
            new DateRange(new DateOnly(2021, 12, 25), new DateOnly(2021, 12, 28)), "AU");

        Console.WriteLine("  Raw observed-only result (each holiday emitted once, on the day off - the weekend days survive only as ActualDate):");
        foreach (NotableDate date in resolved)
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-18} actual {date.ActualDate:yyyy-MM-dd}");

        // Expand: each observed occurrence whose actual day is absent gains a synthesized actual
        // occurrence, and the result is re-sorted - the consumer-side ActualAndObserved view.
        var timeline = resolved.WithActualOccurrences();

        Console.WriteLine("  Expanded timeline (the nominal days synthesized back in and re-sorted - what a calendar rendering December needs):");
        foreach (NotableDate date in timeline)
        {
            var detail = date.IsObserved
                ? $"observed (in lieu of {date.ActualDate:yyyy-MM-dd})"
                : "actual";
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-18} {detail}");
        }

        // The expansion is idempotent, and a no-op when nothing was substituted.
        Console.WriteLine($"  Expanding again adds nothing: {timeline.WithActualOccurrences().Count} occurrences either way."
            + "  (idempotent, so it is safe to apply without tracking whether it already ran)");

        Console.WriteLine();
    }
}
