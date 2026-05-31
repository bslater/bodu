// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.CaseSensitivity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationKeyTests
{
    /// <summary>
    /// Verifies that the default key options compare keys case-insensitively, matching
    /// <c>Microsoft.Extensions.Configuration</c>.
    /// </summary>
    [TestMethod]
    public void KeyComparer_WhenCaseInsensitive_ShouldEqualOrdinalIgnoreCase()
    {
        ConfigurationKeyOptions options = new();

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, options.KeyComparer);
    }

    /// <summary>
    /// Verifies that setting <see cref="ConfigurationKeyOptions.CaseSensitive" /> to <see langword="true" />
    /// switches comparison to ordinal case-sensitive.
    /// </summary>
    [TestMethod]
    public void KeyComparer_WhenCaseSensitive_ShouldEqualOrdinal()
    {
        ConfigurationKeyOptions options = new() { CaseSensitive = true };

        Assert.AreSame(StringComparer.Ordinal, options.KeyComparer);
    }

    /// <summary>
    /// Verifies that two keys differing only in case compare equal under case-insensitive options.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCaseInsensitiveAndKeysDifferInCase_ShouldBeTrue()
    {
        var upper = ConfigurationKey.Parse("LOGGING.LEVEL");
        var lower = ConfigurationKey.Parse("logging.level");

        Assert.IsTrue(upper.Equals(lower));
        Assert.AreEqual(upper.GetHashCode(), lower.GetHashCode());
    }

    /// <summary>
    /// Verifies that two keys differing only in case compare unequal under case-sensitive options.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCaseSensitiveAndKeysDifferInCase_ShouldBeFalse()
    {
        ConfigurationKeyOptions options = new() { CaseSensitive = true };
        var upper = ConfigurationKey.Parse("LOGGING.LEVEL", options);
        var lower = ConfigurationKey.Parse("logging.level", options);

        Assert.IsFalse(upper.Equals(lower));
    }
}
