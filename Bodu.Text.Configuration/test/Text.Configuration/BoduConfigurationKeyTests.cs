// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKeyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationKey" /> covering parsing, segment splitting, and mapping.
/// </summary>
[TestClass]
public partial class BoduConfigurationKeyTests
{
    /// <summary>
    /// Verifies that dotted keys map to colon-delimited configuration keys under the default options.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDottedKey_ShouldMapToColon()
    {
        BoduConfigurationKey key = BoduConfigurationKey.Parse("logging.level.default");

        Assert.AreEqual("logging.level.default", key.RawKey);
        Assert.AreEqual("logging:level:default", key.ConfigurationKey);
        Assert.AreEqual(3, key.Segments.Length);
    }

    /// <summary>
    /// Verifies that a single-segment key produces the same value for both raw and configuration forms.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleSegment_ShouldRoundTripUnchanged()
    {
        BoduConfigurationKey key = BoduConfigurationKey.Parse("root");

        Assert.AreEqual("root", key.RawKey);
        Assert.AreEqual("root", key.ConfigurationKey);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationKey.TryParse(string?, out BoduConfigurationKey)" /> reports
    /// failure on null or whitespace input without throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNullOrWhitespace_ShouldReturnFalse()
    {
        Assert.IsFalse(BoduConfigurationKey.TryParse(null, out BoduConfigurationKey result));
        Assert.AreEqual(string.Empty, result.RawKey);

        Assert.IsFalse(BoduConfigurationKey.TryParse(string.Empty, out result));
        Assert.IsFalse(BoduConfigurationKey.TryParse("   ", out result));
    }

    /// <summary>
    /// Verifies that keys compare case-insensitively by default and case-sensitively when configured.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCaseSensitivityVaries_ShouldRespectOptions()
    {
        BoduConfigurationKey insensitiveA = BoduConfigurationKey.Parse("Logging.Level");
        BoduConfigurationKey insensitiveB = BoduConfigurationKey.Parse("logging.level");
        Assert.IsTrue(insensitiveA.Equals(insensitiveB));

        BoduConfigurationKeyOptions sensitive = new() { CaseSensitive = true };
        BoduConfigurationKey sensitiveA = BoduConfigurationKey.Parse("Logging.Level", sensitive);
        BoduConfigurationKey sensitiveB = BoduConfigurationKey.Parse("logging.level", sensitive);
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
            _ = BoduConfigurationKey.Parse("a..b");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationKeyMapping.Identity" /> emits the raw key unchanged.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentity_ShouldEmitRawKeyUnchanged()
    {
        BoduConfigurationKeyOptions options = new() { Mapping = BoduConfigurationKeyMapping.Identity };
        BoduConfigurationKey key = BoduConfigurationKey.Parse("logging.level.default", options);

        Assert.AreEqual("logging.level.default", key.ConfigurationKey);
    }
}
