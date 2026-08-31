// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Recipients.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that the recipient-table rows decode into shared recipient views in table order, with each cell
    /// surfacing through the typed conveniences.
    /// </summary>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    public void Recipients_WhenSyntheticStore_ShouldDecodeRecipientRows(string testName, PstValidationLevel level)
    {
        _ = testName;

        using OutlookMailStore store = OpenSynthetic(level: level);

        IReadOnlyList<OutlookRecipient> recipients = GetFullMessage(store).Recipients;

        Assert.AreEqual(2, recipients.Count);

        Assert.AreEqual(OutlookRecipientType.To, recipients[0].RecipientType);
        Assert.AreEqual(PstMessagingFixtureBuilder.RecipientOneName, recipients[0].DisplayName);
        Assert.AreEqual(PstMessagingFixtureBuilder.RecipientOneEmail, recipients[0].EmailAddress);
        Assert.AreEqual(PstMessagingFixtureBuilder.RecipientOneAddressType, recipients[0].AddressType);

        Assert.AreEqual(OutlookRecipientType.Cc, recipients[1].RecipientType);
        Assert.AreEqual(PstMessagingFixtureBuilder.RecipientTwoName, recipients[1].DisplayName);
        Assert.AreEqual(PstMessagingFixtureBuilder.RecipientTwoEmail, recipients[1].EmailAddress);
        Assert.IsNull(recipients[1].AddressType, "The second row's address-type cell is marked absent.");
    }

    /// <summary>
    /// Verifies that a message without a recipient-table subnode reports an empty recipient list, and that the list
    /// is materialized once.
    /// </summary>
    [TestMethod]
    public void Recipients_WhenMessageHasNoRecipientTable_ShouldBeEmpty()
    {
        using OutlookMailStore store = OpenSynthetic();

        OutlookMailMessage message = GetPlainMessage(store);

        Assert.AreEqual(0, message.Recipients.Count);
        Assert.AreSame(message.Recipients, message.Recipients, "The recipient list must be built once and cached.");
    }

    /// <summary>
    /// Verifies that every message of the reference corpus decodes its recipients, and that at least one message
    /// carries a recipient with an addressable identity.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Recipients_WhenReferenceFixture_ShouldDecodeRecipientTables()
    {
        using OutlookMailStore store = OutlookMailStoreTests.OpenSample1(PstValidationLevel.Strict);

        var recipients = OutlookMailStoreTests.Walk(store.RootFolder)
            .SelectMany(static f => f.EnumerateMessages())
            .SelectMany(static m => m.Recipients)
            .ToList();

        Assert.IsTrue(recipients.Count > 0, "The corpus carries at least one message with a recipient table.");
        Assert.IsTrue(
            recipients.Any(static r => r.DisplayName is not null || r.EmailAddress is not null),
            "At least one recipient must carry a display name or email address.");
    }
}
