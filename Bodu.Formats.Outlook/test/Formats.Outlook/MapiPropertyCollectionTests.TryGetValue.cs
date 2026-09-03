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

    /// <summary>
    /// Verifies that the by-identifier lookup returns the first property stored under an identifier regardless of
    /// its wire type, and reports a miss for an absent identifier.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenIdentifierGivenWithoutType_ShouldReturnFirstMatch()
    {
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(0x0E08, MapiPropertyType.Int64), 10L),
            new MapiProperty(new MapiPropertyTag(0x0E08, MapiPropertyType.Int32), 20),
        });

        Assert.IsTrue(collection.TryGetValue((ushort)0x0E08, out MapiProperty? property));
        Assert.AreEqual(MapiPropertyType.Int64, property.Tag.Type);
        Assert.AreEqual(10L, property.Value);
        Assert.IsFalse(collection.TryGetValue((ushort)0x0E09, out _));
    }
}
