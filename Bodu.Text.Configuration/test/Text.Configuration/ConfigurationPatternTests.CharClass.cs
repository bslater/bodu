// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.CharClass.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Regression-tier sweep: verifies that every character in a small range matches the corresponding
    /// character class, and that characters outside the range do not match.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void IsMatch_WhenCharacterClassCoversAlphaRange_ShouldMatchEachLetterInRange()
    {
        var pattern = ConfigurationPattern.Compile("file-[a-e].txt");

        for (char c = 'a'; c <= 'e'; c++)
            Assert.IsTrue(pattern.IsMatch($"file-{c}.txt"), $"expected match for {c}");

        for (char c = 'f'; c <= 'z'; c++)
            Assert.IsFalse(pattern.IsMatch($"file-{c}.txt"), $"unexpected match for {c}");
    }

    /// <summary>
    /// Verifies that an empty negated set rejects every character, matching the EditorConfig 0.17.2 behaviour
    /// of treating <c>[!]</c> as "not in the empty set" - every character matches.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenCharacterClassIsExplicitAndContains_ShouldMatchOnlyListed()
    {
        var pattern = ConfigurationPattern.Compile("foo[xyz]bar");

        Assert.IsTrue(pattern.IsMatch("fooxbar"));
        Assert.IsTrue(pattern.IsMatch("fooybar"));
        Assert.IsTrue(pattern.IsMatch("foozbar"));
        Assert.IsFalse(pattern.IsMatch("fooabar"));
    }

    /// <summary>
    /// Verifies that a bracket expression containing <c>/</c> is literal text, because a single-character class cannot
    /// match a path separator: <c>ab[e/]cd.i</c> matches only the name written with its brackets, and neither
    /// <c>ab/cd.i</c> nor <c>abecd.i</c> (EditorConfig core-tests <c>brackets_slash_inside1</c> to
    /// <c>brackets_slash_inside3</c>).
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenBracketExpressionContainsSlash_ShouldMatchItLiterally()
    {
        var pattern = ConfigurationPattern.Compile("ab[e/]cd.i");

        Assert.IsTrue(pattern.IsMatch("ab[e/]cd.i"));
        Assert.IsFalse(pattern.IsMatch("ab/cd.i"));
        Assert.IsFalse(pattern.IsMatch("abecd.i"));
    }

    /// <summary>
    /// Verifies that a negated bracket expression containing <c>/</c> is literal text too, so it matches only the name
    /// written with its brackets and not a name holding some other character in its place.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenNegatedBracketExpressionContainsSlash_ShouldMatchItLiterally()
    {
        var pattern = ConfigurationPattern.Compile("ab[!e/]cd.i");

        Assert.IsTrue(pattern.IsMatch("ab[!e/]cd.i"));
        Assert.IsFalse(pattern.IsMatch("abxcd.i"));
    }

    /// <summary>
    /// Verifies that a bracket expression without <c>/</c> beside a literal one stays a character class.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenOnlyOneBracketExpressionContainsSlash_ShouldKeepTheOtherAsAClass()
    {
        var pattern = ConfigurationPattern.Compile("[ab]x[c/]");

        Assert.IsTrue(pattern.IsMatch("ax[c/]"));
        Assert.IsTrue(pattern.IsMatch("bx[c/]"));
        Assert.IsFalse(pattern.IsMatch("ax/"));
    }
}
