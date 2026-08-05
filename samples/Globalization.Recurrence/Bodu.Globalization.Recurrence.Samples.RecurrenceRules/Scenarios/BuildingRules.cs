// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BuildingRules.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceRules.Scenarios;

/// <summary>
/// Demonstrates <see cref="RecurrenceRuleBuilder" />, the fluent alternative to writing
/// <c>RRULE</c> text by hand, and the <see cref="WeekDayNum" /> value that carries an optional
/// ordinal alongside a weekday.
/// </summary>
public static class BuildingRules
{
    /// <summary>
    /// Builds several rules fluently and shows that they equal their hand-written text form.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- RecurrenceRuleBuilder: fluent construction ---");

        // The builder takes the frequency up front -- the one part RFC 5545 makes mandatory --
        // then layers optional parts. Build() produces an immutable RecurrenceRule.
        RecurrenceRule everyOtherWeek = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
            .WithInterval(2)
            .ByDay(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
            .WithCount(6)
            .Build();

        Console.WriteLine($"built      : {everyOtherWeek}");
        Console.WriteLine($"occurrences: {Join(everyOtherWeek.GetOccurrences(new DateTime(2026, 1, 5)))}");

        // The builder is just a spelling of the same grammar, so the built rule equals the parsed
        // one. That equality is what makes the builder safe to use in configuration code paths.
        RecurrenceRule parsed = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;COUNT=6;BYDAY=MO,WE,FR");
        Console.WriteLine($"equals parsed text : {everyOtherWeek.Equals(parsed)}");

        Console.WriteLine();
        Console.WriteLine("--- WeekDayNum: ordinals inside BYDAY ---");

        // A DayOfWeek overload covers the common "every Monday" case. For "the third Thursday"
        // you need the ordinal form, which WeekDayNum carries.
        RecurrenceRule thirdThursday = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .ByDay(new WeekDayNum(3, DayOfWeek.Thursday))
            .WithCount(4)
            .Build();

        Console.WriteLine($"third Thursday    : {thirdThursday}");
        Console.WriteLine($"occurrences       : {Join(thirdThursday.GetOccurrences(new DateTime(2026, 1, 1)))}");

        // A negative ordinal counts back from the end of the period: -1FR is the last Friday.
        RecurrenceRule lastFriday = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .ByDay(new WeekDayNum(-1, DayOfWeek.Friday))
            .WithCount(4)
            .Build();

        Console.WriteLine($"last Friday       : {lastFriday}");
        Console.WriteLine($"occurrences       : {Join(lastFriday.GetOccurrences(new DateTime(2026, 1, 1)))}");

        // An ordinal of 0 means "every occurrence of this weekday", which is how a bare BYDAY
        // entry parses. WeekDayNum reports that through IsEveryOccurrence and deconstructs.
        var everyTuesday = new WeekDayNum(0, DayOfWeek.Tuesday);
        var (ordinal, day) = everyTuesday;
        Console.WriteLine($"WeekDayNum(0, Tue) : Ordinal={ordinal}, Day={day}, IsEveryOccurrence={everyTuesday.IsEveryOccurrence}");

        // The canonical iCalendar token for each entry comes from the rule that carries it, so
        // round-tripping a built rule through ToString is how you see "TU" / "3TH" / "-1FR".
        foreach (WeekDayNum entry in new[] { everyTuesday, new WeekDayNum(3, DayOfWeek.Thursday), new WeekDayNum(-1, DayOfWeek.Friday) })
        {
            RecurrenceRule single = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly).ByDay(entry).Build();
            Console.WriteLine($"  ordinal {entry.Ordinal,2} {entry.Day,-9} -> {single}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Builder: a bounded yearly rule ---");

        // A more elaborate rule: the US Thanksgiving pattern -- the fourth Thursday of November --
        // bounded by an explicit UNTIL rather than a count.
        RecurrenceRule thanksgiving = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByMonth(11)
            .ByDay(new WeekDayNum(4, DayOfWeek.Thursday))
            .WithUntil(new DateTime(2030, 12, 31))
            .Build();

        Console.WriteLine($"rule       : {thanksgiving}");
        Console.WriteLine($"occurrences: {Join(thanksgiving.GetOccurrences(new DateTime(2026, 1, 1)))}");

        // BySetPos through the builder: the last weekday of each quarter.
        RecurrenceRule quarterEnd = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .WithInterval(3)
            .ByDay(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday)
            .BySetPos(-1)
            .WithCount(4)
            .Build();

        Console.WriteLine($"quarter end: {quarterEnd}");
        Console.WriteLine($"occurrences: {Join(quarterEnd.GetOccurrences(new DateTime(2026, 3, 1)))}");

        Console.WriteLine();
        Console.WriteLine("--- WithWeekStart: the builder reaches WKST too ---");

        // WKST is the part most easily missed, because Monday is the default and a default is omitted
        // from canonical text -- so the two rules below print almost identically while selecting
        // different dates. Only the second carries a visible WKST.
        var wkstStart = new DateTime(1997, 8, 5);   // a Tuesday

        foreach (DayOfWeek weekStart in new[] { DayOfWeek.Monday, DayOfWeek.Sunday })
        {
            RecurrenceRule fortnightly = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
                .WithInterval(2)
                .ByDay(DayOfWeek.Tuesday, DayOfWeek.Sunday)
                .WithWeekStart(weekStart)
                .WithCount(4)
                .Build();

            Console.WriteLine($"  WKST={weekStart,-9} {fortnightly}");
            Console.WriteLine($"                  {Join(fortnightly.GetOccurrences(wkstStart))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- The remaining BY parts ---");

        // ByMonthDay, ByYearDay and ByWeekNo complete the date-selecting set.
        RecurrenceRule paydays = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .ByMonthDay(15, -1)          // the 15th and the last day of the month
            .WithCount(4)
            .Build();
        Console.WriteLine($"  ByMonthDay(15, -1) : {paydays}");
        Console.WriteLine($"                       {Join(paydays.GetOccurrences(new DateTime(2026, 1, 1)))}");

        RecurrenceRule hundredth = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByYearDay(100, -100)        // the 100th day, and the 100th from the end
            .WithCount(4)
            .Build();
        Console.WriteLine($"  ByYearDay(100, -100): {hundredth}");
        Console.WriteLine($"                       {Join(hundredth.GetOccurrences(new DateTime(2026, 1, 1)))}");

        RecurrenceRule weekTwenty = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByWeekNo(20)
            .ByDay(DayOfWeek.Monday)
            .WithCount(3)
            .Build();
        Console.WriteLine($"  ByWeekNo(20)+Monday : {weekTwenty}");
        Console.WriteLine($"                       {Join(weekTwenty.GetOccurrences(new DateTime(2026, 1, 1)))}");

        Console.WriteLine();
        Console.WriteLine("--- Time-of-day parts build, but do not enumerate ---");

        // ByHour, ByMinute and BySecond are part of the RFC 5545 grammar, so the builder accepts them
        // and the rule round-trips through text. Intra-day expansion is not modelled, though: this
        // library enumerates dates, so a rule whose frequency is sub-daily -- or which relies on
        // BYHOUR to place several occurrences inside one day -- is out of scope for enumeration.
        RecurrenceRule intraDay = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily)
            .ByHour(9, 17)
            .ByMinute(30)
            .BySecond(0)
            .WithCount(4)
            .Build();

        Console.WriteLine($"  built   : {intraDay}");
        Console.WriteLine($"  parts   : ByHour=[{string.Join(", ", intraDay.ByHour)}], ByMinute=[{string.Join(", ", intraDay.ByMinute)}], BySecond=[{string.Join(", ", intraDay.BySecond)}]");
        Console.WriteLine($"  re-parses equal : {intraDay.Equals(RecurrenceRule.Parse(intraDay.ToString()))}");

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
