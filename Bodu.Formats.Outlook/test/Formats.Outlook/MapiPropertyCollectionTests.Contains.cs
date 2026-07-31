// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored tag is reported present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenTagPresent_ShouldReturnTrue()
    {
        MapiPropertyCollection collection = CreateSample();

        Assert.IsTrue(collection.Contains(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.Unicode)));
    }

    /// <summary>
    /// Verifies that lookup is keyed by the full tag — the same identifier with a different type is reported absent.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSameIdDifferentType_ShouldReturnFalse()
    {
        MapiPropertyCollection collection = CreateSample();

        Assert.IsFalse(collection.Contains(new MapiPropertyTag(MapiPropertyIds.Subject, MapiPropertyType.String8)));
    }
}
