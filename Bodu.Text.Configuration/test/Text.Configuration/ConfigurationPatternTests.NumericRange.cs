// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.NumericRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Regression-tier sweep: verifies that every integer in a modest range matches and out-of-range integers
    /// do not, for the <c>{n1..n2}</c> form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void IsMatch_WhenNumericRangeIsModest_ShouldMatchEachIntegerInRange()
    {
        var pattern = ConfigurationPattern.Compile("file-{1..10}.txt");

        for (var i = 1; i <= 10; i++)
            Assert.IsTrue(pattern.IsMatch($"file-{i}.txt"), $"expected match for {i}");

        Assert.IsFalse(pattern.IsMatch("file-0.txt"));
        Assert.IsFalse(pattern.IsMatch("file-11.txt"));
        Assert.IsFalse(pattern.IsMatch("file-99.txt"));
    }

    /// <summary>
    /// Verifies that a reversed range (n1 &gt; n2) still matches the canonical inclusive range.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenNumericRangeIsReversed_ShouldMatchSameIntegersAsForwardRange()
    {
        var pattern = ConfigurationPattern.Compile("file-{5..1}.txt");

        for (var i = 1; i <= 5; i++)
            Assert.IsTrue(pattern.IsMatch($"file-{i}.txt"), $"expected match for {i}");

        Assert.IsFalse(pattern.IsMatch("file-0.txt"));
        Assert.IsFalse(pattern.IsMatch("file-6.txt"));
    }
}
