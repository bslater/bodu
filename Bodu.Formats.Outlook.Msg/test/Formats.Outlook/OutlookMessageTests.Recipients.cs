// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Recipients.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

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

    /// <summary>
    /// Verifies that under the tolerant levels a recipient storage with no <c>__properties_version1.0</c> stream is
    /// omitted, as the <c>.pst</c> reader skips a row it cannot resolve, while the rest of the message reads normally.
    /// </summary>
    /// <param name="level">The tolerant validation level the message is read at.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Minimal)]
    public void Recipients_WhenAStorageHasNoPropertyStream_ShouldOmitIt(CompoundValidationLevel level)
    {
        using MemoryStream container = CreateWithRecipientMissingItsPropertyStream();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = level });

        CollectionAssert.AreEqual(
            new[] { "Alex Doe", "Casey Poe" },
            message.Recipients.Select(static recipient => recipient.DisplayName).ToArray());
        Assert.AreEqual("Test subject", message.Subject);
        Assert.AreEqual("Test body", message.BodyText);
        Assert.AreEqual(1, message.Attachments.Count);
    }

    /// <summary>
    /// Verifies that under <see cref="CompoundValidationLevel.Strict" /> a recipient storage with no
    /// <c>__properties_version1.0</c> stream throws <see cref="OutlookMsgFormatException" /> when the recipients are read.
    /// </summary>
    [TestMethod]
    public void Recipients_WhenAStorageHasNoPropertyStream_ForStrict_ShouldThrowOutlookMsgFormatException()
    {
        using MemoryStream container = CreateWithRecipientMissingItsPropertyStream();
        using var message = OutlookMessage.Open(
            container, new OutlookMessageReaderOptions { ValidationLevel = CompoundValidationLevel.Strict });

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = message.Recipients;
        });
    }

    /// <summary>
    /// Builds a minimal message with three recipient storages, the second without a property stream, and one attachment.
    /// </summary>
    /// <returns>The container stream.</returns>
    private static MemoryStream CreateWithRecipientMissingItsPropertyStream() =>
        MsgFixtureBuilder.CreateMinimal()
            .AddRecipient(static recipient => recipient.AddUnicode(MapiPropertyIds.DisplayName, "Alex Doe"))
            .AddRecipient(static recipient => recipient.OmitPropertiesStream())
            .AddRecipient(static recipient => recipient.AddUnicode(MapiPropertyIds.DisplayName, "Casey Poe"))
            .AddAttachment(static attachment => attachment.AddUnicode(MapiPropertyIds.AttachLongFilename, "kept.bin"))
            .Build();
}
