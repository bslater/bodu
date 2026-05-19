// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.Mappings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationKeyTests
{
    /// <summary>
    /// Verifies that the default <see cref="ConfigurationKeyMapping.DotToColon" /> mapping produces a
    /// colon-delimited configuration key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsDotToColon_ShouldEmitColonDelimitedKey()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.DotToColon };
        var key = ConfigurationKey.Parse("a.b.c", options);

        Assert.AreEqual("a:b:c", key.Path);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Colon" /> mapping preserves a colon-input key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsColonAndInputUsesColon_ShouldEmitColonDelimitedKey()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Colon };
        var key = ConfigurationKey.Parse("a:b:c", options);

        Assert.AreEqual("a:b:c", key.Path);
        Assert.HasCount(3, key.Segments);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> mapping reconstructs the raw key
    /// using the first configured segment separator.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentity_ShouldReconstructUsingFirstSeparator()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };
        var key = ConfigurationKey.Parse("a.b.c", options);

        Assert.AreEqual("a.b.c", key.Path);
    }
}
