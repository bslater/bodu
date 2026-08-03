// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="MapiPropertyCollection" />, the tag-addressed property collection.
/// </summary>
[TestClass]
public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a collection built from decoded properties surfaces a string value through the typed accessor —
    /// the primary happy path of the shared model.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void GetString_WhenSubjectPresent_ShouldReturnStoredValue()
    {
        MapiPropertyCollection collection = CreateSample();

        Assert.AreEqual("Quarterly report", collection.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Builds the sample collection shared by the member tests.
    /// </summary>
    /// <returns>A collection carrying a Unicode subject and an Int32 code page.</returns>
    internal static MapiPropertyCollection CreateSample() =>
        new(new[]
        {
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode), "Quarterly report"),
            new MapiProperty(new MapiPropertyTag(MapiPropertyIds.MessageCodepage, MapiPropertyType.Int32), 1252),
        });

    /// <summary>
    /// Builds a single-property collection for a typed-accessor scenario.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="type">The property type.</param>
    /// <param name="value">The stored value.</param>
    /// <returns>A collection carrying exactly the given property.</returns>
    internal static MapiPropertyCollection CreateSingle(ushort id, MapiPropertyType type, object? value) =>
        new(new[] { new MapiProperty(new MapiPropertyTag(id, type), value) });
}
