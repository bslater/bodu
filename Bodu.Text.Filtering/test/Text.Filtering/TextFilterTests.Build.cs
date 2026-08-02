// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.Build.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.Build(IEnumerable{TextFilterPattern}, TextFilterOptions?)" />.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> pattern collection throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = TextFilter.Build(null!);
        });

        Assert.AreEqual("patterns", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a pattern collection containing a <see langword="null" /> entry throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsContainsNull_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = TextFilter.Build([TextFilterPattern.Include("a"), null!]);
        });

        Assert.AreEqual("patterns", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty pattern set builds an include-all filter.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternSetIsEmpty_ShouldAcceptEveryValue()
    {
        var filter = TextFilter.Build([]);

        Assert.AreEqual(0, filter.Patterns.Count);
        Assert.IsTrue(filter.IsMatch("anything"));
        Assert.AreEqual(TextFilterDecision.IncludedByDefault, filter.Evaluate("anything").Decision);
    }

    /// <summary>
    /// Verifies that <see cref="TextFilter.Patterns" /> preserves declaration order even though evaluation reorders
    /// compiled entries by cost.
    /// </summary>
    [TestMethod]
    public void Build_WhenPatternsSpanCostTiers_ShouldPreserveDeclarationOrderInPatterns()
    {
        var regex = TextFilterPattern.Include("^a+$", TextFilterPatternKind.Regex);
        var literal = TextFilterPattern.Include("abc");
        var filter = TextFilter.Build([regex, literal]);

        Assert.AreSame(regex, filter.Patterns[0]);
        Assert.AreSame(literal, filter.Patterns[1]);
    }

    /// <summary>
    /// Verifies that the filter reflects the mode and case default it was built with.
    /// </summary>
    [TestMethod]
    public void Build_WhenOptionsSupplied_ShouldExposeModeAndIgnoreCase()
    {
        var filter = TextFilter.Build([], new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins, IgnoreCase = false });

        Assert.AreEqual(TextFilterEvaluationMode.LastMatchWins, filter.Mode);
        Assert.IsFalse(filter.IgnoreCase);
    }

    /// <summary>
    /// Verifies that an invalid regular-expression pattern throws <see cref="ArgumentException" /> preserving the
    /// engine's parse failure as the inner exception.
    /// </summary>
    [TestMethod]
    public void Build_WhenRegexPatternIsInvalid_ShouldThrowArgumentExceptionWithInnerException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = TextFilter.Build([TextFilterPattern.Include("(unclosed", TextFilterPatternKind.Regex)]);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<ArgumentException>(ex.InnerException);
        Assert.IsTrue(ex.Message.Contains("(unclosed", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a regular expression the non-backtracking engine cannot compile (a backreference) still builds
    /// via the backtracking fallback and matches correctly.
    /// </summary>
    [TestMethod]
    public void Build_WhenRegexUsesBackreference_ShouldFallBackToBacktrackingEngine()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include(@"^(a+)\1$", TextFilterPatternKind.Regex)]);

        Assert.IsTrue(filter.IsMatch("aa"));
        Assert.IsFalse(filter.IsMatch("a"));
    }

    /// <summary>
    /// Verifies that a malformed wildcard pattern surfaces its grammar error from the build.
    /// </summary>
    [TestMethod]
    public void Build_WhenWildcardPatternIsMalformed_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = TextFilter.Build([TextFilterPattern.Include("[unterminated")]);
        });
    }

    /// <summary>
    /// Verifies that an undefined evaluation mode on the options throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenOptionsModeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = TextFilter.Build([], new TextFilterOptions { Mode = (TextFilterEvaluationMode)99 });
        });
    }

    /// <summary>
    /// Verifies that a non-positive regular-expression match timeout throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenRegexMatchTimeoutIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = TextFilter.Build([], new TextFilterOptions { RegexMatchTimeout = TimeSpan.Zero });
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" /> is accepted as a
    /// regular-expression match timeout.
    /// </summary>
    [TestMethod]
    public void Build_WhenRegexMatchTimeoutIsInfinite_ShouldSucceed()
    {
        var filter = TextFilter.Build(
            [TextFilterPattern.Include("a+", TextFilterPatternKind.Regex)],
            new TextFilterOptions { RegexMatchTimeout = System.Text.RegularExpressions.Regex.InfiniteMatchTimeout });

        Assert.IsTrue(filter.IsMatch("aaa"));
    }
}
