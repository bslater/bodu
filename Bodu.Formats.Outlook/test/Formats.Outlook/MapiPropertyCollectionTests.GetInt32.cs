// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetInt32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored 32-bit integer is returned.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenPresent_ShouldReturnValue()
    {
        MapiPropertyCollection collection = CreateSample();

        Assert.AreEqual(1252, collection.GetInt32(MapiPropertyIds.MessageCodepage));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetInt32(MapiPropertyIds.MessageCodepage));
    }

    /// <summary>
    /// Verifies that a stored value of a different CLR type returns <see langword="null" /> instead of throwing.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenStoredValueIsNotInt32_ShouldReturnNull()
    {
        MapiPropertyCollection collection = CreateSingle(MapiPropertyIds.MessageCodepage, MapiPropertyType.Int32, "1252");

        Assert.IsNull(collection.GetInt32(MapiPropertyIds.MessageCodepage));
    }
}
