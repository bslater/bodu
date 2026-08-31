// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.OpenContentStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailAttachmentTests
{
    /// <summary>
    /// Verifies that a by-value attachment's content stream serves exactly the stored payload bytes.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void OpenContentStream_WhenByValueAttachment_ShouldReturnStoredPayload()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        using Stream content = GetAttachments(store)[0].OpenContentStream();
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);

        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.AttachmentContent, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that opening the content stream of an embedded-message attachment throws
    /// <see cref="NotSupportedException" /> — the payload is a message object, not a byte stream.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenEmbeddedMessageAttachment_ShouldThrowNotSupportedException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        OutlookMailAttachment attachment = GetAttachments(store)[1];

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = attachment.OpenContentStream();
        });
    }
}
