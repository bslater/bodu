// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Subject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that a stored subject carrying the MS-PST prefix marker surfaces without it, while the raw stored
    /// value remains reachable through the property collection.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Subject_WhenStoredWithPrefixMarker_ShouldReturnNormalizedSubject()
    {
        using OutlookMailStore store = OpenSynthetic();

        OutlookMailMessage message = GetFullMessage(store);

        Assert.AreEqual(PstMessagingFixtureBuilder.NormalizedSubject, message.Subject);
        Assert.AreEqual(
            PstMessagingFixtureBuilder.StoredSubject,
            message.Properties.GetString(MapiPropertyIds.Subject),
            "The property collection must surface the stored value unmodified.");
    }

    /// <summary>
    /// Verifies that a stored subject without the prefix marker surfaces unchanged.
    /// </summary>
    [TestMethod]
    public void Subject_WhenStoredWithoutPrefixMarker_ShouldReturnStoredSubject()
    {
        using OutlookMailStore store = OpenSynthetic();

        Assert.AreEqual(PstMessagingFixtureBuilder.PlainSubject, GetPlainMessage(store).Subject);
    }
}
