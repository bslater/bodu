// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.Mappings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> mapping returns a dotted key as written rather
    /// than splitting it and rejoining the segments.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentity_ShouldReturnTheDottedKeyAsWritten()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };
        var key = ConfigurationKey.Parse("a.b.c", options);

        Assert.AreEqual("a.b.c", key.Path);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> keeps a key exactly as written, as its path and as
    /// its one segment, whether the key holds a colon, a dot, both, or neither.
    /// </summary>
    /// <param name="rawKey">The key as written.</param>
    [TestMethod]
    [DataRow("a:b")]
    [DataRow("a.b")]
    [DataRow("a.b:c")]
    [DataRow("ab")]
    public void Parse_WhenMappingIsIdentity_ShouldKeepTheKeyAsWritten(string rawKey)
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };

        var key = ConfigurationKey.Parse(rawKey, options);

        Assert.AreEqual(rawKey, key.Path);
        CollectionAssert.AreEqual(new[] { rawKey }, key.Segments.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> keeps the dots of a key when the colon is the
    /// first configured separator, rather than rejoining the key on the colon.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentityAndColonIsTheFirstSeparator_ShouldKeepTheDots()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity, SegmentSeparators = [':', '.'] };

        var key = ConfigurationKey.Parse("a.b:c", options);

        Assert.AreEqual("a.b:c", key.Path);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" />, which does not split a key, keeps a key whose
    /// separators would leave an empty segment under the other mappings.
    /// </summary>
    /// <param name="rawKey">The key as written.</param>
    [TestMethod]
    [DataRow(".a")]
    [DataRow("a..b")]
    [DataRow("a:")]
    public void Parse_WhenMappingIsIdentityAndKeyWouldHaveAnEmptySegment_ShouldKeepTheKey(string rawKey)
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };

        var key = ConfigurationKey.Parse(rawKey, options);

        Assert.AreEqual(rawKey, key.Path);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> with
    /// <see cref="ConfigurationKeyOptions.LowercaseKeys" /> set changes nothing in a key but its case, its dots and
    /// colons included, while the raw key keeps its case.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentityAndLowercaseKeysIsSet_ShouldOnlyLowercaseTheKey()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity, LowercaseKeys = true };

        var key = ConfigurationKey.Parse("Dotnet.CA1000:Severity", options);

        Assert.AreEqual("dotnet.ca1000:severity", key.Path);
        Assert.AreEqual("Dotnet.CA1000:Severity", key.RawKey);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" />, which does not split a key, keeps the whitespace
    /// inside it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentityAndKeyHasInternalWhitespace_ShouldKeepIt()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };

        var key = ConfigurationKey.Parse("a . b", options);

        Assert.AreEqual("a . b", key.Path);
        CollectionAssert.AreEqual(new[] { "a . b" }, key.Segments.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyMapping.Identity" /> trims only the ends of a key, as the reader trims
    /// a key, and keeps the whitespace inside it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMappingIsIdentityAndKeyHasSurroundingWhitespace_ShouldTrimOnlyItsEnds()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };

        var key = ConfigurationKey.Parse(" a . b ", options);

        Assert.AreEqual("a . b", key.Path);
        Assert.AreEqual(" a . b ", key.RawKey);
    }
}
