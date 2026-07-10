// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SubdivisionShadowing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates ISO 3166-2 subdivision shadowing: a state territory (AU-VIC) sees the national rules
/// plus its own, and where both define the same concept the most specific rule wins. One country
/// resource serves every subdivision — the territory string picks the view.
/// </summary>
public static class SubdivisionShadowing
{
    /// <summary>
    /// Compares national and state views of the same year, and pins Victoria's Labour Day.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Subdivision shadowing: AU vs AU-NSW vs AU-VIC ---");

        // A subdivision territory selects the same country resource; filtering happens per query.
        NotableDateService au = AsiaPacificCalendarData.CreateService("AU");
        NotableDateService vic = AsiaPacificCalendarData.CreateService("AU-VIC");
        NotableDateService nsw = AsiaPacificCalendarData.CreateService("AU-NSW");

        var national = au.Resolve(2026, "AU");
        var victorian = vic.Resolve(2026, "AU-VIC");
        var newSouthWales = nsw.Resolve(2026, "AU-NSW");

        // The state views are supersets: national rules plus state-only concepts.
        Console.WriteLine($"2026 notable dates - AU: {national.Count}, AU-VIC: {victorian.Count}, AU-NSW: {newSouthWales.Count}");

        // Labour Day is the classic shadowing example: one concept, a different rule per state.
        // Victoria observes the second Monday of March - 2026-03-09 is a published known-good date.
        NotableDate vicLabourDay = victorian.First(d => d.DisplayName.Contains("Labour", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"AU-VIC Labour Day 2026 : {vicLabourDay.Date:yyyy-MM-dd} ({vicLabourDay.Date.DayOfWeek})");

        // New South Wales observes the first Monday of October - same concept, different date.
        NotableDate nswLabourDay = newSouthWales.First(d => d.DisplayName.Contains("Labour", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"AU-NSW Labour Day 2026 : {nswLabourDay.Date:yyyy-MM-dd} ({nswLabourDay.Date.DayOfWeek})");

        // State-only concepts simply do not exist in the national view.
        var vicOnly = victorian.Select(d => d.DisplayName)
            .Except(national.Select(d => d.DisplayName))
            .ToArray();
        Console.WriteLine($"Concepts only in AU-VIC: {string.Join(", ", vicOnly)}");

        Console.WriteLine();
    }
}
