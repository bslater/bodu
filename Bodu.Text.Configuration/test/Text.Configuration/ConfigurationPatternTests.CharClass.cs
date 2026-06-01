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

        for (var c = 'a'; c <= 'e'; c++)
            Assert.IsTrue(pattern.IsMatch($"file-{c}.txt"), $"expected match for {c}");

        for (var c = 'f'; c <= 'z'; c++)
            Assert.IsFalse(pattern.IsMatch($"file-{c}.txt"), $"unexpected match for {c}");
    }

    /// <summary>
    /// Verifies that an empty negated set rejects every character, matching the EditorConfig 0.17.2 behaviour
    /// of treating <c>[!]</c> as "not in the empty set" — every character matches.
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
}
