// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollectionTests.GetDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiPropertyCollectionTests
{
    /// <summary>
    /// Verifies that a stored time stamp is returned.
    /// </summary>
    [TestMethod]
    public void GetDateTime_WhenPresent_ShouldReturnValue()
    {
        var sent = new DateTimeOffset(2026, 7, 31, 9, 30, 0, TimeSpan.Zero);
        MapiPropertyCollection collection = CreateSingle(MapiPropertyIds.ClientSubmitTime, MapiPropertyType.SystemTime, sent);

        Assert.AreEqual(sent, collection.GetDateTime(MapiPropertyIds.ClientSubmitTime));
    }

    /// <summary>
    /// Verifies that an absent identifier returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetDateTime_WhenAbsent_ShouldReturnNull()
    {
        Assert.IsNull(MapiPropertyCollection.Empty.GetDateTime(MapiPropertyIds.ClientSubmitTime));
    }
}
