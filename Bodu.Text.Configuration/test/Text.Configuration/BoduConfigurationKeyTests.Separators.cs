// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKeyTests.Separators.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationKeyTests
{
    /// <summary>
    /// Verifies that the default separator set splits on both <c>.</c> and <c>:</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUsingDefaultSeparators_ShouldSplitOnDotAndColon()
    {
        BoduConfigurationKey dotted = BoduConfigurationKey.Parse("a.b.c");
        BoduConfigurationKey colon = BoduConfigurationKey.Parse("a:b:c");

        Assert.AreEqual(3, dotted.Segments.Length);
        Assert.AreEqual(3, colon.Segments.Length);
    }

    /// <summary>
    /// Verifies that a custom separator set splits only on the supplied characters.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeparatorsAreCustom_ShouldSplitOnlyOnSuppliedChars()
    {
        BoduConfigurationKeyOptions options = new() { SegmentSeparators = ['/'] };
        BoduConfigurationKey key = BoduConfigurationKey.Parse("a/b/c", options);

        Assert.AreEqual(3, key.Segments.Length);
        Assert.AreEqual("a", key.Segments[0]);
    }

    /// <summary>
    /// Verifies that a key whose separator does not appear is treated as a single segment.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyHasNoSeparator_ShouldProduceSingleSegment()
    {
        BoduConfigurationKey key = BoduConfigurationKey.Parse("standalone");

        Assert.AreEqual(1, key.Segments.Length);
        Assert.AreEqual("standalone", key.Segments[0]);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationKeyOptions.AllowEmptySegments" /> permits empty segments
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowEmptySegmentsIsTrue_ShouldPreserveEmptySegments()
    {
        BoduConfigurationKeyOptions options = new() { AllowEmptySegments = true };
        BoduConfigurationKey key = BoduConfigurationKey.Parse("a..b", options);

        Assert.AreEqual(3, key.Segments.Length);
        Assert.AreEqual(string.Empty, key.Segments[1]);
    }
}
