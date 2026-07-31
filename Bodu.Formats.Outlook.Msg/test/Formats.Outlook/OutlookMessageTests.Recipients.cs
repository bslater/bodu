// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Recipients.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that recipients surface in index order with their typed conveniences.
    /// </summary>
    [TestMethod]
    public void Recipients_WhenPresent_ShouldSurfaceInIndexOrder()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddRecipient(recipient => recipient
                .AddUnicode(MapiPropertyIds.DisplayName, "Alex Doe")
                .AddUnicode(MapiPropertyIds.EmailAddress, "alex@example.com")
                .AddUnicode(MapiPropertyIds.AddressType, "SMTP")
                .AddFixedEntry(0x0C150003, (ulong)(int)OutlookRecipientType.To))
            .AddRecipient(recipient => recipient
                .AddUnicode(MapiPropertyIds.DisplayName, "Blair Roe")
                .AddFixedEntry(0x0C150003, (ulong)(int)OutlookRecipientType.Cc))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(2, message.Recipients.Count);
        Assert.AreEqual("Alex Doe", message.Recipients[0].DisplayName);
        Assert.AreEqual("alex@example.com", message.Recipients[0].EmailAddress);
        Assert.AreEqual("SMTP", message.Recipients[0].AddressType);
        Assert.AreEqual(OutlookRecipientType.To, message.Recipients[0].RecipientType);
        Assert.AreEqual(OutlookRecipientType.Cc, message.Recipients[1].RecipientType);
    }

    /// <summary>
    /// Verifies that a message without recipients surfaces an empty list.
    /// </summary>
    [TestMethod]
    public void Recipients_WhenAbsent_ShouldReturnEmptyList()
    {
        using MemoryStream container = CreateMinimalContainer();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(0, message.Recipients.Count);
    }

    /// <summary>
    /// Verifies that the recipient list is materialized once and reused.
    /// </summary>
    [TestMethod]
    public void Recipients_WhenAccessedTwice_ShouldReturnSameInstance()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddRecipient(recipient => recipient.AddUnicode(MapiPropertyIds.DisplayName, "Alex"))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreSame(message.Recipients, message.Recipients);
    }
}
