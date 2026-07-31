// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetDouble.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored 64-bit floating-point value is returned.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenStoredAsDouble_ShouldReturnValue()
    {
        MapiPropertyCollection collection = CreateSingle(0x1234, MapiPropertyType.Double, 3.5d);

        Assert.AreEqual(3.5d, collection.GetDouble(0x1234));
    }

    /// <summary>
    /// Verifies that a stored 32-bit floating-point value is found through the Float probe and widened.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenStoredAsFloat_ShouldWidenValue()
    {
        MapiPropertyCollection collection = CreateSingle(0x1234, MapiPropertyType.Float, 1.5f);

        Assert.AreEqual(1.5d, collection.GetDouble(0x1234));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetDouble(0x1234));
    }
}
