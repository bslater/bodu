// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.Evaluate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.Evaluate" /> — the decision enumeration and the deciding-pattern contract.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that a value accepted with no include patterns reports
    /// <see cref="TextFilterDecision.IncludedByDefault" /> and no deciding pattern.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenIncludeSetIsEmptyAndNoExcludeMatches_ShouldReportIncludedByDefault()
    {
        var filter = TextFilter.Build([TextFilterPattern.Exclude("*.tmp")]);

        var result = filter.Evaluate("report.txt");

        Assert.AreEqual(TextFilterDecision.IncludedByDefault, result.Decision);
        Assert.IsNull(result.Pattern);
        Assert.IsTrue(result.IsMatch);
    }

    /// <summary>
    /// Verifies that a value admitted by an include reports <see cref="TextFilterDecision.Included" /> with that
    /// pattern.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenIncludeMatches_ShouldReportIncludedWithDecidingPattern()
    {
        var include = TextFilterPattern.Include("error*");
        var filter = TextFilter.Build([include]);

        var result = filter.Evaluate("error1");

        Assert.AreEqual(TextFilterDecision.Included, result.Decision);
        Assert.AreSame(include, result.Pattern);
        Assert.IsTrue(result.IsMatch);
    }

    /// <summary>
    /// Verifies that a vetoed value reports <see cref="TextFilterDecision.Excluded" /> with the vetoing pattern.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenExcludeMatches_ShouldReportExcludedWithVetoingPattern()
    {
        var exclude = TextFilterPattern.Exclude("*debug*");
        var filter = TextFilter.Build([TextFilterPattern.Include("error*"), exclude]);

        var result = filter.Evaluate("error-debug");

        Assert.AreEqual(TextFilterDecision.Excluded, result.Decision);
        Assert.AreSame(exclude, result.Pattern);
        Assert.IsFalse(result.IsMatch);
    }

    /// <summary>
    /// Verifies that a value matching no include reports <see cref="TextFilterDecision.NotIncluded" /> and no
    /// deciding pattern.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenIncludesExistAndNoneMatch_ShouldReportNotIncluded()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("error*")]);

        var result = filter.Evaluate("info");

        Assert.AreEqual(TextFilterDecision.NotIncluded, result.Decision);
        Assert.IsNull(result.Pattern);
        Assert.IsFalse(result.IsMatch);
    }

    /// <summary>
    /// Verifies that when several includes match, the reported deciding pattern is one of the matching includes.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenSeveralIncludesMatch_ShouldReportOneOfTheMatchingPatterns()
    {
        var regex = TextFilterPattern.Include("^err.*$", TextFilterPatternKind.Regex);
        var literal = TextFilterPattern.Include("error");
        var filter = TextFilter.Build([regex, literal]);

        var result = filter.Evaluate("error");

        Assert.AreEqual(TextFilterDecision.Included, result.Decision);
        Assert.IsTrue(ReferenceEquals(result.Pattern, regex) || ReferenceEquals(result.Pattern, literal));
    }

    /// <summary>
    /// Verifies that a pattern declared through brace alternation is reported as the deciding pattern itself, not as
    /// one of its expanded alternatives.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenBraceAlternativeMatches_ShouldReportDeclaredPattern()
    {
        var braced = TextFilterPattern.Include("{error,warn}*");
        var filter = TextFilter.Build([braced]);

        var result = filter.Evaluate("warn7");

        Assert.AreSame(braced, result.Pattern);
    }
}
