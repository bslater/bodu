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
public static class MultiPatternSearch
{
    /// <summary>
    /// Builds an automaton from a keyword-to-category dictionary and enumerates all matches over a text.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AhoCorasickAutomaton<TValue>: multi-pattern scan ---");

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
        Console.WriteLine($"  patterns : {automaton.Patterns.Count}");
        Console.WriteLine($"  text     : \"{text}\"");

        // EnumerateMatches yields every match - including overlapping ones ("she", "he", "hers" all hide in
        // "ushers") - in ascending start position, which is already deterministic.
        foreach (var match in automaton.EnumerateMatches(text))
            Console.WriteLine($"  match    : '{match.Pattern}' ({match.Value}) at [{match.Start}..{match.End})");

        Console.WriteLine($"  total    : {automaton.CountMatches(text)} match(es)");

        Console.WriteLine();
    }
}
