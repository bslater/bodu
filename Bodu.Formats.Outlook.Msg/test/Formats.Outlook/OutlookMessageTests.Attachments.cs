// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Attachments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

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
}
