// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByPartSemantics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceRules.Scenarios;

/// <summary>
/// Demonstrates the four <c>BY*</c> behaviours that recurrence implementations most often disagree
/// on: invalid dates are skipped rather than clamped, the occurrence set is a genuine set,
/// <c>BYSETPOS</c> indexes the whole frequency period, and a <c>BY</c> filter never re-anchors the
/// interval it is applied to.
/// </summary>
public static class ByPartSemantics
{
    /// <summary>
    /// Runs one demonstration for each of the four semantics.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Invalid dates are skipped, never clamped ---");

        // A monthly rule anchored on the 31st yields only the months that HAVE a 31st. February,
        // April, June, September and November are omitted -- not rolled back to the 28th or 30th.
        // This is the single most common false bug report filed against recurrence libraries.
        RecurrenceRule monthly = RecurrenceRule.Parse("FREQ=MONTHLY;COUNT=6");
        var from31st = new DateTime(2026, 1, 31, 0, 0, 0);
        Console.WriteLine($"FREQ=MONTHLY from 2026-01-31 : {Join(monthly.GetOccurrences(from31st))}");

        // BYMONTHDAY=31 states the same thing explicitly, and behaves identically.
        RecurrenceRule explicitDay = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=31;COUNT=6");
        Console.WriteLine($"BYMONTHDAY=31 from 2026-01-01: {Join(explicitDay.GetOccurrences(new DateTime(2026, 1, 1)))}");

        // The 29th of February exists only in leap years, so a yearly rule on it skips three years
        // at a time.
        RecurrenceRule leapDay = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29;COUNT=3");
        Console.WriteLine($"29 February yearly           : {Join(leapDay.GetOccurrences(new DateTime(2024, 1, 1)))}");

        Console.WriteLine();
        Console.WriteLine("--- The occurrence set is a set ---");

        // Two BY values that resolve to the same date contribute ONE occurrence. In a 31-day month
        // the 1st and the -31st are the same day, so January yields a single occurrence.
        RecurrenceRule colliding = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1,-31;COUNT=4");
        Console.WriteLine($"BYMONTHDAY=1,-31             : {Join(colliding.GetOccurrences(new DateTime(2026, 1, 1)))}");

        // The same holds for BYDAY ordinals: in a four-Monday month, 1MO and -4MO coincide.
        RecurrenceRule collidingDays = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=1MO,-4MO;COUNT=4");
        Console.WriteLine($"BYDAY=1MO,-4MO               : {Join(collidingDays.GetOccurrences(new DateTime(2026, 2, 1)))}");

        // Deduplication happens BEFORE BYSETPOS indexes the candidates and before COUNT counts
        // them, so neither is thrown off by a collision.
        Console.WriteLine();
        Console.WriteLine("--- BYSETPOS indexes the whole frequency period ---");

        // BYSETPOS selects positions within each period's candidate list. -1 is the last weekday
        // of each month.
        RecurrenceRule lastWeekday = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1;COUNT=4");
        Console.WriteLine($"last weekday of the month    : {Join(lastWeekday.GetOccurrences(new DateTime(2026, 1, 1)))}");

        // The subtle part: the candidate list is the WHOLE period, including candidates that fall
        // before the series start. Those are dropped only after the positions are assigned, so a
        // rule anchored mid-week selects the same positions as one anchored on the week start.
        RecurrenceRule weekly = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1;COUNT=3");
        var onMonday = new DateTime(2026, 3, 2);     // a Monday -- the start of the week
        var onWednesday = new DateTime(2026, 3, 4);  // mid-week
        Console.WriteLine($"BYSETPOS=1 anchored Monday   : {Join(weekly.GetOccurrences(onMonday))}");
        Console.WriteLine($"BYSETPOS=1 anchored Wednesday: {Join(weekly.GetOccurrences(onWednesday))}");

        // The Wednesday anchor does not promote Wednesday to position 1: the first period's
        // position 1 is still its Monday, which simply falls before the start and is dropped.
        Console.WriteLine();
        Console.WriteLine("--- A BY filter never re-anchors the interval ---");

        // INTERVAL=14 counts every fourteenth day from the start UNCONDITIONALLY. BYMONTH then
        // DROPS the ones outside October and December -- it does not restart the count at the
        // month boundary, so the surviving dates stay on the original fourteen-day grid.
        RecurrenceRule fortnightly = RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=14;BYMONTH=10,12;COUNT=6");
        Console.WriteLine($"every 14th day, Oct+Dec only : {Join(fortnightly.GetOccurrences(new DateTime(2026, 9, 27)))}");

        // Reading the gaps: consecutive survivors are 14 days apart, and the jump across November
        // is a multiple of 14, which is the proof that the grid was never restarted.
        DateTime[] dates = [.. fortnightly.GetOccurrences(new DateTime(2026, 9, 27))];
        Console.WriteLine($"gaps in days                 : {string.Join(", ", dates.Zip(dates.Skip(1), (a, b) => (b - a).Days))}");

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
