// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseAndOrderedRules.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Filtering;

namespace Bodu.Text.Filtering.Samples.FilteringTour.Scenarios;

/// <summary>
/// Demonstrates parsing raw pattern lines with the gitignore file conventions and the
/// <c>LastMatchWins</c> ordered-rule mode, where the last matching rule decides and a later include
/// can re-admit a value an earlier exclude rejected.
/// </summary>
public static class ParseAndOrderedRules
{
    /// <summary>
    /// Parses gitignore-style lines in both evaluation modes and shows the ordered-rule behaviors.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Parse + gitignore-style ordered rules (LastMatchWins) ---");

        // TextFilterOptions selects the evaluation mode at build time. LastMatchWins is the gitignore model:
        // the rules form ONE ordered list and the LAST rule that matches a value decides its fate.
        var ordered = new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins };

        // TextFilter.Parse reads raw lines exactly like a .gitignore file:
        //   - a line starting with '#' is a comment and is skipped,
        //   - a bare line becomes an INCLUDE pattern,
        //   - a leading '!' flips the line into an EXCLUDE pattern,
        //   - blank lines are skipped, and surrounding whitespace is trimmed.
        var filter = TextFilter.Parse(
        [
            "# keep everything except logs...",  // comment - ignored by the parser
            "!*.log",                            // exclude rule: veto anything ending in ".log"
            "# ...but this one matters",         // comment - ignored by the parser
            "important.log",                     // include rule DECLARED LATER: re-admits this exact name
        ],
        ordered);

        // "app.log" matches only "!*.log"            -> the last (only) match is an exclude -> rejected.
        Console.WriteLine($"app.log       -> {filter.IsMatch("app.log")}");

        // "important.log" matches BOTH rules; the include is declared later, so it wins -> re-included.
        // This is the gitignore re-inclusion idiom that AnyMatch sets cannot express.
        Console.WriteLine($"important.log -> {filter.IsMatch("important.log")} (re-included by the later rule)");

        // "readme.txt" matches no rule at all; in LastMatchWins unmatched values are INCLUDED,
        // exactly as gitignore treats files no pattern ignores.
        Console.WriteLine($"readme.txt    -> {filter.IsMatch("readme.txt")} (unmatched values are included)");
        Console.WriteLine();

        // The allowlist idiom: because unmatched values pass, an allowlist starts by excluding EVERYTHING
        // ("!*"), then re-admits what is wanted, then carves exceptions back out — reading top to bottom
        // like firewall rules, with later lines overriding earlier ones.
        var allowlist = TextFilter.Parse(["!*", "error*", "!*debug*"], ordered);

        Console.WriteLine($"error1      -> {allowlist.IsMatch("error1")}");        // last match "error*"   -> included
        Console.WriteLine($"error-debug -> {allowlist.IsMatch("error-debug")}");   // last match "!*debug*" -> excluded
        Console.WriteLine($"info        -> {allowlist.IsMatch("info")}");          // last match "!*"       -> excluded

        Console.WriteLine();
    }
}
