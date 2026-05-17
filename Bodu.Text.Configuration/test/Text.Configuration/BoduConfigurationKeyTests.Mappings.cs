// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKeyTests.Mappings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationKeyTests
{
    /// <summary>
    /// Verifies that the default <see cref="BoduConfigurationKeyMapping.DotToColon" /> mapping produces a
    /// colon-delimited configuration key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsDotToColon_ShouldEmitColonDelimitedKey()
    {
        BoduConfigurationKeyOptions options = new() { Mapping = BoduConfigurationKeyMapping.DotToColon };
        var key = BoduConfigurationKey.Parse("a.b.c", options);

        Assert.AreEqual("a:b:c", key.ConfigurationKey);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationKeyMapping.Colon" /> mapping preserves a colon-input key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsColonAndInputUsesColon_ShouldEmitColonDelimitedKey()
    {
        BoduConfigurationKeyOptions options = new() { Mapping = BoduConfigurationKeyMapping.Colon };
        var key = BoduConfigurationKey.Parse("a:b:c", options);

        Assert.AreEqual("a:b:c", key.ConfigurationKey);
        Assert.HasCount(3, key.Segments);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationKeyMapping.Identity" /> mapping reconstructs the raw key
    /// using the first configured segment separator.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentity_ShouldReconstructUsingFirstSeparator()
    {
        BoduConfigurationKeyOptions options = new() { Mapping = BoduConfigurationKeyMapping.Identity };
        var key = BoduConfigurationKey.Parse("a.b.c", options);

        Assert.AreEqual("a.b.c", key.ConfigurationKey);
    }
}
