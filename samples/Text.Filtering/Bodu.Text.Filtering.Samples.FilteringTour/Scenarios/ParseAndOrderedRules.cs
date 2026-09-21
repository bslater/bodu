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
        SampleConsole.Scenario(
            "Parsing gitignore lines and the LastMatchWins ordered model",
            what: "Parses four gitignore-style lines - comments, a '!' exclude and a later include - in ordered "
                + "mode and tests three values against them, then builds an allowlist out of the same mechanism.",
            why: "LastMatchWins is a different model from the include/exclude set, not a variation on it. There "
                + "is one ordered list rather than two groups, and the last rule that matches decides - so a "
                + "later include can re-admit a value an earlier exclude rejected. That re-inclusion idiom is "
                + "precisely what the AnyMatch model cannot express, because there an exclude is final. The other "
                + "half of the model is that an unmatched value is included, which is why gitignore files list "
                + "what to ignore rather than what to keep. Together those two rules make the rule list read top "
                + "to bottom like firewall rules, and they are also why an allowlist has to start by excluding "
                + "everything.",
            expect: "'app.log' is rejected by the exclude, but 'important.log' is kept even though the same "
                + "exclude matches it, because the include is declared later - that is the whole point of the "
                + "mode. 'readme.txt' matches nothing and is kept, which is the unmatched-means-included rule. "
                + "The allowlist then inverts the default with a leading '!*' and carves an exception back out, "
                + "giving three different outcomes decided by three different lines.");

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
        Console.WriteLine($"  app.log       -> {filter.IsMatch("app.log")}"
            + "  (expected False - its only matching rule is the exclude)");

        // "important.log" matches BOTH rules; the include is declared later, so it wins -> re-included.
        // This is the gitignore re-inclusion idiom that AnyMatch sets cannot express.
        Console.WriteLine($"  important.log -> {filter.IsMatch("important.log")}"
            + "  (expected True - it matches the same exclude, but a later include overrides it; an AnyMatch set could not express this)");

        // "readme.txt" matches no rule at all; in LastMatchWins unmatched values are INCLUDED,
        // exactly as gitignore treats files no pattern ignores.
        Console.WriteLine($"  readme.txt    -> {filter.IsMatch("readme.txt")}"
            + "  (expected True - unmatched values are included, the default that makes a gitignore file a list of what to ignore)");
        Console.WriteLine();

        // The allowlist idiom: because unmatched values pass, an allowlist starts by excluding EVERYTHING
        // ("!*"), then re-admits what is wanted, then carves exceptions back out — reading top to bottom
        // like firewall rules, with later lines overriding earlier ones.
        var allowlist = TextFilter.Parse(["!*", "error*", "!*debug*"], ordered);

        Console.WriteLine($"  error1      -> {allowlist.IsMatch("error1")}"
            + "  (expected True - last matching rule is the 'error*' include)");
        Console.WriteLine($"  error-debug -> {allowlist.IsMatch("error-debug")}"
            + "  (expected False - it matched the include too, but '!*debug*' comes after it)");
        Console.WriteLine($"  info        -> {allowlist.IsMatch("info")}"
            + "  (expected False - only the leading '!*' matches, which is how an allowlist inverts the default)");

        Console.WriteLine();
    }
}
