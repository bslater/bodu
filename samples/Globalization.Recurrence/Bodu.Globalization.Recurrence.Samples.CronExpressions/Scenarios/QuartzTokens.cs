// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuartzTokens.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.CronExpressions.Scenarios;

/// <summary>
/// Demonstrates the Quartz day tokens the two day fields accept - <c>L</c>, <c>L-n</c>, <c>nW</c>, <c>LW</c>,
/// <c>dL</c>, <c>d#k</c>, and <c>?</c> - and the two Vixie rules they keep: weekday numbering from zero, and the
/// union of two restricted day fields.
/// </summary>
public static class QuartzTokens
{
    /// <summary>
    /// Walks a schedule for each token, then shows canonical text and the Vixie rules the tokens keep.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Quartz day tokens - month ends, nearest weekdays, and n-th weekdays",
            what: "Walks schedules using each day token Quartz added to cron: the last day of the month and a day "
                + "counted back from it, the weekday nearest a day, the month's last and n-th weekday, and '?' for "
                + "a day field that restricts nothing. It then prints each token's canonical text and shows the two "
                + "Vixie rules the tokens keep.",
            why: "Month ends and n-th weekdays are among the most common business schedules - closing the books on "
                + "the last day, paying on the last weekday, a meeting on the first Monday - and plain cron cannot "
                + "say them: the last day is the 28th, 29th, 30th or 31st depending on the month, and 'the first "
                + "Monday' written as '1-7 * MON' is the union of the first seven days and every Monday. The tokens "
                + "say them directly. Carrying them over from Quartz still needs care, because this parser keeps "
                + "Vixie's weekday numbers and union rule.",
            expect: "Each walk lands on the day its token names in every month, the short ones included: 'L' finds "
                + "28 February, 'LW' moves a month end that falls on a weekend back to the Friday, '1W' moves a "
                + "Saturday the 1st forward to Monday the 3rd rather than back into the month before, and 'MON#5' "
                + "passes over the months with only four Mondays. Weekday numbers stay Vixie's, so '5#1' and "
                + "'FRI#1' agree while Quartz's own spelling of the first Friday, '6#1', reads here as the first "
                + "Saturday.");
        Console.WriteLine();

        // Every token stands for a whole day field; the walks start from the same instant so the months
        // line up across the rows.
        var from = new DateTime(2026, 1, 1);
        (string Expression, string Meaning)[] tokens =
        [
            ("0 23 L * *", "the last day of the month"),
            ("0 9 L-2 * *", "two days before the last"),
            ("0 18 LW * *", "the last weekday of the month"),
            ("0 9 15W * *", "the weekday nearest the 15th"),
            ("0 9 1W * *", "the weekday nearest the 1st"),
            ("0 17 * * FRIL", "the last Friday"),
            ("0 9 * * MON#1", "the first Monday"),
            ("0 9 * * MON#5", "the fifth Monday, where there is one"),
        ];

        Console.WriteLine($"walks from {from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:");
        foreach ((string expression, string meaning) in tokens)
        {
            CronExpression cron = CronExpression.Parse(expression);
            Console.WriteLine($"  {expression,-15} {meaning,-37} {Walk(cron, from, 4)}");
        }

        // The nearest weekday never leaves the month: 1 August 2026 is a Saturday, and the Friday before it
        // is in July, so '1W' moves forward to Monday the 3rd instead.
        CronExpression firstWeekday = CronExpression.Parse("0 9 1W * *");
        Console.WriteLine($"  {"0 9 1W * *",-15} {"from 2026-07-15, past a Saturday 1st",-37} {Walk(firstWeekday, new DateTime(2026, 7, 15), 2)}");

        Console.WriteLine();
        Console.WriteLine("--- '?' and canonical text ---");

        // '?' restricts nothing, exactly as '*' does; it exists so Quartz expressions parse unchanged.
        CronExpression question = CronExpression.Parse("0 0 ? * MON#1");
        Console.WriteLine($"  '0 0 ? * MON#1' == '0 0 * * MON#1' : {question.Equals(CronExpression.Parse("0 0 * * MON#1"))}");

        // A token is written in upper case with a numeric weekday and no zero offset.
        string[] spellings = ["0 17 * * FRIL", "0 9 l-0w * *", "0 0 ? * sun#2", "0 0 * * 7L"];
        foreach (string spelling in spellings)
        {
            Console.WriteLine($"  {spelling,-15} -> {CronExpression.Parse(spelling)}");
        }

        Console.WriteLine();
        Console.WriteLine("--- The Vixie rules the tokens keep ---");

        // Weekday numbers are Vixie's, Sunday 0 (or 7) through Saturday 6. Quartz numbers Sunday 1 through
        // Saturday 7, so its '6#1' (the first Friday) reads here as the first Saturday. A name means the same
        // in both.
        CronExpression byNumber = CronExpression.Parse("0 9 * * 5#1");
        CronExpression byName = CronExpression.Parse("0 9 * * FRI#1");
        CronExpression quartzNumber = CronExpression.Parse("0 9 * * 6#1");
        Console.WriteLine($"  '0 9 * * 5#1' == '0 9 * * FRI#1' : {byNumber.Equals(byName)}");
        Console.WriteLine($"  0 9 * * FRI#1 -> {Walk(byName, from, 3)}");
        Console.WriteLine($"  0 9 * * 6#1   -> {Walk(quartzNumber, from, 3)}  (Quartz's first Friday, read as Vixie's first Saturday)");

        // A token is a restriction, so a token beside values in the other day field takes the union.
        CronExpression union = CronExpression.Parse("0 0 L * MON");
        Console.WriteLine($"  0 0 L * MON   -> {Walk(union, from, 5)}  (the last day OR any Monday)");

        Console.WriteLine();
    }

    /// <summary>
    /// Walks a schedule forward and formats the first several occurrences after an instant with their weekdays.
    /// </summary>
    /// <param name="cron">The schedule to walk.</param>
    /// <param name="from">The instant to start from.</param>
    /// <param name="count">The number of occurrences to take.</param>
    /// <returns>The formatted, comma-separated occurrence dates.</returns>
    private static string Walk(CronExpression cron, DateTime from, int count)
    {
        var occurrences = new List<string>();
        DateTime cursor = from;

        while (occurrences.Count < count && cron.GetNextOccurrence(cursor) is DateTime next)
        {
            occurrences.Add(next.ToString("MM-dd ddd", CultureInfo.InvariantCulture));
            cursor = next;
        }

        return string.Join(", ", occurrences);
    }
}
