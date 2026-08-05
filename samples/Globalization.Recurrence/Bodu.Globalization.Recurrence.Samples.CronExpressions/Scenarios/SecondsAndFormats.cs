// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecondsAndFormats.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.CronExpressions.Scenarios;

/// <summary>
/// Demonstrates the six-field seconds layout selected by <see cref="CronFormat" />, and the
/// canonical text that <c>ToString</c> emits — a normalized form that makes equality decidable by
/// meaning rather than by source spelling.
/// </summary>
public static class SecondsAndFormats
{
    /// <summary>
    /// Parses six-field expressions and shows canonical rendering, round-tripping, and equality.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CronFormat.WithSeconds: the six-field layout ---");
        Console.WriteLine("fields: second minute hour day-of-month month day-of-week");
        Console.WriteLine();

        var now = new DateTime(2026, 3, 10, 14, 32, 17);
        Console.WriteLine($"query instant : {Format(now)}");
        Console.WriteLine();

        // The seconds field is opt-in: the same text means different things under the two formats,
        // so the format is a parse-time decision rather than something inferred from field count.
        (string Expression, string Meaning)[] catalogue =
        [
            ("*/10 * * * * *", "every ten seconds"),
            ("0 * * * * *", "at the top of every minute"),
            ("30 * * * * *", "at 30 seconds past every minute"),
            ("0 0 * * * *", "on the hour"),
            ("15 30 9 * * *", "09:30:15 daily"),
            ("0 0 12 * * MON", "Mondays at noon"),
        ];

        foreach ((string expression, string meaning) in catalogue)
        {
            CronExpression cron = CronExpression.Parse(expression, CronFormat.WithSeconds);
            Console.WriteLine($"{expression,-16} {meaning,-31} next: {Format(cron.GetNextOccurrence(now))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Inferring the format, or stating it ---");

        // The overloads WITHOUT a CronFormat infer the layout from the field count, which is the
        // convenient default for reading a value a human typed.
        foreach (string expression in new[] { "0 12 * * MON", "0 0 12 * * MON" })
        {
            CronExpression inferred = CronExpression.Parse(expression);
            Console.WriteLine($"  {$"'{expression}'",-18} inferred    -> {inferred.Format}");
        }

        Console.WriteLine();

        // The overloads WITH a CronFormat enforce it instead, so a configuration value cannot
        // silently change meaning by gaining or losing a field. Use these where the layout is part
        // of the contract rather than the caller's guess.
        (string Expression, CronFormat Format)[] stated =
        [
            ("0 12 * * MON", CronFormat.Standard),
            ("0 12 * * MON", CronFormat.WithSeconds),
            ("0 0 12 * * MON", CronFormat.Standard),
            ("0 0 12 * * MON", CronFormat.WithSeconds),
        ];

        foreach ((string expression, CronFormat format) in stated)
        {
            bool parsed = CronExpression.TryParse(expression, format, out CronExpression? cron, out string? defect);
            string outcome = parsed ? $"next {Format(cron!.GetNextOccurrence(now))}" : defect!;
            Console.WriteLine($"  {$"'{expression}'",-18} as {format,-11} : {outcome}");
        }

        // Either way the parsed expression remembers its layout, so a round trip cannot change it.
        CronExpression withSeconds = CronExpression.Parse("0 0 12 * * MON", CronFormat.WithSeconds);
        Console.WriteLine($"  {"Format property",-18}             : {withSeconds.Format} -> '{withSeconds}'");

        Console.WriteLine();
        Console.WriteLine("--- Canonical text ---");

        // ToString renders each field as "*" when it spans its whole range, and otherwise as the
        // sorted list of values it selects. Ranges, steps and names are all expanded, so two
        // spellings of the same schedule render identically.
        (string Input, string Note)[] canonical =
        [
            ("* * * * *", "already canonical"),
            ("0-3 * * * *", "a range expands to a list"),
            ("*/20 * * * *", "a step expands to the values it hits"),
            ("MON * * * *", "not a name field -- minute 'MON' would be invalid"),
            ("0 * * * MON", "a weekday name becomes its number"),
            ("0 0 * JAN,JUL *", "month names become numbers"),
            ("0 0 * * 1-5", "a weekday range expands"),
        ];

        foreach ((string input, string note) in canonical)
        {
            if (CronExpression.TryParse(input, out CronExpression? parsed))
            {
                Console.WriteLine($"{input,-18} -> {parsed,-28} ({note})");
            }
            else
            {
                Console.WriteLine($"{input,-18} -> (rejected){new string(' ', 18)} ({note})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Equality is by schedule, not by spelling ---");

        // Because parsing normalizes to a field mask, expressions that select the same instants
        // compare equal and hash alike -- so a schedule can be used as a dictionary key.
        (string Left, string Right)[] pairs =
        [
            ("* * * * *", "0-59 * * * *"),
            ("* * * * *", "0-59/1 * * * *"),
            ("0 8 * * 1-5", "0 8 * * MON-FRI"),
            ("*/30 * * * *", "0,30 * * * *"),
            ("@hourly", "0 * * * *"),
            ("0 0 * * 0", "0 0 * * 7"),
            ("1 1 1 1 1", "2 1 1 1 1"),
        ];

        foreach ((string left, string right) in pairs)
        {
            CronExpression a = CronExpression.Parse(left);
            CronExpression b = CronExpression.Parse(right);
            Console.WriteLine($"{left,-14} == {right,-16} : {a.Equals(b),-5} (hash match: {a.GetHashCode() == b.GetHashCode()})");
        }

        // A canonical rendering re-parses to an equal expression, so the normalized text is a
        // faithful serialization rather than a lossy display form.
        CronExpression original = CronExpression.Parse("0 8-10 * JAN,JUL MON-WED");
        CronExpression reparsed = CronExpression.Parse(original.ToString());
        Console.WriteLine();
        Console.WriteLine($"'0 8-10 * JAN,JUL MON-WED' -> '{original}'");
        Console.WriteLine($"re-parses equal            : {original.Equals(reparsed)}");

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
