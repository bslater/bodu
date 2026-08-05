// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyBlocks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceSets.Scenarios;

/// <summary>
/// Demonstrates the iCalendar property-block form: a <see cref="RecurrenceSet" /> renders to the
/// <c>DTSTART</c> / <c>RRULE</c> / <c>RDATE</c> / <c>EXDATE</c> lines a calendar file carries, and
/// parses back from them — a round trip that makes the text a storage format rather than a display
/// form.
/// </summary>
public static class PropertyBlocks
{
    /// <summary>
    /// Renders a set to text, parses it back, and compares the two by value.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Rendering a set to an iCalendar property block ---");

        var start = new DateTime(2026, 3, 2, 14, 30, 0);
        var set = new RecurrenceSet(
            start,
            [RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,WE;COUNT=8")],
            [new DateTime(2026, 3, 20, 14, 30, 0)],
            [new DateTime(2026, 3, 11, 14, 30, 0)]);

        // ToString emits CRLF-separated property lines, in the order a calendar file uses.
        string text = set.ToString();
        Console.WriteLine("ToString():");
        foreach (string line in text.Split("\r\n"))
        {
            Console.WriteLine($"  {line}");
        }

        Console.WriteLine();
        Console.WriteLine($"occurrences : {Join(set.GetOccurrences())}");

        Console.WriteLine();
        Console.WriteLine("--- Parsing it back ---");

        // Parse accepts the same block, so a set survives a trip through storage unchanged.
        RecurrenceSet reparsed = RecurrenceSet.Parse(text);
        Console.WriteLine($"round trip equal    : {set.Equals(reparsed)}");
        Console.WriteLine($"hash codes match    : {set.GetHashCode() == reparsed.GetHashCode()}");
        Console.WriteLine($"same occurrences    : {set.GetOccurrences().SequenceEqual(reparsed.GetOccurrences())}");

        Console.WriteLine();
        Console.WriteLine("--- Parsing a block written by hand ---");

        // The parser accepts the properties in any order and tolerates multi-valued RDATE/EXDATE
        // lines, which is how calendar files in the wild are written.
        string handWritten = string.Join(
            "\r\n",
            "DTSTART:20260601T080000",
            "RRULE:FREQ=DAILY;COUNT=10",
            "EXDATE:20260603T080000,20260604T080000",
            "RDATE:20260615T080000");

        Console.WriteLine("input:");
        foreach (string line in handWritten.Split("\r\n"))
        {
            Console.WriteLine($"  {line}");
        }

        RecurrenceSet parsed = RecurrenceSet.Parse(handWritten);
        Console.WriteLine();
        Console.WriteLine($"Rules          : {parsed.Rules.Count}");
        Console.WriteLine($"Dates          : {Join(parsed.Dates)}");
        Console.WriteLine($"ExceptionDates : {Join(parsed.ExceptionDates)}");
        Console.WriteLine($"occurrences    : {Join(parsed.GetOccurrences())}");
        Console.WriteLine("                 (the 3rd and 4th are excluded; the 15th is added)");

        Console.WriteLine();
        Console.WriteLine("--- Equality is by value, not by text ---");

        // Two sets built differently but denoting the same schedule compare equal, so a set can be
        // used as a dictionary key or compared in a test without normalizing the source text.
        var a = new RecurrenceSet(
            new DateTime(2026, 5, 4, 10, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY;COUNT=3")],
            exceptionDates: [new DateTime(2026, 5, 5, 10, 0, 0)]);

        RecurrenceSet b = RecurrenceSet.Parse(
            "DTSTART:20260504T100000\r\nRRULE:FREQ=DAILY;COUNT=3\r\nEXDATE:20260505T100000");

        Console.WriteLine($"constructed == parsed : {a.Equals(b)}");
        Console.WriteLine($"occurrences           : {Join(a.GetOccurrences())}");

        Console.WriteLine();
        Console.WriteLine("--- Malformed text names its defect ---");

        // The defect-reporting overload matches the other three schedule forms, so configuration
        // validation looks the same whichever form a host is reading.
        string[] malformed =
        [
            "",
            "RRULE:FREQ=DAILY",
            "DTSTART:20260504T100000",
            "DTSTART:not-a-date\r\nRRULE:FREQ=DAILY",
            "DTSTART:20260504T100000\r\nRRULE:FREQ=FORTNIGHTLY",
        ];

        foreach (string candidate in malformed)
        {
            bool ok = RecurrenceSet.TryParse(candidate, out RecurrenceSet? result, out string? defect);
            string label = candidate.Length == 0 ? "(empty)" : candidate.Replace("\r\n", " | ", StringComparison.Ordinal);
            Console.WriteLine($"  {label}");
            Console.WriteLine($"    parsed={ok} {(ok ? $"-> {result!.Rules.Count} rule(s)" : defect)}");
        }

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
