// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.IsMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.IsMatch(string)" /> and the <see cref="TextFilterEvaluationMode.AnyMatch" />
/// semantics matrix.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that a typical include/exclude set accepts the matching values and rejects the rest — the library's
    /// primary happy path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void IsMatch_WhenTypicalIncludeExcludeSet_ShouldAcceptExpectedItems()
    {
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("error*"),
            TextFilterPattern.Include("warn*"),
            TextFilterPattern.Exclude("*debug*"),
        ]);

        Assert.IsTrue(filter.IsMatch("error: disk full"));
        Assert.IsTrue(filter.IsMatch("WARN: retrying"));
        Assert.IsFalse(filter.IsMatch("error-debug-trace"));
        Assert.IsFalse(filter.IsMatch("info: started"));
    }

    /// <summary>
    /// Verifies that with no include patterns every value passes unless an exclude vetoes it.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenOnlyExcludesDeclared_ShouldIncludeAllExceptVetoed()
    {
        var filter = TextFilter.Build([TextFilterPattern.Exclude("*.tmp")]);

        Assert.IsTrue(filter.IsMatch("report.txt"));
        Assert.IsFalse(filter.IsMatch("scratch.tmp"));
    }

    /// <summary>
    /// Verifies that with include patterns declared, a value matching none of them is rejected.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenIncludesDeclaredAndNoneMatch_ShouldReject()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("error*")]);

        Assert.IsFalse(filter.IsMatch("info"));
    }

    /// <summary>
    /// Verifies that an exclude vetoes a value even when an include matches it.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenIncludeAndExcludeBothMatch_ShouldRejectByVeto()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("error*"), TextFilterPattern.Exclude("*x")]);

        Assert.IsTrue(filter.IsMatch("error1"));
        Assert.IsFalse(filter.IsMatch("errorx"));
    }

    /// <summary>
    /// Verifies that declaration order does not affect the outcome in
    /// <see cref="TextFilterEvaluationMode.AnyMatch" /> mode.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternOrderReversed_ForAnyMatchMode_ShouldProduceSameOutcome()
    {
        TextFilterPattern[] patterns = [TextFilterPattern.Exclude("*x"), TextFilterPattern.Include("error*")];
        var forward = TextFilter.Build(patterns);
        var reversed = TextFilter.Build(Enumerable.Reverse(patterns).ToArray());
        string[] values = ["error1", "errorx", "info", "x"];

        foreach (var value in values)
            Assert.AreEqual(forward.IsMatch(value), reversed.IsMatch(value), $"Disagreement on '{value}'.");
    }

    /// <summary>
    /// Verifies that the string and span overloads agree for the same values.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenCalledWithStringAndSpan_ShouldAgree()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("a?c*"), TextFilterPattern.Exclude("*z")]);
        string[] values = ["abc", "abcd", "abz", "ac", string.Empty];

        foreach (var value in values)
            Assert.AreEqual(filter.IsMatch(value), filter.IsMatch(value.AsSpan()), $"Disagreement on '{value}'.");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> value throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var filter = TextFilter.Build([]);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.IsMatch(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a brace-alternation pattern admits every expanded alternative.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternUsesBraceAlternation_ShouldMatchEveryAlternative()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("{error,warn,fatal}*")]);

        Assert.IsTrue(filter.IsMatch("error1"));
        Assert.IsTrue(filter.IsMatch("warn2"));
        Assert.IsTrue(filter.IsMatch("fatal3"));
        Assert.IsFalse(filter.IsMatch("info4"));
    }

    /// <summary>
    /// Verifies that wildcard and regular-expression patterns cooperate within one filter.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenWildcardAndRegexPatternsMix_ShouldCombineResults()
    {
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("error*"),
            TextFilterPattern.Include(@"^\d{3}$", TextFilterPatternKind.Regex),
            TextFilterPattern.Exclude("*7"),
        ]);

        Assert.IsTrue(filter.IsMatch("error!"));
        Assert.IsTrue(filter.IsMatch("123"));
        Assert.IsFalse(filter.IsMatch("127"));
        Assert.IsFalse(filter.IsMatch("12"));
    }
}
