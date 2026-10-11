// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Attachments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that attachments surface in index order with their typed conveniences.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenPresent_ShouldSurfaceInIndexOrder()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddFixedEntry(0x37050003, 1)
                .AddUnicode(MapiPropertyIds.AttachLongFilename, "report-final.pdf")
                .AddUnicode(MapiPropertyIds.AttachFilename, "REPORT~1.PDF")
                .AddUnicode(MapiPropertyIds.AttachContentId, "cid:report")
                .AddFixedEntry(0x0E200003, 1024)
                .AddBinary(MapiPropertyIds.AttachData, new byte[] { 1, 2, 3 }))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(1, message.Attachments.Count);
        OutlookAttachment attachment = message.Attachments[0];
        Assert.AreEqual(OutlookAttachmentMethod.ByValue, attachment.Method);
        Assert.AreEqual("report-final.pdf", attachment.FileName);
        Assert.AreEqual("cid:report", attachment.ContentId);
        Assert.AreEqual(1024L, attachment.Size);
    }

    /// <summary>
    /// Verifies that a message without attachments surfaces an empty list.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenAbsent_ShouldReturnEmptyList()
    {
        using MemoryStream container = CreateMinimalContainer();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(0, message.Attachments.Count);
    }

    /// <summary>
    /// Verifies that under the tolerant levels an attachment storage that holds a content stream but no
    /// <c>__properties_version1.0</c> stream is omitted, as the <c>.pst</c> reader skips a row it cannot resolve, while
    /// the rest of the message reads normally.
    /// </summary>
    /// <param name="level">The tolerant validation level the message is read at.</param>
    [TestMethod]
    [DataRow(CompoundValidationLevel.Compatible)]
    [DataRow(CompoundValidationLevel.Minimal)]
    public void Attachments_WhenAStorageHasNoPropertyStream_ShouldOmitIt(CompoundValidationLevel level)
    {
        using MemoryStream container = CreateWithAttachmentMissingItsPropertyStream();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = level });

        CollectionAssert.AreEqual(
            new[] { "first.bin", "third.bin" },
            message.Attachments.Select(static attachment => attachment.FileName).ToArray());
        Assert.AreEqual("Test subject", message.Subject);
        Assert.AreEqual("Test body", message.BodyText);
        Assert.AreEqual(1, message.Recipients.Count);
    }

    /// <summary>
    /// Verifies that under <see cref="CompoundValidationLevel.Strict" /> an attachment storage with no
    /// <c>__properties_version1.0</c> stream throws <see cref="OutlookMsgFormatException" /> when the attachments are
    /// read.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenAStorageHasNoPropertyStream_ForStrict_ShouldThrowOutlookMsgFormatException()
    {
        using MemoryStream container = CreateWithAttachmentMissingItsPropertyStream();
        using var message = OutlookMessage.Open(
            container, new OutlookMessageReaderOptions { ValidationLevel = CompoundValidationLevel.Strict });

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = message.Attachments;
        });
    }

    /// <summary>
    /// Builds a minimal message with three by-value attachment storages, the second holding its content stream but no
    /// property stream, and one recipient.
    /// </summary>
    /// <returns>The container stream.</returns>
    private static MemoryStream CreateWithAttachmentMissingItsPropertyStream() =>
        MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(static attachment => attachment
                .AddFixedEntry(0x37050003, 1)
                .AddUnicode(MapiPropertyIds.AttachLongFilename, "first.bin")
                .AddBinary(MapiPropertyIds.AttachData, [1]))
            .AddAttachment(static attachment => attachment
                .OmitPropertiesStream()
                .AddBinary(MapiPropertyIds.AttachData, [2]))
            .AddAttachment(static attachment => attachment
                .AddFixedEntry(0x37050003, 1)
                .AddUnicode(MapiPropertyIds.AttachLongFilename, "third.bin")
                .AddBinary(MapiPropertyIds.AttachData, [3]))
            .AddRecipient(static recipient => recipient.AddUnicode(MapiPropertyIds.DisplayName, "Alex Doe"))
            .Build();
}
