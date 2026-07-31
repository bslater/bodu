// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeCounting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.WorkingDays.Scenarios;

/// <summary>
/// Demonstrates counting and enumerating working days over a window — the SLA/duration questions:
/// "how many business days between these dates?" and "which days are they?".
/// </summary>
public static class RangeCounting
{
    /// <summary>
    /// Counts and lists the working days of April 2024 (which contains Anzac Day).
    /// </summary>
    /// <param name="service">The notable-date service supplying holiday knowledge.</param>
    public static void Run(INotableDateService service)
    {
        Console.WriteLine("--- Counting and enumerating working days ---");

        var start = new DateOnly(2024, 4, 1);
        var end = new DateOnly(2024, 4, 30);

        // April 2024: 30 days, 8 weekend days, 2 public holidays that fall on weekdays
        // (Easter Monday 04-01 and Anzac Day 04-25).
        var count = start.WorkingDaysBetween(end, service, "AU");   // both endpoints inclusive
        Console.WriteLine($"Working days in April 2024 (AU): {count}");

        // Enumerate lazily - here, the working days of Anzac week only.
        IEnumerable<DateOnly> anzacWeek = new DateOnly(2024, 4, 22)
            .EnumerateWorkingDays(new DateOnly(2024, 4, 26), service, "AU");
        Console.WriteLine($"Anzac week working days: {string.Join(", ", anzacWeek.Select(d => d.ToString("MM-dd")))}");

        Console.WriteLine();
    }
}
