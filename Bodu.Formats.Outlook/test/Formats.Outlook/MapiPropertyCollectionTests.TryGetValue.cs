// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored property is retrieved with its tag and value intact.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenTagPresent_ShouldReturnProperty()
    {
        MapiPropertyCollection collection = CreateSample();
        var tag = new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode);

        Assert.IsTrue(collection.TryGetValue(tag, out MapiProperty? property));
        Assert.AreEqual(tag, property.Tag);
        Assert.AreEqual("Quarterly report", property.Value);
    }

    /// <summary>
    /// Verifies that an absent tag reports <see langword="false" /> with a <see langword="null" /> property.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenTagAbsent_ShouldReturnFalse()
    {
        MapiPropertyCollection collection = CreateSample();

        Assert.IsFalse(collection.TryGetValue(new MapiPropertyTag(MapiPropertyIds.Body, MapiPropertyType.Unicode), out MapiProperty? property));
        Assert.IsNull(property);
    }
}
