// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.EvaluationMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Cross-cutting tests for the <see cref="TextFilterEvaluationMode.LastMatchWins" /> ordered-rule semantics and their
/// contrast with <see cref="TextFilterEvaluationMode.AnyMatch" />.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// The options selecting gitignore-style ordered evaluation.
    /// </summary>
    private static readonly TextFilterOptions LastMatchWins = new() { Mode = TextFilterEvaluationMode.LastMatchWins };

    /// <summary>
    /// Verifies that a later include re-admits a value an earlier exclude rejected — the gitignore re-inclusion
    /// pattern.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenLaterIncludeReAdmits_ForLastMatchWins_ShouldAcceptValue()
    {
        var filter = TextFilter.Parse(["!*.log", "important.log"], LastMatchWins);

        Assert.IsFalse(filter.IsMatch("app.log"));
        Assert.IsTrue(filter.IsMatch("important.log"));
        Assert.IsTrue(filter.IsMatch("readme.txt"));
    }

    /// <summary>
    /// Verifies that an unmatched value is included — the gitignore-faithful default.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenNoRuleMatches_ForLastMatchWins_ShouldIncludeByDefault()
    {
        var filter = TextFilter.Build([TextFilterPattern.Exclude("*.tmp")], LastMatchWins);

        var result = filter.Evaluate("report.txt");

        Assert.AreEqual(TextFilterDecision.IncludedByDefault, result.Decision);
        Assert.IsNull(result.Pattern);
    }

    /// <summary>
    /// Verifies that an allowlist is expressed with a leading exclude-everything rule.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenLeadingExcludeAllThenIncludes_ForLastMatchWins_ShouldActAsAllowlist()
    {
        var filter = TextFilter.Parse(["!*", "error*", "!*debug*"], LastMatchWins);

        Assert.IsTrue(filter.IsMatch("error1"));
        Assert.IsFalse(filter.IsMatch("error-debug"));
        Assert.IsFalse(filter.IsMatch("info"));
    }

    /// <summary>
    /// Verifies that the last matching rule decides and is reported as the deciding pattern.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenSeveralRulesMatch_ForLastMatchWins_ShouldReportLastMatchingRule()
    {
        var first = TextFilterPattern.Include("a*");
        var second = TextFilterPattern.Exclude("ab*");
        var third = TextFilterPattern.Include("abc*");
        var filter = TextFilter.Build([first, second, third], LastMatchWins);

        Assert.AreSame(third, filter.Evaluate("abcd").Pattern);
        Assert.AreSame(second, filter.Evaluate("abx").Pattern);
        Assert.AreSame(first, filter.Evaluate("ax").Pattern);
    }

    /// <summary>
    /// Verifies that declaration order changes the outcome in ordered mode — the semantic difference from
    /// <see cref="TextFilterEvaluationMode.AnyMatch" />, where the same rules are order-independent.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenOrderReversed_ForLastMatchWins_ShouldChangeOutcome()
    {
        TextFilterPattern[] rules = [TextFilterPattern.Exclude("*.log"), TextFilterPattern.Include("important.log")];
        var forward = TextFilter.Build(rules, LastMatchWins);
        var reversed = TextFilter.Build(Enumerable.Reverse(rules).ToArray(), LastMatchWins);

        Assert.IsTrue(forward.IsMatch("important.log"));
        Assert.IsFalse(reversed.IsMatch("important.log"));
    }

    /// <summary>
    /// Verifies that the same rule set decides differently under the two modes when an exclude and an include both
    /// match: sets let the exclude veto, ordered rules let the later include win.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenExcludeAndLaterIncludeBothMatch_ShouldDifferBetweenModes()
    {
        TextFilterPattern[] rules = [TextFilterPattern.Exclude("*.log"), TextFilterPattern.Include("important.log")];
        var sets = TextFilter.Build(rules);
        var ordered = TextFilter.Build(rules, LastMatchWins);

        Assert.IsFalse(sets.IsMatch("important.log"));
        Assert.IsTrue(ordered.IsMatch("important.log"));
    }
}
