// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentTests.Size.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookAttachmentTests
{
    /// <summary>
    /// Verifies that <see cref="OutlookAttachment.Size" /> returns the declared <c>PidTagAttachSize</c> when present.
    /// </summary>
    [TestMethod]
    public void Size_WhenAttachSizeDeclared_ShouldReturnDeclaredValue()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddFixedEntry(0x0E200003, 12345)
                .AddBinary(MapiPropertyIds.AttachData, new byte[] { 1, 2, 3 }))
            .Build();
        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(12345L, message.Attachments[0].Size);
    }

    /// <summary>
    /// Verifies that <see cref="OutlookAttachment.Size" /> falls back to the inline payload length when
    /// <c>PidTagAttachSize</c> is absent.
    /// </summary>
    [TestMethod]
    public void Size_WhenAttachSizeAbsent_ForInlinePayload_ShouldReturnPayloadLength()
    {
        using MemoryStream container = CreateWithByValueAttachment([1, 2, 3, 4, 5]);
        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(5L, message.Attachments[0].Size);
    }

    /// <summary>
    /// Verifies that <see cref="OutlookAttachment.Size" /> falls back to the container stream length for a deferred
    /// payload, without materializing it.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Size_WhenAttachSizeAbsent_ForDeferredPayload_ShouldReturnPayloadLength()
    {
        (MemoryStream container, OutlookMessage message) = OpenLargeAttachmentMessage(64 * 1024);
        using (container)
        using (message)
        {
            Assert.AreEqual((long)LargePayloadLength, message.Attachments[0].Size);
        }
    }
}
