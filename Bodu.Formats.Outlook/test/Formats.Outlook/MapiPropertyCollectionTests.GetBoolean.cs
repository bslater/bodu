// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetBoolean.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored Boolean is returned.
    /// </summary>
    /// <param name="value">The stored Boolean value.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetBoolean_WhenPresent_ShouldReturnValue(bool value)
    {
        MapiPropertyCollection collection = CreateSingle(0x0E1F, MapiPropertyType.Boolean, value);

        Assert.AreEqual(value, collection.GetBoolean(0x0E1F));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBoolean_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetBoolean(0x0E1F));
    }
}
