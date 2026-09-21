// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingQueries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates the asynchronous streaming projection: <c>ResolveAsync</c> yields a large multi-year range's
/// occurrences one civil year at a time with cooperative cancellation, instead of materializing the whole list
/// up front.
/// </summary>
public static class StreamingQueries
{
    /// <summary>
    /// Streams a decade of occurrences and stops early through cancellation.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "ResolveAsync - streaming a multi-year range",
            what: "Streams a decade of non-working occurrences a civil year at a time, counting them per year, "
                + "then streams the same range again and cancels partway through.",
            why: "Resolving a range means running every rule for every year in it, so a decade is ten times the "
                + "work of one year. Materializing that up front makes the caller wait for the last year before "
                + "seeing the first, which is the wrong shape for a report that renders as it goes or a query "
                + "the user may abandon. A civil year is the natural granularity because that is the unit the "
                + "rules are evaluated in - there is no partial year to yield. Cancellation is observed at the "
                + "same boundary, which is what makes an abandoned query actually stop rather than finish "
                + "quietly in the background.",
            expect: "The streamed results are element-for-element what the synchronous call returns - streaming "
                + "changes when you get them, not what they are. The cancelled run stops after the first year "
                + "rather than completing the decade, so the remaining years were never computed.");

        RunAsync().GetAwaiter().GetResult();

        Console.WriteLine();
    }

    /// <summary>
    /// Enumerates the stream: a full count first, then an early exit after the first year.
    /// </summary>
    private static async Task RunAsync()
    {
        INotableDateService service = AsiaPacificCalendarData.CreateService("AU");
        var decade = new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31));

        // The stream is element-for-element identical to the synchronous Resolve over the same range,
        // but yields as each civil year resolves - a consumer can process year one while year two computes.
        var perYear = new Dictionary<int, int>();
        await foreach (NotableDate occurrence in service.ResolveAsync(decade, "AU", NotableDateFilter.IsNonWorkingDay()))
            perYear[occurrence.Date.Year] = perYear.GetValueOrDefault(occurrence.Date.Year) + 1;

        Console.WriteLine($"  Streamed {perYear.Values.Sum()} non-working occurrences across {perYear.Count} years"
            + "  (element-for-element what the synchronous Resolve returns - streaming changes when, not what)");
        Console.WriteLine($"  first year 2020: {perYear[2020]}, last year 2029: {perYear[2029]}");

        // Cancellation is observed between years, so an early consumer stops the remaining computation.
        using var cancellation = new CancellationTokenSource();
        var streamedBeforeCancel = 0;

        try
        {
            await foreach (NotableDate occurrence in service.ResolveAsync(decade, "AU", cancellationToken: cancellation.Token))
            {
                streamedBeforeCancel++;

                if (occurrence.Date.Year > 2020)
                    await cancellation.CancelAsync();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"  Cancelled after streaming {streamedBeforeCancel} occurrences - later years never resolved."
                + "  (cancellation is observed between years, so an abandoned query stops rather than finishing in the background)");
        }
    }
}
