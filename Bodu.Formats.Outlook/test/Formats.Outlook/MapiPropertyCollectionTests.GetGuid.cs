// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetGuid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored GUID is returned.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenPresent_ShouldReturnValue()
    {
        var guid = new Guid("00020329-0000-0000-C000-000000000046");
        MapiPropertyCollection collection = CreateSingle(0x0FF9, MapiPropertyType.Guid, guid);

        Assert.AreEqual(guid, collection.GetGuid(0x0FF9));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetGuid(0x0FF9));
    }
}
