// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.IReadOnlyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that the count reflects the distinct tags stored.
    /// </summary>
    [TestMethod]
    public void Count_WhenBuilt_ShouldReflectDistinctTags()
    {
        Assert.AreEqual(2, CreateSample().Count);
        Assert.AreEqual(0, MapiPropertyCollection.Empty.Count);
    }

    /// <summary>
    /// Verifies that enumeration preserves first-occurrence order even when a later duplicate replaced a value.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDuplicateReplacedValue_ShouldPreserveFirstOccurrenceOrder()
    {
        var subject = new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode);
        var codepage = new MapiPropertyTag(MapiPropertyIds.MessageCodepage, MapiPropertyType.Int32);
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(subject, "First"),
            new MapiProperty(codepage, 1252),
            new MapiProperty(subject, "Second"),
        });

        MapiProperty[] items = collection.ToArray();

        Assert.AreEqual(2, items.Length);
        Assert.AreEqual(subject, items[0].Tag);
        Assert.AreEqual("Second", items[0].Value);
        Assert.AreEqual(codepage, items[1].Tag);
    }

    /// <summary>
    /// Verifies that the non-generic enumerator yields the same items as the generic one.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughIEnumerable_ShouldYieldSameItems()
    {
        MapiPropertyCollection collection = CreateSample();

        var items = new List<object?>();
        foreach (object? item in (IEnumerable)collection)
            items.Add(item);

        Assert.AreEqual(collection.Count, items.Count);
    }
}
