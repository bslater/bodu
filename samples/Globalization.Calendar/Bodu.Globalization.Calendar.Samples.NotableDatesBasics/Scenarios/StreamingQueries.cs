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
        Console.WriteLine("--- ResolveAsync: streaming a multi-year range ---");

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

        Console.WriteLine($"Streamed {perYear.Values.Sum()} non-working occurrences across {perYear.Count} years");
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
            Console.WriteLine($"Cancelled after streaming {streamedBeforeCancel} occurrences - later years never resolved.");
        }
    }
}
