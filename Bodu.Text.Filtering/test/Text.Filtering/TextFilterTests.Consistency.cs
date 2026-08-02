// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.Consistency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Regression-tier consistency sweeps: the short-circuiting evaluation path must agree with the exhaustive
/// all-patterns path over a large synthetic corpus, and the statistics must reconcile.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Generates a deterministic corpus of identifier-shaped values.
    /// </summary>
    /// <param name="count">The number of values to generate.</param>
    /// <returns>The generated values.</returns>
    private static string[] GenerateCorpus(int count)
    {
        var random = new Random(20260801);
        string[] stems = ["error", "warn", "info", "debug", "trace", "fatal", "audit", "metric"];
        var values = new string[count];
        for (var i = 0; i < count; i++)
        {
            var stem = stems[random.Next(stems.Length)];
            values[i] = $"{stem}-{random.Next(1000)}{(random.Next(4) == 0 ? ".tmp" : ".log")}";
        }

        return values;
    }

    /// <summary>
    /// Computes the expected outcome for one value under <see cref="TextFilterEvaluationMode.AnyMatch" /> semantics
    /// from the exhaustive all-matching-patterns view.
    /// </summary>
    /// <param name="filter">The filter under test.</param>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>The expected acceptance.</returns>
    private static bool ExpectedAnyMatch(TextFilter filter, string value)
    {
        var matches = filter.GetMatchingPatterns(value);
        var hasIncludes = filter.Patterns.Any(static p => p.Action == TextFilterAction.Include);
        var includeHit = matches.Any(static p => p.Action == TextFilterAction.Include);
        var excludeHit = matches.Any(static p => p.Action == TextFilterAction.Exclude);

        return (!hasIncludes || includeHit) && !excludeHit;
    }

    /// <summary>
    /// Verifies over a 100k-value corpus that the cost-ordered short-circuiting evaluator agrees with the exhaustive
    /// all-patterns evaluation in <see cref="TextFilterEvaluationMode.AnyMatch" /> mode, and that the statistics
    /// buckets reconcile with the total.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IsMatch_WhenSweptOver100kCorpus_ForAnyMatchMode_ShouldAgreeWithExhaustiveEvaluation()
    {
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("error*"),
            TextFilterPattern.Include("{warn,fatal}*"),
            TextFilterPattern.Include(@"^audit-\d+\.log$", TextFilterPatternKind.Regex),
            TextFilterPattern.Exclude("*.tmp"),
            TextFilterPattern.Exclude("*-9??.log"),
        ]);
        var corpus = GenerateCorpus(100_000);

        foreach (var value in corpus)
        {
            var expected = ExpectedAnyMatch(filter, value);
            if (filter.IsMatch(value) != expected)
                Assert.Fail($"Disagreement on '{value}': expected {expected}.");
        }

        var stats = filter.GetStatistics();
        Assert.AreEqual(corpus.Length, stats.ItemsEvaluated);
        Assert.AreEqual(stats.ItemsEvaluated, stats.ItemsAccepted + stats.ItemsExcluded + stats.ItemsNotIncluded);
    }

    /// <summary>
    /// Verifies over a 100k-value corpus that the last-to-first evaluator agrees with the exhaustive
    /// all-patterns view (the last matching rule in declaration order decides; unmatched values are included) in
    /// <see cref="TextFilterEvaluationMode.LastMatchWins" /> mode.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IsMatch_WhenSweptOver100kCorpus_ForLastMatchWinsMode_ShouldAgreeWithExhaustiveEvaluation()
    {
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Exclude("*"),
            TextFilterPattern.Include("{error,warn}*"),
            TextFilterPattern.Exclude("*debug*"),
            TextFilterPattern.Include("*-1.log"),
        ],
        new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins });
        var corpus = GenerateCorpus(100_000);

        foreach (var value in corpus)
        {
            var matches = filter.GetMatchingPatterns(value);
            var expected = matches.Count == 0 || matches[^1].Action == TextFilterAction.Include;
            if (filter.IsMatch(value) != expected)
                Assert.Fail($"Disagreement on '{value}': expected {expected}.");
        }
    }
}
