// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSets.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates <see cref="DayOfWeekSet" /> and its siblings <see cref="MonthSet" />, <see cref="DayOfMonthSet" />,
/// <see cref="HourSet" />, <see cref="MinuteSet" />, and <see cref="SecondSet" />: immutable sets that record which
/// values of one calendar field are selected (for example, which days are working days, or which hours a job may
/// run in). Each ships a canonical text form that round-trips, supports set-style operators, and answers a
/// membership test with a single bit test.
/// </summary>
public static class DayOfWeekSets
{
    /// <summary>
    /// Shows the presets, formatting and parsing, the set operators, a working-day walk over a fixed week, and a
    /// schedule window built from the numeric sets.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "DayOfWeekSet and the calendar value sets",
            what: "Builds a working week, formats it through four format specifiers, and parses each text back to " +
                  "confirm the round trip; then builds a schedule window from a month set, a day-of-month set and " +
                  "an hour set.",
            why: "A set of weekdays is otherwise a bool[7] or a flags enum, and both lose to the same problem: " +
                 "nobody agrees which day index zero is. DayOfWeekSet fixes the order and makes the text form " +
                 "canonical, so a set written in configuration, stored in a database and parsed back is the " +
                 "same set. The numeric sets do the same for months, days of the month, hours, minutes and " +
                 "seconds, so a schedule window is a few bit tests rather than a list of ranges to search.",
            expect: "One set renders four ways - Sunday-first, Monday-first, binary and asterisk-filled - all " +
                    "describing the same five days, and parsing every one of them returns an equal set. Only the " +
                    "first of the three instants falls inside the quarter-end window.");

        // Presets cover the common working weeks around the world.
        var week = DayOfWeekSet.MondayToFriday;
        Console.WriteLine($"  MondayToFriday   : Count={week.Count}, S-format='{week}'  (five days selected; the S format marks unselected days with an underscore so position is unambiguous)");

        // ToString accepts a format: 'M' = Monday-first ordering, 'B' = binary, 'A' = asterisk fill.
        Console.WriteLine($"  ToString(\"M\")   : {week.ToString("M")}");
        Console.WriteLine($"  ToString(\"B\")   : {week.ToString("B")}");
        Console.WriteLine($"  ToString(\"A\")   : {week.ToString("A")}");

        // Parse reads the order, the placeholder and the binary form from the text itself, so every rendering
        // round-trips back to an equal value.
        var roundTrips = new[] { "S", "M", "B", "A" }.All(format => DayOfWeekSet.Parse(week.ToString(format)) == week);
        Console.WriteLine($"  Parse round-trip: {roundTrips}  (all four renderings parse back to the same set)");

        // The operators treat the value as a set of days: | adds days, & intersects, ~ complements within the week.
        var withSaturday = week | new DayOfWeekSet(DayOfWeek.Saturday);
        var weekend = ~DayOfWeekSet.Weekdays;
        Console.WriteLine($"  | Saturday      : Count={withSaturday.Count}, '{withSaturday}'");
        Console.WriteLine($"  ~Weekdays       : Count={weekend.Count}, '{weekend}' (the weekend)");

        // A DayOfWeekSet answers "is this day selected?" - drive it across a fixed date range to list working days.
        var start = new DateOnly(2024, 1, 1); // a Monday
        Console.WriteLine("  Working days 2024-01-01 .. 2024-01-07:");
        for (var day = start; day < start.AddDays(7); day = day.AddDays(1))
        {
            var isWorking = week.Contains(day.DayOfWeek);
            Console.WriteLine($"    {day:yyyy-MM-dd} {day.DayOfWeek,-9} -> {(isWorking ? "work" : "off")}");
        }

        // The numeric sets read any list of values and ranges and write the canonical one: ascending, with each run
        // of consecutive values written as a range.
        var minutes = MinuteSet.Parse("45, 0, 30, 15, 0-1");
        Console.WriteLine($"  MinuteSet       : '{minutes}'  (the canonical form of \"45, 0, 30, 15, 0-1\")");

        // Together they describe a schedule window: the last days of each quarter, in business hours, on a working day.
        var quarterEnds = MonthSet.Parse("3,6,9,12");
        var lastDays = DayOfMonthSet.Parse("28-31");
        var businessHours = HourSet.Parse("9-17");
        Console.WriteLine($"  Window          : months '{quarterEnds}', days '{lastDays}', hours '{businessHours}', {week.Count} working days");

        DateTime[] instants = [new(2024, 3, 29, 10, 0, 0), new(2024, 3, 29, 20, 0, 0), new(2024, 4, 30, 10, 0, 0)];
        foreach (var instant in instants)
        {
            var inWindow = quarterEnds.Contains(instant.Month)
                && lastDays.Contains(instant.Day)
                && businessHours.Contains(instant.Hour)
                && week.Contains(instant.DayOfWeek);
            Console.WriteLine($"    {instant:yyyy-MM-dd HH:mm} {instant.DayOfWeek,-9} -> {(inWindow ? "in window" : "outside")}");
        }

        Console.WriteLine();
    }
}
