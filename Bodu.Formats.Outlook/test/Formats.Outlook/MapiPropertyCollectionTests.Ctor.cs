// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> property sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertiesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new MapiPropertyCollection(null!);
            },
            "properties");
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> element in the sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenElementIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new MapiPropertyCollection(new MapiProperty[] { null! });
            },
            "properties");
    }

    /// <summary>
    /// Verifies that when the input carries the same tag twice, the last occurrence wins and the count is not
    /// inflated.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDuplicateTag_ShouldKeepLastOccurrence()
    {
        var tag = new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode);
        var collection = new MapiPropertyCollection(new[]
        {
            new MapiProperty(tag, "First"),
            new MapiProperty(tag, "Second"),
        });

        Assert.AreEqual(1, collection.Count);
        Assert.AreEqual("Second", collection.GetString(MapiPropertyIds.Subject));
    }
}
