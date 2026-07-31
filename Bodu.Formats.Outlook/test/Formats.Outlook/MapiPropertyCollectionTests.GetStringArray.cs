// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetStringArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a multi-valued Unicode property is returned through the array accessor.
    /// </summary>
    [TestMethod]
    public void GetStringArray_WhenStoredAsMultiValuedUnicode_ShouldReturnValues()
    {
        string[] keywords = { "red", "green" };
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(0x3A58101Fu), keywords),
        });

        CollectionAssert.AreEqual(keywords, collection.GetStringArray(0x3A58));
    }

    /// <summary>
    /// Verifies that a multi-valued code-page string property is found through the String8 probe.
    /// </summary>
    [TestMethod]
    public void GetStringArray_WhenStoredAsMultiValuedString8_ShouldReturnValues()
    {
        string[] keywords = { "alpha" };
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(0x3A58101Eu), keywords),
        });

        CollectionAssert.AreEqual(keywords, collection.GetStringArray(0x3A58));
    }

    /// <summary>
    /// Verifies that the single-valued string tag does not satisfy the multi-valued accessor.
    /// </summary>
    [TestMethod]
    public void GetStringArray_WhenOnlySingleValuedStringPresent_ShouldReturnNull()
    {
        MapiPropertyCollection collection = CreateSingle(0x3A58, MapiPropertyType.Unicode, "red");

        Assert.IsNull(collection.GetStringArray(0x3A58));
    }
}
