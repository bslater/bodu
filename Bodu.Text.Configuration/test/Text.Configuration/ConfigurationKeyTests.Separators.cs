// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.Separators.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
}
