// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.CronExpressions.Scenarios;

/// <summary>
/// Demonstrates the <see cref="CronExpression" /> basics: the five-field Vixie layout, the field
/// grammar (lists, ranges, steps, names), the <c>@</c> macros, and the next/previous point queries
/// with their inclusive flags.
/// </summary>
public static class CronBasics
{
    /// <summary>
    /// Parses a range of expressions and queries the schedule around a fixed instant.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CronExpression: the five-field Vixie layout ---");
        Console.WriteLine("fields: minute hour day-of-month month day-of-week");
        Console.WriteLine();

        // A cron expression carries no start instant of its own -- unlike a recurrence rule it has
        // no series origin at all, so the query instant is the only input GetNextOccurrence needs.
        var now = new DateTime(2026, 3, 10, 14, 32, 0);   // a Tuesday
        Console.WriteLine($"query instant : {Format(now)} ({now.DayOfWeek})");
        Console.WriteLine();

        // The field grammar: a literal, a list, a range, a step over a range, a stepped star, and
        // the three-letter names for months and weekdays.
        (string Expression, string Meaning)[] catalogue =
        [
            ("* * * * *", "every minute"),
            ("0 * * * *", "on the hour"),
            ("30 9 * * *", "09:30 daily"),
            ("0 9,17 * * *", "09:00 and 17:00"),
            ("0 9-11 * * *", "09:00, 10:00 and 11:00"),
            ("*/15 * * * *", "every quarter hour"),
            ("0 0 1 * *", "midnight on the 1st"),
            ("0 8 * * MON", "Mondays at 08:00"),
            ("0 8 * * 1-5", "weekdays at 08:00"),
            ("0 0 1 JAN *", "New Year's Day"),
        ];

        foreach ((string expression, string meaning) in catalogue)
        {
            CronExpression cron = CronExpression.Parse(expression);
            Console.WriteLine($"{expression,-14} {meaning,-24} next: {Format(cron.GetNextOccurrence(now))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- The @ macros ---");

        // crontab(5) publishes a small set of named shorthands. Each parses to an ordinary
        // expression, so a macro and its long form compare equal.
        (string Macro, string Equivalent)[] macros =
        [
            ("@hourly", "0 * * * *"),
            ("@daily", "0 0 * * *"),
            ("@midnight", "0 0 * * *"),
            ("@weekly", "0 0 * * 0"),
            ("@monthly", "0 0 1 * *"),
            ("@yearly", "0 0 1 1 *"),
            ("@annually", "0 0 1 1 *"),
        ];

        foreach ((string macro, string equivalent) in macros)
        {
            CronExpression parsed = CronExpression.Parse(macro);
            Console.WriteLine($"{macro,-11} == {equivalent,-11} : {parsed.Equals(CronExpression.Parse(equivalent)),-5} next: {Format(parsed.GetNextOccurrence(now))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Next and previous, with the inclusive flag ---");

        // Both directions are available on every schedule form in this package, with the same
        // inclusive flag deciding whether an exact hit on the query instant counts.
        CronExpression onTheHour = CronExpression.Parse("0 * * * *");
        var exactHit = new DateTime(2026, 3, 10, 14, 0, 0);

        Console.WriteLine($"query          : {Format(exactHit)} (an exact match for '0 * * * *')");
        Console.WriteLine($"next  exclusive: {Format(onTheHour.GetNextOccurrence(exactHit))}");
        Console.WriteLine($"next  inclusive: {Format(onTheHour.GetNextOccurrence(exactHit, inclusive: true))}");
        Console.WriteLine($"prev  exclusive: {Format(onTheHour.GetPreviousOccurrence(exactHit))}");
        Console.WriteLine($"prev  inclusive: {Format(onTheHour.GetPreviousOccurrence(exactHit, inclusive: true))}");

        // Walking a schedule forwards is just repeated next-from-the-last-answer.
        Console.WriteLine();
        Console.WriteLine("--- Walking a schedule ---");

        CronExpression standup = CronExpression.Parse("15 9 * * 1-5");
        Console.WriteLine($"'15 9 * * 1-5' (weekdays 09:15) from {Format(now)}:");
        var cursor = now;
        for (int i = 0; i < 6; i++)
        {
            cursor = standup.GetNextOccurrence(cursor)!.Value;
            Console.WriteLine($"  {Format(cursor)} {cursor.DayOfWeek}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an optional instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "(never)";
}
