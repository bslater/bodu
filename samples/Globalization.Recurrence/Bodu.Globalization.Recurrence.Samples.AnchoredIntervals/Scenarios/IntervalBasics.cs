// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.AnchoredIntervals.Scenarios;

/// <summary>
/// Demonstrates the <see cref="AnchoredInterval" /> basics: constructing one from a
/// <see cref="TimeSpan" /> or from RFC 5545 duration text, the canonical text it renders back to,
/// and value equality.
/// </summary>
public static class IntervalBasics
{
    /// <summary>
    /// Builds intervals both ways, renders them, and compares them.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AnchoredInterval: construction ---");

        // An anchored interval is the simplest recurrence form there is: a fixed spacing, with no
        // calendar semantics at all. It stores ONLY the interval -- the anchor is supplied per
        // query, which is what lets one instance serve many series.
        var fromTimeSpan = new AnchoredInterval(TimeSpan.FromHours(6));
        AnchoredInterval fromText = AnchoredInterval.Parse("PT6H");

        Console.WriteLine($"new AnchoredInterval(TimeSpan.FromHours(6)) : {fromTimeSpan}");
        Console.WriteLine($"AnchoredInterval.Parse(\"PT6H\")              : {fromText}");
        Console.WriteLine($"Interval property                           : {fromText.Interval}");
        Console.WriteLine($"equal                                       : {fromTimeSpan.Equals(fromText)}");

        Console.WriteLine();
        Console.WriteLine("--- The RFC 5545 duration grammar ---");

        // The grammar is the iCalendar dur-value production: an optional sign, 'P', then week,
        // day, and (after 'T') hour, minute and second components.
        (string Text, string Meaning)[] durations =
        [
            ("PT30S", "thirty seconds"),
            ("PT15M", "fifteen minutes"),
            ("PT1H", "one hour"),
            ("PT1H30M", "ninety minutes"),
            ("P1D", "one day"),
            ("P1DT12H", "a day and a half"),
            ("P1W", "one week"),
            ("P2W", "a fortnight"),
            ("PT0.5H", "invalid -- components are whole numbers"),
        ];

        foreach ((string text, string meaning) in durations)
        {
            if (AnchoredInterval.TryParse(text, out AnchoredInterval? interval))
            {
                Console.WriteLine($"  {text,-10} {interval!.Interval,-16} {meaning}");
            }
            else
            {
                Console.WriteLine($"  {text,-10} {"(rejected)",-16} {meaning}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Canonical duration text ---");

        // ToString renders the canonical form, which is not always the input spelling: components
        // are normalized, and a whole number of weeks renders in the weeks form.
        (string Input, string Note)[] canonical =
        [
            ("PT60M", "sixty minutes normalizes to an hour"),
            ("PT3600S", "and so does 3600 seconds"),
            ("PT90M", "ninety minutes is an hour and a half"),
            ("P7D", "seven days is a week"),
            ("P14D", "and fourteen is a fortnight"),
            ("P10D", "ten days is not a whole number of weeks"),
            ("P1DT2H3M4S", "mixed components are preserved"),
            ("+PT4H", "an explicit plus sign is the positive form"),
        ];

        foreach ((string input, string note) in canonical)
        {
            AnchoredInterval interval = AnchoredInterval.Parse(input);
            Console.WriteLine($"  {input,-12} -> {interval,-12} ({note})");
        }

        Console.WriteLine();
        Console.WriteLine("--- Equality and round-tripping ---");

        // Equality is by elapsed time, so every spelling of the same duration compares equal --
        // and the canonical text re-parses to an equal value.
        (string Left, string Right)[] pairs =
        [
            ("PT1H", "PT60M"),
            ("PT1H", "PT3600S"),
            ("P1W", "P7D"),
            ("P1DT12H", "PT36H"),
            ("PT1H", "PT2H"),
        ];

        foreach ((string left, string right) in pairs)
        {
            AnchoredInterval a = AnchoredInterval.Parse(left);
            AnchoredInterval b = AnchoredInterval.Parse(right);
            Console.WriteLine($"  {left,-10} == {right,-10} : {a.Equals(b),-5} (hash match: {a.GetHashCode() == b.GetHashCode()})");
        }

        AnchoredInterval original = AnchoredInterval.Parse("P1DT2H3M4S");
        Console.WriteLine($"  round trip '{original}' : {original.Equals(AnchoredInterval.Parse(original.ToString()))}");

        Console.WriteLine();
    }
}
