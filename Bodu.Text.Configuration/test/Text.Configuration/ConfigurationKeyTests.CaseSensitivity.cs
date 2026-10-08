// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.CaseSensitivity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyOptions.LowercaseKeys" /> lowercases the segments and the path with the
    /// invariant culture while <see cref="ConfigurationKey.RawKey" /> keeps the case the key was written in.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLowercaseKeysIsSet_ShouldLowercaseSegmentsAndPathOnly()
    {
        ConfigurationKeyOptions options = new() { LowercaseKeys = true };

        var key = ConfigurationKey.Parse("Logging.LEVEL", options);

        Assert.AreEqual("Logging.LEVEL", key.RawKey);
        Assert.AreEqual("logging:level", key.Path);
        CollectionAssert.AreEqual(new[] { "logging", "level" }, key.Segments.ToArray());
    }

    /// <summary>
    /// Verifies that the default key options keep the case of every segment.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLowercaseKeysIsUnset_ShouldKeepTheCaseOfSegments()
    {
        var key = ConfigurationKey.Parse("Logging.LEVEL");

        Assert.IsFalse(ConfigurationKeyOptions.Default.LowercaseKeys);
        Assert.AreEqual("Logging:LEVEL", key.Path);
    }
}
