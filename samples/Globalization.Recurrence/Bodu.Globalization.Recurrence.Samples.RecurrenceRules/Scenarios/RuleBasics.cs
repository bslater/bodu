// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceRules.Scenarios;

/// <summary>
/// Demonstrates the <see cref="RecurrenceRule" /> basics: parsing an RFC 5545 <c>RRULE</c>, reading
/// the parsed parts back as typed properties, enumerating occurrences from a start instant, and
/// formatting the rule back to canonical text.
/// </summary>
public static class RuleBasics
{
    /// <summary>
    /// Parses a rule, inspects its parts, enumerates occurrences, and round-trips it to text.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- RecurrenceRule: parse, inspect, enumerate ---");

        // A rule is pure syntax: it carries no start instant. The series start is supplied per
        // query, which is what keeps the type free of hidden state.
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=1FR;COUNT=5");
        Console.WriteLine($"rule       : {rule}");

        // Every part of the grammar is exposed as a typed property.
        Console.WriteLine($"Frequency  : {rule.Frequency}");
        Console.WriteLine($"Interval   : {rule.Interval}");   // defaults to 1 when INTERVAL is absent
        Console.WriteLine($"Count      : {rule.Count}");
        Console.WriteLine($"Until      : {(rule.Until is null ? "(none)" : rule.Until.ToString())}");
        Console.WriteLine($"WeekStart  : {rule.WeekStart}");  // defaults to Monday per RFC 5545

        // BYDAY entries are WeekDayNum values: an optional ordinal plus a weekday. "1FR" is the
        // first Friday; an entry with no ordinal reports IsEveryOccurrence. The canonical "1FR"
        // token is emitted by the rule's own ToString, shown above -- read the parts individually
        // here rather than calling WeekDayNum.ToString().
        WeekDayNum byDay = rule.ByDay[0];
        Console.WriteLine($"ByDay[0]   : Ordinal={byDay.Ordinal}, Day={byDay.Day}, IsEveryOccurrence={byDay.IsEveryOccurrence}");

        // The start instant is passed to the enumeration, not stored on the rule. COUNT=5 bounds
        // the stream, so this terminates without a Take.
        var start = new DateTime(2026, 1, 1, 9, 30, 0);
        Console.WriteLine($"start      : {Format(start)}");
        Console.WriteLine($"occurrences: {string.Join(", ", rule.GetOccurrences(start).Select(FormatDate))}");

        // The time of day rides along from the start instant; a date-valued rule never invents one.
        Console.WriteLine($"first      : {Format(rule.GetOccurrences(start).First())}");

        Console.WriteLine();
        Console.WriteLine("--- RecurrenceRule: canonical text round trip ---");

        // ToString emits canonical text: parts in RFC order, defaults omitted. Re-parsing it
        // yields an equal rule, so the text is a faithful serialization rather than a display form.
        RecurrenceRule verbose = RecurrenceRule.Parse("INTERVAL=1;FREQ=WEEKLY;BYDAY=MO,WE,FR;WKST=MO");
        RecurrenceRule reparsed = RecurrenceRule.Parse(verbose.ToString());
        Console.WriteLine($"input      : INTERVAL=1;FREQ=WEEKLY;BYDAY=MO,WE,FR;WKST=MO");
        Console.WriteLine($"canonical  : {verbose}");
        Console.WriteLine($"round trip : {verbose.Equals(reparsed)}");

        // Equality is by meaning, not by source text: the two spellings above agree.
        RecurrenceRule terse = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,WE,FR");
        Console.WriteLine($"terse == verbose : {terse.Equals(verbose)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an instant with the invariant culture so sample output does not vary by machine.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats the date portion of an instant with the invariant culture.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The formatted date.</returns>
    private static string FormatDate(DateTime value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
