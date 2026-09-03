// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetAll.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that every property stored under an identifier is enumerated in first-occurrence order, one per
    /// wire type, and that an absent identifier enumerates nothing.
    /// </summary>
    [TestMethod]
    public void GetAll_WhenIdentifierHasMultipleTypes_ShouldReturnAllInOrder()
    {
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(0x0E08, MapiPropertyType.Int64), 10L),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode), "s"),
            new MapiProperty(new MapiPropertyTag(0x0E08, MapiPropertyType.Int32), 20),
        });

        MapiProperty[] all = collection.GetAll(0x0E08).ToArray();

        Assert.AreEqual(2, all.Length);
        Assert.AreEqual(MapiPropertyType.Int64, all[0].Tag.Type);
        Assert.AreEqual(MapiPropertyType.Int32, all[1].Tag.Type);
        Assert.AreEqual(0, collection.GetAll(0x0E09).Count());
    }
}
