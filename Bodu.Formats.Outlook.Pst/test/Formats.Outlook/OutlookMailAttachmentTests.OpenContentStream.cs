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

    /// <summary>
    /// Verifies that opening the content of a large, subnode-resident attachment does not copy the payload again:
    /// the properties already hold the decoded bytes, and the stream must serve them in place rather than duplicate
    /// tens of megabytes per open.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void OpenContentStream_WhenPayloadIsLarge_ShouldNotCopyIt()
    {
        const int XBlocks = 5;
        const long CeilingBytes = 8L * 1024 * 1024;
        var builder = new PstMessagingFixtureBuilder { LargeAttachmentXBlocks = XBlocks };
        using OutlookMailStore store = OutlookMailStore.Open(builder.BuildStream(), new OutlookMailStoreReaderOptions());

        OutlookMailAttachment attachment = GetAttachments(store)[0];
        _ = attachment.Properties;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long baseline = GC.GetTotalMemory(forceFullCollection: true);

        long total = 0;
        long maxDelta = 0;
        var chunk = new byte[64 * 1024];
        using (Stream content = attachment.OpenContentStream())
        {
            int read;
            int chunkIndex = 0;
            while ((read = content.Read(chunk, 0, chunk.Length)) > 0)
            {
                total += read;
                if ((chunkIndex++ & 15) == 0)
                    maxDelta = Math.Max(maxDelta, GC.GetTotalMemory(forceFullCollection: false) - baseline);
            }
        }

        Assert.AreEqual(builder.LargeAttachmentLength, total);
        Assert.IsTrue(
            maxDelta < CeilingBytes,
            $"Opening a {total / (1024 * 1024)} MB attachment allocated {maxDelta / (1024 * 1024)} MB — the payload is being copied.");
    }
}
