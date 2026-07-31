// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="MapiProperty" />, the decoded tag-and-value pair.
/// </summary>
[TestClass]
public class MapiPropertyTests
{
    /// <summary>
    /// Verifies that the constructor exposes the tag and value verbatim.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTagAndValue_ShouldExposeBoth()
    {
        var tag = new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode);
        var property = new MapiProperty(tag, "Quarterly report");

        Assert.AreEqual(tag, property.Tag);
        Assert.AreEqual("Quarterly report", property.Value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> value is permitted and preserved.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueIsNull_ShouldExposeNull()
    {
        var property = new MapiProperty(new MapiPropertyTag(MapiPropertyIds.AttachData, MapiPropertyType.Object), null);

        Assert.IsNull(property.Value);
    }

    /// <summary>
    /// Verifies that the diagnostic text combines the canonical tag form with the value.
    /// </summary>
    [TestMethod]
    public void ToString_WhenValuePresent_ShouldCombineTagAndValue()
    {
        var property = new MapiProperty(new MapiPropertyTag(0x0037001Fu), "Hello");

        Assert.AreEqual("0x0037001F = Hello", property.ToString());
    }
}
