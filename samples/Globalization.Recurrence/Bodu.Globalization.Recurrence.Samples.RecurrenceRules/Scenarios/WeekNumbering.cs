// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekNumbering.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceRules.Scenarios;

/// <summary>
/// Demonstrates that <c>WKST</c> reparameterises week numbering rather than merely shifting weekly
/// intervals: it changes which dates <c>BYWEEKNO</c> resolves to, and which years have a
/// fifty-third week. Numbered weeks straddle the calendar year, so week 1 can begin in December.
/// </summary>
public static class WeekNumbering
{
    /// <summary>
    /// Compares week numbering under different week starts and shows year-straddling weeks.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- WKST shifts a weekly interval ---");

        // With INTERVAL=2 the week boundary decides which side of the fortnight a date lands on.
        // Under WKST=MO the Sunday closes the current week; under WKST=SU it opens the next one,
        // so the same rule selects different dates.
        var start = new DateTime(1997, 8, 5);  // a Tuesday
        RecurrenceRule mondayStart = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU;WKST=MO;COUNT=4");
        RecurrenceRule sundayStart = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU;WKST=SU;COUNT=4");
        Console.WriteLine($"WKST=MO : {Join(mondayStart.GetOccurrences(start))}");
        Console.WriteLine($"WKST=SU : {Join(sundayStart.GetOccurrences(start))}");

        Console.WriteLine();
        Console.WriteLine("--- BYWEEKNO: weeks straddle the calendar year ---");

        // Week 1 is the first week containing at least four days of the new year. In 2026 that
        // week begins on Monday 29 December 2025 -- so a BYWEEKNO=1 rule yields dates in the
        // PREVIOUS calendar year, which a naive "week 1 starts in January" implementation misses.
        RecurrenceRule weekOne = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO;COUNT=3");
        Console.WriteLine($"BYWEEKNO=1;BYDAY=MO      : {Join(weekOne.GetOccurrences(new DateTime(2025, 1, 1)))}");

        // Without a BYDAY limit, BYWEEKNO expands to the WHOLE week -- all seven days.
        RecurrenceRule wholeWeek = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=1;COUNT=7");
        Console.WriteLine($"BYWEEKNO=1 (whole week)  : {Join(wholeWeek.GetOccurrences(new DateTime(2026, 1, 1)))}");

        Console.WriteLine();
        Console.WriteLine("--- BYWEEKNO honours WKST ---");

        // Changing the week start moves every week boundary, so week 20 resolves to a different
        // seven-day span. The two rules below differ only in WKST.
        RecurrenceRule week20Monday = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO;WKST=MO;COUNT=2");
        RecurrenceRule week20Sunday = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO;WKST=SU;COUNT=2");
        Console.WriteLine($"week 20, WKST=MO         : {Join(week20Monday.GetOccurrences(new DateTime(2026, 1, 1)))}");
        Console.WriteLine($"week 20, WKST=SU         : {Join(week20Sunday.GetOccurrences(new DateTime(2026, 1, 1)))}");

        Console.WriteLine();
        Console.WriteLine("--- The fifty-third week exists only in some years ---");

        // A year has 53 numbered weeks only when its length and start line up. Asking for week 53
        // therefore skips most years entirely -- another expansion that must not be clamped to 52.
        RecurrenceRule week53 = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=53;BYDAY=MO;COUNT=3");
        Console.WriteLine($"BYWEEKNO=53;BYDAY=MO     : {Join(week53.GetOccurrences(new DateTime(2020, 1, 1)))}");

        // Negative week numbers count back from the end of the year: -1 is the last numbered week.
        RecurrenceRule lastWeek = RecurrenceRule.Parse("FREQ=YEARLY;BYWEEKNO=-1;BYDAY=MO;COUNT=3");
        Console.WriteLine($"BYWEEKNO=-1;BYDAY=MO     : {Join(lastWeek.GetOccurrences(new DateTime(2026, 1, 1)))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats a sequence of occurrences as a comma-separated invariant-culture date list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTime> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
}
