// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobsAndCostTiers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Filtering;

namespace Bodu.Text.Filtering.Samples.FilteringTour.Scenarios;

/// <summary>
/// Demonstrates the glob grammar — <c>*</c>, <c>?</c>, character classes, <c>{a,b}</c> brace
/// alternation, and <c>\</c> escapes — and the diagnostic surfaces that reveal which pattern
/// decided an outcome. At build time each glob is classified into the cheapest strategy its shape
/// permits (literal, prefix, suffix, contains, general, regex), so evaluation runs cheapest-first.
/// </summary>
public static class GlobsAndCostTiers
{
    /// <summary>
    /// Exercises each grammar feature and inspects decisions with <c>Evaluate</c> and
    /// <c>GetMatchingPatterns</c>.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Glob grammar + cost tiers ---");

        // '{error,warn}*' expands at build time into two cheap prefix matchers; '[0-9]' and '?'
        // route through the general matcher; regex is always the last (most expensive) tier.
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("{error,warn}*"),
            TextFilterPattern.Include("job-[0-9][0-9]"),
            TextFilterPattern.Include(@"^metric\.[a-z]+\.p\d{2}$", TextFilterPatternKind.Regex),
            TextFilterPattern.Exclude("*retry*"),
        ]);

        string[] values = ["warn: slow disk", "job-42", "job-7x", "metric.http.p99", "error-retry-8"];
        foreach (var value in values)
        {
            var result = filter.Evaluate(value);
            Console.WriteLine($"{value,-16} -> {result.Decision,-12} decided by {result.Pattern?.ToString() ?? "(no pattern)"}");
        }

        Console.WriteLine();

        // GetMatchingPatterns reports EVERY pattern that matches (globset's matches() idea) -
        // useful for diagnostics when several patterns overlap.
        var overlapping = filter.GetMatchingPatterns("error-retry-8");
        Console.WriteLine($"error-retry-8 matches {overlapping.Count} pattern(s): {string.Join(", ", overlapping)}");

        // '\' escapes a metacharacter: this pattern matches the literal text "a*b" only.
        var escaped = TextFilter.Build([TextFilterPattern.Include(@"a\*b")]);
        Console.WriteLine($"literal 'a*b' -> {escaped.IsMatch("a*b")}, 'axb' -> {escaped.IsMatch("axb")}");

        Console.WriteLine();
    }
}
