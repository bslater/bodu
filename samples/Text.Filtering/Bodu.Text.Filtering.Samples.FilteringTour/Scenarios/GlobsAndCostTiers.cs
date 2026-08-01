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

        var filter = TextFilter.Build(
        [
            // "{error,warn}*" uses brace alternation. The braces are expanded AT BUILD TIME into two
            // separate cheap prefix matchers ("error*" and "warn*") — alternation costs nothing per value.
            TextFilterPattern.Include("{error,warn}*"),

            // "[0-9]" is a character class matching exactly one character from the range. Classes route the
            // pattern through the general wildcard matcher (still allocation-free, just not a simple prefix test).
            TextFilterPattern.Include("job-[0-9][0-9]"),

            // Anything the glob grammar cannot express becomes a regex pattern. Regexes always sit in the most
            // expensive cost tier, so they are only consulted after every cheaper pattern failed to match.
            TextFilterPattern.Include(@"^metric\.[a-z]+\.p\d{2}$", TextFilterPatternKind.Regex),

            // A contains-glob exclude: vetoes any value with "retry" anywhere in it.
            TextFilterPattern.Exclude("*retry*"),
        ]);

        // One value per interesting decision path; Evaluate() returns the decision AND the deciding pattern.
        string[] values = ["warn: slow disk", "job-42", "job-7x", "metric.http.p99", "error-retry-8"];
        foreach (var value in values)
        {
            // Evaluate is IsMatch plus provenance: TextFilterResult carries the TextFilterDecision
            // (Included / Excluded / NotIncluded / IncludedByDefault) and the pattern that caused it.
            var result = filter.Evaluate(value);

            // Pattern is null when no pattern was involved (nothing matched); ToString on a pattern renders
            // the diagnostic form "+wildcard:text" / "-regex:text" so the output is self-describing.
            Console.WriteLine($"{value,-16} -> {result.Decision,-12} decided by {result.Pattern?.ToString() ?? "(no pattern)"}");
        }

        Console.WriteLine();

        // GetMatchingPatterns is the deep-diagnostic surface (globset's matches() idea): unlike Evaluate it
        // does NOT short-circuit — it tests every pattern and reports ALL that match, in declaration order.
        // "error-retry-8" matches both the "{error,warn}*" include and the "*retry*" exclude.
        var overlapping = filter.GetMatchingPatterns("error-retry-8");
        Console.WriteLine($"error-retry-8 matches {overlapping.Count} pattern(s): {string.Join(", ", overlapping)}");

        // '\' escapes the next metacharacter: this pattern is the LITERAL three characters "a*b".
        // Escaping demotes it to the literal cost tier — a single whole-string equality check.
        var escaped = TextFilter.Build([TextFilterPattern.Include(@"a\*b")]);
        Console.WriteLine($"literal 'a*b' -> {escaped.IsMatch("a*b")}, 'axb' -> {escaped.IsMatch("axb")}");

        Console.WriteLine();
    }
}
