// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatterns.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates <see cref="WeekPattern" />: a compact seven-bit value type that records which days of the week
/// are "selected" (for example, which are working days). It ships regional presets, round-trips through a small
/// family of text formats, supports set-style bitwise operators, and drives working-day queries when combined
/// with a date range.
/// </summary>
public static class WeekPatterns
{
    /// <summary>
    /// Shows the presets, formatting and parsing, the bitwise operators, and a working-day walk over a fixed
    /// two-week window.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- WeekPattern: seven-day selection sets ---");

        // Presets cover the common working weeks around the world.
        var week = WeekPattern.MondayToFriday;
        Console.WriteLine($"MondayToFriday   : Count={week.Count}, S-format='{week}'");

        // ToString accepts a format character: 'M' = Monday-first ordering, 'B' = binary, 'A' = asterisk fill.
        Console.WriteLine($"  ToString(\"M\")   : {week.ToString("M")}");
        Console.WriteLine($"  ToString(\"B\")   : {week.ToString("B")}");
        Console.WriteLine($"  ToString(\"A\")   : {week.ToString("A")}");

        // Parse infers the format from the input, so a formatted pattern round-trips back to an equal value.
        var parsed = WeekPattern.Parse(week.ToString());
        Console.WriteLine($"  Parse round-trip: {parsed == week}");

        // The bitwise operators treat a pattern as a set of days: OR adds days, AND intersects, ~ complements.
        var withSaturday = week | new WeekPattern(DayOfWeek.Saturday);
        var weekend = ~WeekPattern.Weekdays;
        Console.WriteLine($"  | Saturday      : Count={withSaturday.Count}, '{withSaturday}'");
        Console.WriteLine($"  ~Weekdays       : Count={weekend.Count}, '{weekend}' (the weekend)");

        // A WeekPattern answers "is this day selected?" - drive it across a fixed date range to list working days.
        var start = new DateOnly(2024, 1, 1); // a Monday
        Console.WriteLine("  Working days 2024-01-01 .. 2024-01-07:");
        for (var day = start; day < start.AddDays(7); day = day.AddDays(1))
        {
            var isWorking = week.Contains(day.DayOfWeek);
            Console.WriteLine($"    {day:yyyy-MM-dd} {day.DayOfWeek,-9} -> {(isWorking ? "work" : "off")}");
        }

        Console.WriteLine();
    }
}
