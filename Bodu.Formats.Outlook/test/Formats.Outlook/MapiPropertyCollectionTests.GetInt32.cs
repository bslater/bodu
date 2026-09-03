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

    /// <summary>
    /// Verifies that an integer a writer stored under the 16-bit type is widened by the 32-bit accessor: real-world
    /// writers store <c>PidTagAttachMethod</c> and its peers as <c>PT_SHORT</c>, and the accessor documents probing
    /// the plausible wire types.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenStoredAsInt16_ShouldWiden()
    {
        MapiPropertyCollection collection = CreateSingle(MapiPropertyIds.AttachMethod, MapiPropertyType.Int16, (short)5);

        Assert.AreEqual(5, collection.GetInt32(MapiPropertyIds.AttachMethod));
    }
}
