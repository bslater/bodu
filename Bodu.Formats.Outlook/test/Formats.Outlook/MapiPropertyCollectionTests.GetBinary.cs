// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetBinary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored binary payload is returned intact.
    /// </summary>
    [TestMethod]
    public void GetBinary_WhenPresent_ShouldReturnPayload()
    {
        byte[] payload = { 0x01, 0x02, 0x03 };
        MapiPropertyCollection collection = CreateSingle(MapiPropertyIds.AttachData, MapiPropertyType.Binary, payload);

        ReadOnlyMemory<byte>? result = collection.GetBinary(MapiPropertyIds.AttachData);

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(payload, result.Value.ToArray());
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBinary_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetBinary(MapiPropertyIds.AttachData));
    }
}
