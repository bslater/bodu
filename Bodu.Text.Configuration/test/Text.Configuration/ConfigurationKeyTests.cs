// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationKey" /> covering parsing, segment splitting, and mapping.
/// </summary>
[TestClass]
public partial class ConfigurationKeyTests
{
    /// <summary>
    /// Verifies that dotted keys map to colon-delimited configuration keys under the default options.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDottedKey_ShouldMapToColon()
    {
        var key = ConfigurationKey.Parse("logging.level.default");

        Assert.AreEqual("logging.level.default", key.RawKey);
        Assert.AreEqual("logging:level:default", key.Path);
        Assert.HasCount(3, key.Segments);
    }

    /// <summary>
    /// Verifies that a single-segment key produces the same value for both raw and configuration forms.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleSegment_ShouldRoundTripUnchanged()
    {
        var key = ConfigurationKey.Parse("root");

        Assert.AreEqual("root", key.RawKey);
        Assert.AreEqual("root", key.Path);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKey.TryParse(string?, out ConfigurationKey)" /> reports
    /// failure on null or whitespace input without throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNullOrWhitespace_ShouldReturnFalse()
    {
        Assert.IsFalse(ConfigurationKey.TryParse(null, out ConfigurationKey result));
        Assert.AreEqual(string.Empty, result.RawKey);

        Assert.IsFalse(ConfigurationKey.TryParse(string.Empty, out result));
        Assert.IsFalse(ConfigurationKey.TryParse("   ", out result));
    }

    /// <summary>
    /// Verifies that keys compare case-insensitively by default and case-sensitively when configured.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCaseSensitivityVaries_ShouldRespectOptions()
    {
        var insensitiveA = ConfigurationKey.Parse("Logging.Level");
        var insensitiveB = ConfigurationKey.Parse("logging.level");
        Assert.IsTrue(insensitiveA.Equals(insensitiveB));

        ConfigurationKeyOptions sensitive = new() { CaseSensitive = true };
        var sensitiveA = ConfigurationKey.Parse("Logging.Level", sensitive);
        var sensitiveB = ConfigurationKey.Parse("logging.level", sensitive);
        Assert.IsFalse(sensitiveA.Equals(sensitiveB));
    }

    /// <summary>
    /// Verifies that an empty segment in the raw key is rejected by default.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEmptySegment_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ConfigurationKey.Parse("a..b");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> emits the raw key unchanged.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentity_ShouldEmitRawKeyUnchanged()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };
        var key = ConfigurationKey.Parse("logging.level.default", options);

        Assert.AreEqual("logging.level.default", key.Path);
    }
}
