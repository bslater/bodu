// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.GetStatistics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.GetStatistics" /> and <see cref="TextFilter.ResetStatistics" />.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that the scalar counters attribute each decision to the correct bucket and reconcile with the total.
    /// </summary>
    [TestMethod]
    public void GetStatistics_WhenValuesSpanEveryDecision_ShouldCountEachBucketAndReconcile()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("e*"), TextFilterPattern.Exclude("*x")]);

        Assert.IsTrue(filter.IsMatch("e1"));      // Included
        Assert.IsFalse(filter.IsMatch("ex"));     // Excluded
        Assert.IsFalse(filter.IsMatch("other"));  // NotIncluded
        Assert.IsFalse(filter.IsMatch("plain"));  // NotIncluded

        var stats = filter.GetStatistics();

        Assert.AreEqual(4, stats.ItemsEvaluated);
        Assert.AreEqual(1, stats.ItemsAccepted);
        Assert.AreEqual(1, stats.ItemsExcluded);
        Assert.AreEqual(2, stats.ItemsNotIncluded);
        Assert.AreEqual(stats.ItemsEvaluated, stats.ItemsAccepted + stats.ItemsExcluded + stats.ItemsNotIncluded);
    }

    /// <summary>
    /// Verifies that hit counts attribute each decision to the deciding pattern, in pattern declaration order.
    /// </summary>
    [TestMethod]
    public void GetStatistics_WhenPatternsDecideOutcomes_ShouldCountHitsPerDecidingPattern()
    {
        var include = TextFilterPattern.Include("e*");
        var exclude = TextFilterPattern.Exclude("*x");
        var filter = TextFilter.Build([include, exclude]);

        _ = filter.IsMatch("e1");
        _ = filter.IsMatch("e2");
        _ = filter.IsMatch("ex");

        var stats = filter.GetStatistics();

        Assert.AreEqual(2, stats.Patterns.Count);
        Assert.AreSame(include, stats.Patterns[0].Pattern);
        Assert.AreEqual(2, stats.Patterns[0].HitCount);
        Assert.AreSame(exclude, stats.Patterns[1].Pattern);
        Assert.AreEqual(1, stats.Patterns[1].HitCount);
    }

    /// <summary>
    /// Verifies that an accepted-by-default value contributes to the accepted count without crediting any pattern.
    /// </summary>
    [TestMethod]
    public void GetStatistics_WhenValueIncludedByDefault_ShouldNotCreditAnyPattern()
    {
        var filter = TextFilter.Build([TextFilterPattern.Exclude("*x")]);

        Assert.IsTrue(filter.IsMatch("clean"));

        var stats = filter.GetStatistics();

        Assert.AreEqual(1, stats.ItemsAccepted);
        Assert.AreEqual(0, stats.Patterns[0].HitCount);
    }

    /// <summary>
    /// Verifies that resetting zeroes every counter, including per-pattern hit counts.
    /// </summary>
    [TestMethod]
    public void ResetStatistics_WhenCountersAccumulated_ShouldZeroEverything()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("e*")]);
        _ = filter.IsMatch("e1");
        _ = filter.IsMatch("nope");

        filter.ResetStatistics();

        var stats = filter.GetStatistics();
        Assert.AreEqual(0, stats.ItemsEvaluated);
        Assert.AreEqual(0, stats.ItemsAccepted);
        Assert.AreEqual(0, stats.ItemsNotIncluded);
        Assert.AreEqual(0, stats.Patterns[0].HitCount);
        Assert.AreEqual(TimeSpan.Zero, stats.EvaluationTime);
    }

    /// <summary>
    /// Verifies that evaluation time stays <see cref="TimeSpan.Zero" /> unless capture is enabled, and accumulates
    /// when it is.
    /// </summary>
    [TestMethod]
    public void GetStatistics_WhenTimingCaptureToggled_ShouldAccumulateOnlyWhenEnabled()
    {
        var untimed = TextFilter.Build([TextFilterPattern.Include("e*")]);
        var timed = TextFilter.Build([TextFilterPattern.Include("e*")], new TextFilterOptions { CaptureEvaluationTime = true });

        for (var i = 0; i < 1000; i++)
        {
            _ = untimed.IsMatch("e1");
            _ = timed.IsMatch("e1");
        }

        Assert.AreEqual(TimeSpan.Zero, untimed.GetStatistics().EvaluationTime);
        Assert.IsTrue(timed.GetStatistics().EvaluationTime > TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that a timed-out include fails safe as not-matched and is counted as a regex timeout.
    /// </summary>
    [TestMethod]
    public void GetStatistics_WhenIncludeRegexTimesOut_ShouldRejectValueAndCountTimeout()
    {
        var filter = TextFilter.Build(
            [TextFilterPattern.Include(@"^(a+)+b\1$", TextFilterPatternKind.Regex)],
            new TextFilterOptions { RegexMatchTimeout = TimeSpan.FromTicks(1) });

        var result = filter.Evaluate(new string('a', 64));

        Assert.AreEqual(TextFilterDecision.NotIncluded, result.Decision);
        Assert.IsTrue(filter.GetStatistics().RegexTimeouts >= 1);
    }

    /// <summary>
    /// Verifies that a timed-out exclude fails safe as matched — the value is still vetoed — and is counted as a
    /// regex timeout.
    /// </summary>
    [TestMethod]
    public void GetStatistics_WhenExcludeRegexTimesOut_ShouldVetoValueAndCountTimeout()
    {
        var exclude = TextFilterPattern.Exclude(@"^(a+)+b\1$", TextFilterPatternKind.Regex);
        var filter = TextFilter.Build([exclude], new TextFilterOptions { RegexMatchTimeout = TimeSpan.FromTicks(1) });

        var result = filter.Evaluate(new string('a', 64));

        Assert.AreEqual(TextFilterDecision.Excluded, result.Decision);
        Assert.AreSame(exclude, result.Pattern);
        Assert.IsTrue(filter.GetStatistics().RegexTimeouts >= 1);
    }
}
