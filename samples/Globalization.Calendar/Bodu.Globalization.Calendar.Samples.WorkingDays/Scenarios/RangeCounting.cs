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
        SampleConsole.Scenario(
            "Counting and enumerating working days",
            what: "Counts the working days in April 2024, a month containing two weekday public holidays, then "
                + "enumerates the working days of the Anzac Day week.",
            why: "These are the two shapes an SLA or duration question takes. A count answers 'how long does "
                + "this have' and is the one most often computed wrongly, because dividing calendar days by "
                + "seven and multiplying by five is close enough to look right and wrong in exactly the months "
                + "that matter. The enumeration answers 'which days are they', which a scheduler needs in order "
                + "to place work, and it is lazy so asking about a year does not materialize a year. Both are "
                + "endpoint-inclusive, which is worth stating because off-by-one on a billing period is a real "
                + "invoice.",
            expect: "April 2024 has 30 days, 8 of them weekend days, and two public holidays that fall on "
                + "weekdays - so the count is 20 rather than the 22 a weekend-only calculation gives. The "
                + "enumerated week omits the holiday and the weekend without the caller filtering anything.");

        var start = new DateOnly(2024, 4, 1);
        var end = new DateOnly(2024, 4, 30);

        // April 2024: 30 days, 8 weekend days, 2 public holidays that fall on weekdays
        // (Easter Monday 04-01 and Anzac Day 04-25).
        var count = start.WorkingDaysBetween(end, service, "AU");   // both endpoints inclusive
        Console.WriteLine($"  Working days in April 2024 (AU): {count}"
            + "  (30 days less 8 weekend days less the two weekday holidays - a weekend-only count would say 22)");

        // Enumerate lazily - here, the working days of Anzac week only.
        IEnumerable<DateOnly> anzacWeek = new DateOnly(2024, 4, 22)
            .EnumerateWorkingDays(new DateOnly(2024, 4, 26), service, "AU");
        Console.WriteLine($"  Anzac week working days: {string.Join(", ", anzacWeek.Select(d => d.ToString("MM-dd")))}"
            + "  (the holiday and the weekend are simply absent - enumerated lazily, so asking about a year does not materialize one)");

        Console.WriteLine();
    }
}
