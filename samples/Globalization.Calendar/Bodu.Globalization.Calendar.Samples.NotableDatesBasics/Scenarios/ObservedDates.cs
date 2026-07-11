// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates observed-date substitution (the weekend/in-lieu machinery): when a holiday falls on a
/// weekend, adjustment policies in the rule data move the *observed* day off to a weekday, and the
/// resolved occurrence says exactly what happened — the emitted date, the actual (original) date, and
/// the observed flag.
/// </summary>
public static class ObservedDates
{
    /// <summary>
    /// Resolves Christmas/Boxing Day 2021 for Australia — the classic double-substitution year.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Observed dates: weekend substitution ---");

        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        // 2021: Christmas Day was a Saturday and Boxing Day a Sunday, so both were observed on the
        // following working days (Mon 27 and Tue 28 December) - conflict-aware substitution, since
        // Boxing Day's natural in-lieu slot (Monday) is already taken by Christmas.
        var december = service.Resolve(
            new DateRange(new DateOnly(2021, 12, 24), new DateOnly(2021, 12, 29)), "AU");

        foreach (NotableDate date in december.OrderBy(d => d.Date))
        {
            var detail = date.IsObserved
                ? $"observed (actual {date.ActualDate:yyyy-MM-dd}, reason: {date.AdjustmentReason})"
                : "actual";
            Console.WriteLine($"  {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-18} {detail}");
        }

        // A year where Christmas falls mid-week has no substitution - the same rules, quiet.
        var christmas2024 = service.Resolve(2024, "AU", NotableDateFilter.WithName("Christmas Day"));
        foreach (NotableDate date in christmas2024)
            Console.WriteLine($"  2024 contrast: {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek}) observed flag: {date.IsObserved}");

        Console.WriteLine();
    }
}
