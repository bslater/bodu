// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.Separators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationKeyTests
{
    /// <summary>
    /// Verifies that the default separator set splits on both <c>.</c> and <c>:</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUsingDefaultSeparators_ShouldSplitOnDotAndColon()
    {
        var dotted = ConfigurationKey.Parse("a.b.c");
        var colon = ConfigurationKey.Parse("a:b:c");

        Assert.HasCount(3, dotted.Segments);
        Assert.HasCount(3, colon.Segments);
    }

    /// <summary>
    /// Verifies that a custom separator set splits only on the supplied characters.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeparatorsAreCustom_ShouldSplitOnlyOnSuppliedChars()
    {
        ConfigurationKeyOptions options = new() { SegmentSeparators = ['/'] };
        var key = ConfigurationKey.Parse("a/b/c", options);

        Assert.HasCount(3, key.Segments);
        Assert.AreEqual("a", key.Segments[0]);
    }

    /// <summary>
    /// Verifies that a key whose separator does not appear is treated as a single segment.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyHasNoSeparator_ShouldProduceSingleSegment()
    {
        var key = ConfigurationKey.Parse("standalone");

        Assert.HasCount(1, key.Segments);
        Assert.AreEqual("standalone", key.Segments[0]);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKeyOptions.AllowEmptySegments" /> permits empty segments
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowEmptySegmentsIsTrue_ShouldPreserveEmptySegments()
    {
        ConfigurationKeyOptions options = new() { AllowEmptySegments = true };
        var key = ConfigurationKey.Parse("a..b", options);

        Assert.HasCount(3, key.Segments);
        Assert.AreEqual(string.Empty, key.Segments[1]);
    }

    /// <summary>
    /// Verifies that the whitespace around each segment of a key is trimmed, so a key with whitespace around its
    /// separators or at its ends has the same segments and path as the key written without it.
    /// </summary>
    /// <param name="rawKey">The key as written.</param>
    [TestMethod]
    [DataRow("a . b")]
    [DataRow("a: b")]
    [DataRow(" a.b ")]
    [DataRow("  a  .  b  ")]
    public void Parse_WhenSegmentsHaveSurroundingWhitespace_ShouldTrimEachSegment(string rawKey)
    {
        var key = ConfigurationKey.Parse(rawKey);

        CollectionAssert.AreEqual(new[] { "a", "b" }, key.Segments.ToArray());
        Assert.AreEqual("a:b", key.Path);
        Assert.AreEqual(rawKey, key.RawKey);
    }

    /// <summary>
    /// Verifies that a segment of whitespace alone counts as empty, so the key is rejected while empty segments are not
    /// permitted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenASegmentIsOnlyWhitespace_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ConfigurationKey.Parse("a. .b");
        });
    }

    /// <summary>
    /// Verifies that with <see cref="ConfigurationKeyOptions.AllowEmptySegments" /> set a segment of whitespace alone
    /// is kept as an empty segment.
    /// </summary>
    [TestMethod]
    public void Parse_WhenASegmentIsOnlyWhitespaceAndEmptySegmentsAreAllowed_ShouldKeepAnEmptySegment()
    {
        ConfigurationKeyOptions options = new() { AllowEmptySegments = true };

        var key = ConfigurationKey.Parse("a. .b", options);

        CollectionAssert.AreEqual(new[] { "a", string.Empty, "b" }, key.Segments.ToArray());
        Assert.AreEqual("a::b", key.Path);
    }
}
