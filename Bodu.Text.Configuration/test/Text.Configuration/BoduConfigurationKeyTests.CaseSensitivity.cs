// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKeyTests.CaseSensitivity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationKeyTests
{
    /// <summary>
    /// Verifies that the default key options compare keys case-insensitively, matching
    /// <c>Microsoft.Extensions.Configuration</c>.
    /// </summary>
    [TestMethod]
    public void KeyComparer_WhenCaseInsensitive_ShouldEqualOrdinalIgnoreCase()
    {
        BoduConfigurationKeyOptions options = new();

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, options.KeyComparer);
    }

    /// <summary>
    /// Verifies that setting <see cref="BoduConfigurationKeyOptions.CaseSensitive" /> to <see langword="true" />
    /// switches comparison to ordinal case-sensitive.
    /// </summary>
    [TestMethod]
    public void KeyComparer_WhenCaseSensitive_ShouldEqualOrdinal()
    {
        BoduConfigurationKeyOptions options = new() { CaseSensitive = true };

        Assert.AreSame(StringComparer.Ordinal, options.KeyComparer);
    }

    /// <summary>
    /// Verifies that two keys differing only in case compare equal under case-insensitive options.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCaseInsensitiveAndKeysDifferInCase_ShouldBeTrue()
    {
        BoduConfigurationKey upper = BoduConfigurationKey.Parse("LOGGING.LEVEL");
        BoduConfigurationKey lower = BoduConfigurationKey.Parse("logging.level");

        Assert.IsTrue(upper.Equals(lower));
        Assert.AreEqual(upper.GetHashCode(), lower.GetHashCode());
    }

    /// <summary>
    /// Verifies that two keys differing only in case compare unequal under case-sensitive options.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCaseSensitiveAndKeysDifferInCase_ShouldBeFalse()
    {
        BoduConfigurationKeyOptions options = new() { CaseSensitive = true };
        BoduConfigurationKey upper = BoduConfigurationKey.Parse("LOGGING.LEVEL", options);
        BoduConfigurationKey lower = BoduConfigurationKey.Parse("logging.level", options);

        Assert.IsFalse(upper.Equals(lower));
    }
}
