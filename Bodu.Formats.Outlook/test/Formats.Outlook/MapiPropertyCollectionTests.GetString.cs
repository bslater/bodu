// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a code-page string property is found through the String8 probe.
    /// </summary>
    [TestMethod]
    public void GetString_WhenStoredAsString8_ShouldReturnValue()
    {
        MapiPropertyCollection collection = CreateSingle(MapiPropertyIds.Subject, MapiPropertyType.String8, "Ansi subject");

        Assert.AreEqual("Ansi subject", collection.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Verifies that when both string types are present the Unicode value is preferred.
    /// </summary>
    [TestMethod]
    public void GetString_WhenBothStringTypesPresent_ShouldPreferUnicode()
    {
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.String8), "Ansi"),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode), "Unicode"),
        });

        Assert.AreEqual("Unicode", collection.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Verifies that a stored value of a different CLR type returns <see langword="null" /> instead of throwing.
    /// </summary>
    [TestMethod]
    public void GetString_WhenStoredValueIsNotString_ShouldReturnNull()
    {
        MapiPropertyCollection collection = CreateSingle(MapiPropertyIds.Subject, MapiPropertyType.Unicode, 42);

        Assert.IsNull(collection.GetString(MapiPropertyIds.Subject));
    }
}
