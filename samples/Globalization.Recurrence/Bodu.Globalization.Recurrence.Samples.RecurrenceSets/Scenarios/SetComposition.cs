// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SetComposition.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceSets.Scenarios;

/// <summary>
/// Demonstrates how a <see cref="RecurrenceSet" /> composes its parts: the union of every rule and
/// every explicit date, minus every exception date, emitted once each in ascending order.
/// </summary>
public static class SetComposition
{
    /// <summary>
    /// Builds sets of increasing complexity and shows how each part changes the stream.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Composing a set from rules, dates, and exceptions",
            what: "Builds a set from a single rule, adds explicit RDATE occurrences, removes EXDATE ones, and "
                + "combines several rules into one series.",
            why: "A real schedule is rarely one rule. A term timetable is a weekly pattern minus the public "
                + "holidays; a release cadence is a monthly rule plus the two dates someone moved. Expressing "
                + "that as rule-plus-exceptions is what RFC 5545 does, and following it means a set round-trips "
                + "through any calendar system rather than needing a custom representation. The precedence rule "
                + "is the part that has to be unambiguous: an exception date wins over a rule that generates "
                + "it, because the exception is the more specific statement and the alternative - order "
                + "dependence - would make the same set mean different things depending on how it was "
                + "assembled.",
            expect: "The composed series is the union of the rules and explicit dates with the exception dates "
                + "removed, regardless of the order the parts were added. An exception removes an occurrence a "
                + "rule generated, which is the precedence that makes rule-plus-exception composition usable.");

        var start = new DateTime(2026, 1, 5, 9, 0, 0);   // a Monday
        RecurrenceRule weekly = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;COUNT=6");

        // Unlike a bare rule, a set OWNS its start instant -- so GetOccurrences takes no arguments.
        var simple = new RecurrenceSet(start, [weekly]);
        Console.WriteLine($"start        : {Format(start)}");
        Console.WriteLine($"rule         : {weekly}");
        Console.WriteLine($"occurrences  : {Join(simple.GetOccurrences())}");

        Console.WriteLine();
        Console.WriteLine("--- Adding explicit dates (RDATE) ---");

        // RDATE entries are occurrences the rules would not otherwise produce. They are merged into
        // the stream in order, not appended to the end.
        DateTime[] extras =
        [
            new DateTime(2026, 1, 8, 9, 0, 0),    // a Thursday -- no rule produces it
            new DateTime(2026, 1, 21, 9, 0, 0),   // a Wednesday
        ];

        var withDates = new RecurrenceSet(start, [weekly], extras);
        Console.WriteLine($"RDATEs       : {Join(extras)}");
        Console.WriteLine($"occurrences  : {Join(withDates.GetOccurrences())}");

        Console.WriteLine();
        Console.WriteLine("--- Removing dates (EXDATE) ---");

        // EXDATE removes an instant regardless of which part produced it, and it wins over both
        // rules and RDATEs.
        DateTime[] exceptions =
        [
            new DateTime(2026, 1, 19, 9, 0, 0),   // produced by the rule
            new DateTime(2026, 1, 21, 9, 0, 0),   // an RDATE -- exceptions beat additions
        ];

        var withExceptions = new RecurrenceSet(start, [weekly], extras, exceptions);
        Console.WriteLine($"EXDATEs      : {Join(exceptions)}");
        Console.WriteLine($"occurrences  : {Join(withExceptions.GetOccurrences())}");
        Console.WriteLine("               (the 19th came from the rule, the 21st from an RDATE -- both removed)");

        Console.WriteLine();
        Console.WriteLine("--- Several rules at once ---");

        // Multiple rules union together. Where two rules produce the same instant it appears once,
        // because the result is a set rather than a concatenation.
        RecurrenceRule mondays = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;COUNT=4");
        RecurrenceRule firstOfMonth = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1;COUNT=3");

        var combined = new RecurrenceSet(start, [mondays, firstOfMonth]);
        Console.WriteLine($"rule 1       : {mondays}");
        Console.WriteLine($"rule 2       : {firstOfMonth}");
        Console.WriteLine($"occurrences  : {Join(combined.GetOccurrences())}");

        // A worked overlap: 2026-06-01 is both a Monday and the first of the month, so a set built
        // from those two rules emits it once.
        var june = new DateTime(2026, 6, 1, 9, 0, 0);
        var overlapping = new RecurrenceSet(
            june,
            [RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;COUNT=3"), RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1;COUNT=3")]);

        DateTime[] materialized = [.. overlapping.GetOccurrences()];
        Console.WriteLine();
        Console.WriteLine($"overlapping  : {Join(materialized)}");
        Console.WriteLine($"distinct     : {materialized.Length == materialized.Distinct().Count()} ({materialized.Length} occurrences, {materialized.Distinct().Count()} distinct)");

        Console.WriteLine();
        Console.WriteLine("--- The parts are readable back ---");

        // Every component is exposed, so a set can be inspected, logged, or re-serialized.
        Console.WriteLine($"Start          : {Format(withExceptions.Start)}");
        Console.WriteLine($"Rules          : {withExceptions.Rules.Count} ({string.Join(" | ", withExceptions.Rules)})");
        Console.WriteLine($"Dates          : {withExceptions.Dates.Count} ({Join(withExceptions.Dates)})");
        Console.WriteLine($"ExceptionDates : {withExceptions.ExceptionDates.Count} ({Join(withExceptions.ExceptionDates)})");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an instant with the invariant culture.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a sequence of occurrences as a comma-separated invariant-culture date list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTime> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
}
