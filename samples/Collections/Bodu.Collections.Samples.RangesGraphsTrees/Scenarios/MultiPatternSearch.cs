// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiPatternSearch.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Trees;

namespace Bodu.Collections.Samples.RangesGraphsTrees.Scenarios;

/// <summary>
/// Demonstrates <see cref="AhoCorasickAutomaton{TValue}" />: a finite-state machine that scans a body of text
/// once and reports every occurrence of any pattern in a dictionary, in a single linear pass, regardless of
/// how many patterns are registered.
/// </summary>
/// <remarks>
/// The automaton reports <em>overlapping</em> matches, which a naive loop of independent searches would not:
/// scanning for each pattern in turn and skipping past every hit loses matches that start inside an earlier one.
/// That is why the classic "ushers" example is used here — <c>she</c>, <c>he</c> and <c>hers</c> all overlap.
/// </remarks>
public static class MultiPatternSearch
{
    /// <summary>
    /// Builds an automaton from a keyword-to-category dictionary and enumerates all matches over a text.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AhoCorasickAutomaton<TValue> - multi-pattern scan",
            what: "Registers four patterns with associated values and scans a short string that makes three of " +
                  "them overlap.",
            why: "Searching a document for many terms by looping over the terms costs one pass per term, so the " +
                  "work grows with the dictionary - untenable once the dictionary is thousands of terms. This " +
                  "automaton makes one pass regardless of how many patterns are registered. It also finds " +
                  "overlapping matches, which the loop-per-term approach quietly drops: once a search advances " +
                  "past a hit, any match starting inside that hit is gone.",
            expect: "Three matches in a six-character string, deliberately overlapping: he at [2..4), she at " +
                    "[1..4) and hers at [2..6). Every one shares characters with another, and a naive scan that " +
                    "skipped past the first hit would have reported only one of them.");

        // Each pattern carries a value (its category). Build compiles the goto/fail/output automaton once.
        var keywords = new Dictionary<string, string>
        {
            ["he"] = "pronoun",
            ["she"] = "pronoun",
            ["his"] = "possessive",
            ["hers"] = "possessive",
        };
        var automaton = AhoCorasickAutomaton<string>.Build(keywords);

        const string text = "ushers";
        Console.WriteLine($"  patterns : {automaton.Patterns.Count}  (expected 4 - the scan below costs one pass whether this is 4 or 40,000)");
        Console.WriteLine($"  text     : \"{text}\"  (six characters chosen so three patterns overlap inside them)");

        // EnumerateMatches yields every match - including overlapping ones ("she", "he", "hers" all hide in
        // "ushers") - in ascending start position, which is already deterministic.
        foreach (var match in automaton.EnumerateMatches(text))
            Console.WriteLine($"  match    : \u0027{match.Pattern}\u0027 ({match.Value}) at [{match.Start}..{match.End})  (half-open span; the value rides along, so the automaton doubles as a lookup)");

        Console.WriteLine($"  total    : {automaton.CountMatches(text)} match(es)  (expected 3 - overlapping hits all count; a loop-per-pattern scan would have missed some)");

        Console.WriteLine();
    }
}
