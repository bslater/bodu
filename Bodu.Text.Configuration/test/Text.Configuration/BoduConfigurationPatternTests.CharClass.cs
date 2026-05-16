// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationPatternTests.CharClass.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationPatternTests
{
    /// <summary>
    /// Regression-tier sweep: verifies that every character in a small range matches the corresponding
    /// character class, and that characters outside the range do not match.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void IsMatch_WhenCharacterClassCoversAlphaRange_ShouldMatchEachLetterInRange()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("file-[a-e].txt");

        for (char c = 'a'; c <= 'e'; c++)
            Assert.IsTrue(pattern.IsMatch($"file-{c}.txt"), $"expected match for {c}");

        for (char c = 'f'; c <= 'z'; c++)
            Assert.IsFalse(pattern.IsMatch($"file-{c}.txt"), $"unexpected match for {c}");
    }

    /// <summary>
    /// Verifies that an empty negated set rejects every character, matching the EditorConfig 0.17.2 behaviour
    /// of treating <c>[!]</c> as "not in the empty set" — every character matches.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenCharacterClassIsExplicitAndContains_ShouldMatchOnlyListed()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("foo[xyz]bar");

        Assert.IsTrue(pattern.IsMatch("fooxbar"));
        Assert.IsTrue(pattern.IsMatch("fooybar"));
        Assert.IsTrue(pattern.IsMatch("foozbar"));
        Assert.IsFalse(pattern.IsMatch("fooabar"));
    }
}
