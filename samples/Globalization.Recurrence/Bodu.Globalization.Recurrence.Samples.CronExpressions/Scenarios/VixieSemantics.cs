// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VixieSemantics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.CronExpressions.Scenarios;

/// <summary>
/// Demonstrates the two Vixie behaviours that separate this dialect from the Quartz-flavoured cron
/// many .NET libraries implement: the day-of-month / day-of-week union rule decided by a field's
/// leading character, and a step wider than its range collapsing to the range start.
/// </summary>
public static class VixieSemantics
{
    /// <summary>
    /// Contrasts the union and intersection branches, then shows oversized-step handling.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- The day-of-month / day-of-week union rule ---");
        Console.WriteLine();

        // When BOTH day fields are restricted, Vixie takes their UNION -- an instant matches if
        // EITHER field matches. Most calendar intuitions expect an intersection, so this is the
        // rule most often mistaken for a bug.
        var from = new DateTime(2026, 1, 1, 0, 0, 0);
        CronExpression union = CronExpression.Parse("0 0 1 * MON");
        Console.WriteLine("'0 0 1 * MON' -- both day fields restricted, so UNION:");
        Console.WriteLine($"  {Walk(union, from, 5)}");
        Console.WriteLine("  (the 1st of each month OR any Monday -- an AND engine would give 2026-06-01)");

        Console.WriteLine();

        // "Restricted" is decided by the field's LEADING CHARACTER, not by the set of days it
        // denotes. "*/2" leads with '*', so it is unrestricted and the intersection branch runs;
        // "1-31/2" denotes the same days but leads with a digit, so the union branch runs.
        var may = new DateTime(2023, 5, 2, 0, 0, 0);
        CronExpression steppedStar = CronExpression.Parse("0 16 */2 * sat");
        CronExpression explicitRange = CronExpression.Parse("0 16 1-31/2 * sat");

        Console.WriteLine("'*/2' and '1-31/2' denote the same days, but select different branches:");
        Console.WriteLine($"  0 16 */2    * sat -> {Walk(steppedStar, may, 4)}");
        Console.WriteLine("                       (leading '*' -> unrestricted -> INTERSECTION: odd-numbered Saturdays)");
        Console.WriteLine($"  0 16 1-31/2 * sat -> {Walk(explicitRange, may, 4)}");
        Console.WriteLine("                       (leading digit -> restricted -> UNION: odd days and Saturdays)");

        Console.WriteLine();
        Console.WriteLine("  This is not a rationalizable rule -- it is what cronie's src/entry.c does:");
        Console.WriteLine("      if (ch == '*') e->flags |= DOM_STAR;");
        Console.WriteLine("  croniter carries the same pair as test_dom_dow_vixie_cron_bug.");

        Console.WriteLine();
        Console.WriteLine("--- Only one restricted day field: no union to take ---");

        // With one day field unrestricted there is no ambiguity: the restricted field simply
        // filters, and the star contributes nothing.
        CronExpression domOnly = CronExpression.Parse("0 0 13 * *");
        CronExpression dowOnly = CronExpression.Parse("0 0 * * FRI");
        Console.WriteLine($"  0 0 13 * *   (13th of the month) -> {Walk(domOnly, from, 4)}");
        Console.WriteLine($"  0 0 *  * FRI (every Friday)      -> {Walk(dowOnly, from, 4)}");

        // The classic "Friday the 13th" schedule therefore CANNOT be written in Vixie cron: the
        // union rule turns the obvious spelling into "the 13th or any Friday".
        CronExpression friday13 = CronExpression.Parse("0 0 13 * FRI");
        Console.WriteLine($"  0 0 13 * FRI (NOT Friday the 13th) -> {Walk(friday13, from, 4)}");

        Console.WriteLine();
        Console.WriteLine("--- A step wider than its range selects the range start ---");

        // cronie only WARNS about an oversized step ("Step size %i higher than possible maximum")
        // and then runs `for (i = low; i <= high; i += step)`, which sets exactly one bit. Several
        // libraries reject the same input instead; this dialect follows cronie.
        (string Expression, string Equivalent)[] oversized =
        [
            ("*/60 * * * *", "0 * * * *"),
            ("1/60 * * * *", "1 * * * *"),
            ("* 1/24 * * *", "* 1 * * *"),
            ("* * 1/32 * *", "* * 1 * *"),
            ("* * * 1/13 *", "* * * 1 *"),
            ("* * * * 1/8", "* * * * 1"),
        ];

        foreach ((string expression, string equivalent) in oversized)
        {
            CronExpression cron = CronExpression.Parse(expression);
            Console.WriteLine($"  {expression,-14} -> {cron,-14} == '{equivalent}' : {cron.Equals(CronExpression.Parse(equivalent))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Sunday is both 0 and 7 ---");

        // Vixie accepts 7 as an alias for Sunday, so the two spellings are the same schedule.
        CronExpression sundayZero = CronExpression.Parse("0 0 * * 0");
        CronExpression sundaySeven = CronExpression.Parse("0 0 * * 7");
        CronExpression sundayName = CronExpression.Parse("0 0 * * SUN");
        Console.WriteLine($"  '0 0 * * 0' == '0 0 * * 7'   : {sundayZero.Equals(sundaySeven)}");
        Console.WriteLine($"  '0 0 * * 0' == '0 0 * * SUN' : {sundayZero.Equals(sundayName)}");
        Console.WriteLine($"  canonical                    : {sundaySeven}");

        Console.WriteLine();
    }

    /// <summary>
    /// Walks a schedule forward and formats the first several occurrences after an instant.
    /// </summary>
    /// <param name="cron">The schedule to walk.</param>
    /// <param name="from">The instant to start from.</param>
    /// <param name="count">The number of occurrences to take.</param>
    /// <returns>The formatted, comma-separated occurrence dates.</returns>
    private static string Walk(CronExpression cron, DateTime from, int count)
    {
        var occurrences = new List<string>();
        var cursor = from;

        while (occurrences.Count < count && cron.GetNextOccurrence(cursor) is DateTime next)
        {
            occurrences.Add(next.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cursor = next;
        }

        return string.Join(", ", occurrences);
    }
}
