// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentTests.OpenContentStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookAttachmentTests
{
    /// <summary>
    /// Verifies that the by-value payload streams back byte-for-byte.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenByValue_ShouldStreamPayload()
    {
        byte[] payload = { 0xCA, 0xFE, 0xBA, 0xBE };
        using MemoryStream container = CreateWithByValueAttachment(payload);

        using var message = OutlookMessage.OpenRead(container);
        using Stream content = message.Attachments[0].OpenContentStream();
        using var copy = new MemoryStream();
        content.CopyTo(copy);

        CollectionAssert.AreEqual(payload, copy.ToArray());
    }

    /// <summary>
    /// Verifies that content access on an embedded-message attachment throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenEmbeddedMessage_ShouldThrowNotSupportedException()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded.AddUnicode(MapiPropertyIds.Subject, "Inner")))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = message.Attachments[0].OpenContentStream();
        });
    }

    /// <summary>
    /// Verifies that a by-value attachment whose content stream is missing throws
    /// <see cref="OutlookMsgFormatException" />.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenContentMissing_ShouldThrowOutlookMsgFormatException()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment.AddFixedEntry(0x37050003, 1))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = message.Attachments[0].OpenContentStream();
        });
    }
}
