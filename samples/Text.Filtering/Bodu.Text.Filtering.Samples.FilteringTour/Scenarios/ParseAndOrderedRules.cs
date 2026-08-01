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

        // Parse understands the gitignore file conventions: bare line = include, '!' = exclude,
        // '#' = comment, blank lines skipped. In LastMatchWins mode the LAST matching rule decides.
        var ordered = new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins };
        var filter = TextFilter.Parse(
        [
            "# keep everything except logs...",
            "!*.log",
            "# ...but this one matters",
            "important.log",
        ],
        ordered);

        Console.WriteLine($"app.log       -> {filter.IsMatch("app.log")}");
        Console.WriteLine($"important.log -> {filter.IsMatch("important.log")} (re-included by the later rule)");
        Console.WriteLine($"readme.txt    -> {filter.IsMatch("readme.txt")} (unmatched values are included)");
        Console.WriteLine();

        // Allowlists are expressed with a leading exclude-everything rule - the iptables-style shape.
        var allowlist = TextFilter.Parse(["!*", "error*", "!*debug*"], ordered);
        Console.WriteLine($"error1      -> {allowlist.IsMatch("error1")}");
        Console.WriteLine($"error-debug -> {allowlist.IsMatch("error-debug")}");
        Console.WriteLine($"info        -> {allowlist.IsMatch("info")}");

        Console.WriteLine();
    }
}
