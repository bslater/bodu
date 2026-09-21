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
        SampleConsole.Scenario(
            "Glob grammar and cost tiers",
            what: "Builds a filter mixing brace alternation, a character class, a regex and a contains-glob, "
                + "evaluates five values to see which pattern decided each, asks for every pattern matching an "
                + "overlapping value, and shows a backslash escape demoting a glob to a literal.",
            why: "Two things are happening at build time that make this practical at scale. The braces are "
                + "expanded into separate patterns, so alternation costs nothing per value rather than being "
                + "re-parsed on every call. And each pattern is classified into the cheapest strategy its shape "
                + "allows - a literal equality, a prefix or suffix comparison, a substring search, the general "
                + "wildcard matcher, or a regex - and evaluated in that order, so a regex is only consulted after "
                + "every cheaper pattern has already failed. The diagnostic surfaces exist because a filter that "
                + "gives the wrong answer is nearly impossible to debug from a boolean: Evaluate returns which "
                + "pattern decided, and GetMatchingPatterns returns all of them, which is how you find a rule "
                + "shadowed by another.",
            expect: "Three distinct decision kinds across the five values, each naming the pattern responsible. "
                + "'job-7x' is NotIncluded with no pattern at all, because nothing matched - a different outcome "
                + "from being vetoed, and the distinction matters when tuning rules. GetMatchingPatterns reports "
                + "two patterns for the overlapping value where Evaluate reported only the deciding one, since "
                + "it deliberately does not short-circuit. The escaped pattern matches the literal three "
                + "characters and nothing else.");

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
            Console.WriteLine($"  {value,-16} -> {result.Decision,-12} decided by {result.Pattern?.ToString() ?? "(no pattern)"}");
        }

        Console.WriteLine("  (three decision kinds: matched an include, vetoed by an exclude, and NotIncluded with no pattern - nothing matched at all)");

        Console.WriteLine();

        // GetMatchingPatterns is the deep-diagnostic surface (globset's matches() idea): unlike Evaluate it
        // does NOT short-circuit — it tests every pattern and reports ALL that match, in declaration order.
        // "error-retry-8" matches both the "{error,warn}*" include and the "*retry*" exclude.
        var overlapping = filter.GetMatchingPatterns("error-retry-8");
        Console.WriteLine($"  error-retry-8 matches {overlapping.Count} pattern(s): {string.Join(", ", overlapping)}"
            + "  (Evaluate named only the deciding pattern - this surface does not short-circuit, so it finds rules shadowed by others)");

        // '\' escapes the next metacharacter: this pattern is the LITERAL three characters "a*b".
        // Escaping demotes it to the literal cost tier — a single whole-string equality check.
        var escaped = TextFilter.Build([TextFilterPattern.Include(@"a\*b")]);
        Console.WriteLine($"  literal 'a*b' -> {escaped.IsMatch("a*b")}, 'axb' -> {escaped.IsMatch("axb")}"
            + "  (expected True then False - escaping the star also demotes the pattern to the cheapest tier, a single equality check)");

        Console.WriteLine();
    }
}
