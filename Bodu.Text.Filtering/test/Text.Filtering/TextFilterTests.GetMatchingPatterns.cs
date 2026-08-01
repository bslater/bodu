// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.GetMatchingPatterns.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.GetMatchingPatterns" />.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that every matching pattern is reported, in declaration order, regardless of action.
    /// </summary>
    [TestMethod]
    public void GetMatchingPatterns_WhenSeveralPatternsMatch_ShouldReportAllInDeclarationOrder()
    {
        var include = TextFilterPattern.Include("error*");
        var exclude = TextFilterPattern.Exclude("*1");
        var unrelated = TextFilterPattern.Include("warn*");
        var filter = TextFilter.Build([include, exclude, unrelated]);

        var matches = filter.GetMatchingPatterns("error1");

        CollectionAssert.AreEqual(new[] { include, exclude }, matches.ToArray());
    }

    /// <summary>
    /// Verifies that a value matching nothing reports an empty list.
    /// </summary>
    [TestMethod]
    public void GetMatchingPatterns_WhenNothingMatches_ShouldReturnEmpty()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("error*")]);

        Assert.AreEqual(0, filter.GetMatchingPatterns("info").Count);
    }

    /// <summary>
    /// Verifies that a brace-alternation pattern matching through several alternatives is reported once.
    /// </summary>
    [TestMethod]
    public void GetMatchingPatterns_WhenSeveralAlternativesOfOnePatternMatch_ShouldReportPatternOnce()
    {
        var braced = TextFilterPattern.Include("{a*,ab*}");
        var filter = TextFilter.Build([braced]);

        var matches = filter.GetMatchingPatterns("abc");

        Assert.AreEqual(1, matches.Count);
        Assert.AreSame(braced, matches[0]);
    }

    /// <summary>
    /// Verifies that the diagnostic surface does not contribute to the filter's statistics.
    /// </summary>
    [TestMethod]
    public void GetMatchingPatterns_WhenCalled_ShouldNotUpdateStatistics()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("error*")]);

        _ = filter.GetMatchingPatterns("error1");

        Assert.AreEqual(0, filter.GetStatistics().ItemsEvaluated);
    }

    /// <summary>
    /// Verifies that the surface also reports matches in <see cref="TextFilterEvaluationMode.LastMatchWins" /> mode.
    /// </summary>
    [TestMethod]
    public void GetMatchingPatterns_WhenModeIsLastMatchWins_ShouldReportAllMatches()
    {
        var exclude = TextFilterPattern.Exclude("*.log");
        var include = TextFilterPattern.Include("important.log");
        var filter = TextFilter.Build([exclude, include], new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins });

        var matches = filter.GetMatchingPatterns("important.log");

        CollectionAssert.AreEqual(new[] { exclude, include }, matches.ToArray());
    }
}
