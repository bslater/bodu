// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.LiteralCharacters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Pins the literal-character behaviour of <see cref="ConfigurationKey" /> parsing. The key splitter does not
/// currently support backslash escapes for separator characters — a <c>\.</c> sequence is treated as the two
/// characters <c>\</c> and <c>.</c>, and the <c>.</c> still separates segments. These tests document that
/// contract so any future addition of escape support is a deliberate, observable change.
/// </summary>
public partial class ConfigurationKeyTests
{
    /// <summary>
    /// Verifies that a backslash before a <c>.</c> does <em>not</em> escape the separator. The key splits
    /// into segments, with the backslash retained at the end of the first segment.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBackslashBeforeDotSeparator_ShouldNotEscapeSeparator()
    {
        ConfigurationKey key = ConfigurationKey.Parse(@"service\.name");

        Assert.AreEqual(2, key.Segments.Length);
        Assert.AreEqual(@"service\", key.Segments[0]);
        Assert.AreEqual("name", key.Segments[1]);
    }

    /// <summary>
    /// Verifies that a backslash before a <c>:</c> does <em>not</em> escape the separator.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBackslashBeforeColonSeparator_ShouldNotEscapeSeparator()
    {
        ConfigurationKey key = ConfigurationKey.Parse(@"service\:name");

        Assert.AreEqual(2, key.Segments.Length);
        Assert.AreEqual(@"service\", key.Segments[0]);
        Assert.AreEqual("name", key.Segments[1]);
    }

    /// <summary>
    /// Verifies that the canonical <see cref="ConfigurationKey.Path" /> uses <c>:</c> as the segment
    /// delimiter regardless of which separator was used in the raw key. This is the projection used by the
    /// Microsoft.Extensions.Configuration bridge.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDottedKey_ShouldProduceColonDelimitedPath()
    {
        ConfigurationKey key = ConfigurationKey.Parse("logging.level.default");

        Assert.AreEqual("logging:level:default", key.Path);
    }

    /// <summary>
    /// Verifies that an already-colon-delimited key parses to the same canonical path as the dotted form.
    /// </summary>
    [TestMethod]
    public void Parse_WhenColonDelimitedKey_ShouldProduceColonDelimitedPath()
    {
        ConfigurationKey key = ConfigurationKey.Parse("logging:level:default");

        Assert.AreEqual("logging:level:default", key.Path);
    }

    /// <summary>
    /// Verifies that mixing <c>.</c> and <c>:</c> in the same raw key produces a single, canonical
    /// colon-delimited path — both separators are recognized.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMixedSeparators_ShouldProduceSingleCanonicalPath()
    {
        ConfigurationKey key = ConfigurationKey.Parse("logging.level:default");

        Assert.AreEqual("logging:level:default", key.Path);
        Assert.AreEqual(3, key.Segments.Length);
    }

    /// <summary>
    /// Verifies that numeric-looking segments are preserved verbatim, so that array indices used by
    /// Microsoft.Extensions.Configuration bind correctly (e.g. <c>items.0 = first</c> → key
    /// <c>items:0</c>).
    /// </summary>
    [TestMethod]
    public void Parse_WhenSegmentIsPurelyNumeric_ShouldPreserveDigitsAsSegment()
    {
        ConfigurationKey key = ConfigurationKey.Parse("items.0");

        Assert.AreEqual("items:0", key.Path);
        Assert.AreEqual(2, key.Segments.Length);
        Assert.AreEqual("0", key.Segments[1]);
    }

    /// <summary>
    /// Verifies that constraining the separator set to <c>.</c> only causes a <c>:</c> to be treated as part
    /// of a segment rather than as a separator. This is the option to reach for when authors want to write
    /// literal colons.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeparatorsLimitedToDot_ShouldKeepColonAsLiteralCharacter()
    {
        ConfigurationKeyOptions options = new() { SegmentSeparators = ['.'] };

        ConfigurationKey key = ConfigurationKey.Parse("service:name.value", options);

        Assert.AreEqual(2, key.Segments.Length);
        Assert.AreEqual("service:name", key.Segments[0]);
        Assert.AreEqual("value", key.Segments[1]);
    }
}
