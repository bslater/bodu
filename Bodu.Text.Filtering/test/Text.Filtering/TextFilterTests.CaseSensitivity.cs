// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.CaseSensitivity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Cross-cutting tests for the case-sensitivity contract: the filter-wide default and the per-pattern override,
/// across the wildcard strategies and the regular-expression tier.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that matching ignores case by default across the literal, prefix, contains, and general strategies.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenDefaultOptions_ShouldIgnoreCaseAcrossStrategies()
    {
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("exact"),
            TextFilterPattern.Include("pre*"),
            TextFilterPattern.Include("*core*"),
            TextFilterPattern.Include("g?neral"),
        ]);

        Assert.IsTrue(filter.IsMatch("EXACT"));
        Assert.IsTrue(filter.IsMatch("PREFIXED"));
        Assert.IsTrue(filter.IsMatch("has-CORE-inside"));
        Assert.IsTrue(filter.IsMatch("GENERAL"));
    }

    /// <summary>
    /// Verifies that disabling the filter-wide default makes matching case-sensitive.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenIgnoreCaseDisabled_ShouldMatchCaseSensitively()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("error*")], new TextFilterOptions { IgnoreCase = false });

        Assert.IsTrue(filter.IsMatch("error1"));
        Assert.IsFalse(filter.IsMatch("ERROR1"));
    }

    /// <summary>
    /// Verifies that a per-pattern override wins over the filter-wide default in both directions.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternOverridesCase_ShouldHonorOverride()
    {
        var caseSensitiveFilter = TextFilter.Build(
            [TextFilterPattern.Include("folded*", ignoreCase: true)],
            new TextFilterOptions { IgnoreCase = false });
        var caseInsensitiveFilter = TextFilter.Build(
            [TextFilterPattern.Include("strict*", ignoreCase: false)],
            new TextFilterOptions { IgnoreCase = true });

        Assert.IsTrue(caseSensitiveFilter.IsMatch("FOLDED-X"));
        Assert.IsFalse(caseInsensitiveFilter.IsMatch("STRICT-X"));
        Assert.IsTrue(caseInsensitiveFilter.IsMatch("strict-x"));
    }

    /// <summary>
    /// Verifies that the case default also governs regular-expression patterns.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternIsRegex_ShouldApplyCaseDefault()
    {
        var folded = TextFilter.Build([TextFilterPattern.Include("^error$", TextFilterPatternKind.Regex)]);
        var strict = TextFilter.Build(
            [TextFilterPattern.Include("^error$", TextFilterPatternKind.Regex)],
            new TextFilterOptions { IgnoreCase = false });

        Assert.IsTrue(folded.IsMatch("ERROR"));
        Assert.IsFalse(strict.IsMatch("ERROR"));
    }
}
