// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.OpenContentStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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
    /// <see cref="NotSupportedException" /> - the payload is a message object, not a byte stream.
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
            $"Opening a {total / (1024 * 1024)} MB attachment allocated {maxDelta / (1024 * 1024)} MB - the payload is being copied.");
    }

    /// <summary>
    /// Verifies that the whole path - enumerating the attachment, reading its method and properties, and streaming
    /// its content - stays under a memory ceiling far below a deferred payload's size, and yields every byte.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void OpenContentStream_WhenPayloadDeferred_ShouldStreamWithoutMaterializing()
    {
        const long CeilingBytes = 8L * 1024 * 1024;
        var builder = new PstMessagingFixtureBuilder { LargeAttachmentXBlocks = 5 };
        using OutlookMailStore store = OutlookMailStore.Open(builder.BuildStream(), new OutlookMailStoreReaderOptions());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long baseline = GC.GetTotalMemory(forceFullCollection: true);

        OutlookMailAttachment attachment = GetAttachments(store)[0];
        Assert.AreEqual(OutlookAttachmentMethod.ByValue, attachment.Method);
        _ = attachment.Properties;

        long total = 0;
        long maxDelta = GC.GetTotalMemory(forceFullCollection: false) - baseline;
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
        Assert.IsTrue(maxDelta < CeilingBytes,
            $"Reading a {builder.LargeAttachmentLength / (1024 * 1024)} MB attachment peaked {maxDelta / (1024 * 1024)} MB above baseline - the payload is being materialized.");
    }

    /// <summary>
    /// Verifies that content access on an <c>afByWebReference</c> attachment throws <see cref="NotSupportedException" />:
    /// the content is held by a web service, so a by-value payload the attachment object also carries is not served.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenByWebReferenceAttachment_ShouldThrowNotSupportedException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b => b.AttachMethodOverride = 7);

        OutlookMailAttachment attachment = GetAttachments(store)[0];

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = attachment.OpenContentStream();
        });
    }

    /// <summary>
    /// Verifies that content access on an attachment that declares a method other than <c>afByValue</c> throws
    /// <see cref="NotSupportedException" /> naming that method, and does not serve the by-value payload the attachment
    /// object also carries.
    /// </summary>
    /// <param name="method">The declared <c>PidTagAttachMethod</c> value.</param>
    [TestMethod]
    [DataRow(OutlookAttachmentMethod.None)]
    [DataRow(OutlookAttachmentMethod.ByReference)]
    [DataRow(OutlookAttachmentMethod.ByReferenceResolve)]
    [DataRow(OutlookAttachmentMethod.ByReferenceOnly)]
    [DataRow(OutlookAttachmentMethod.EmbeddedMessage)]
    [DataRow(OutlookAttachmentMethod.Ole)]
    [DataRow(OutlookAttachmentMethod.ByWebReference)]
    public void OpenContentStream_WhenMethodIsNotByValueAndPayloadPresent_ShouldThrowNotSupportedException(OutlookAttachmentMethod method)
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(b => b.AttachMethodOverride = (uint)method);

        OutlookMailAttachment attachment = GetAttachments(store)[0];

        var ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = attachment.OpenContentStream();
        });

        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, OutlookPstResourceStrings.Op_NotSupported_PstAttachmentContent, method),
            ex.Message);
    }

    /// <summary>
    /// Verifies that content access on an attachment that declares a method other than <c>afByValue</c> and carries no
    /// by-value payload throws <see cref="NotSupportedException" />, not <see cref="OutlookPstFormatException" />: the
    /// method stores no payload, so its absence is not a format error.
    /// </summary>
    /// <param name="method">The declared <c>PidTagAttachMethod</c> value.</param>
    [TestMethod]
    [DataRow(OutlookAttachmentMethod.None)]
    [DataRow(OutlookAttachmentMethod.ByReference)]
    [DataRow(OutlookAttachmentMethod.ByReferenceResolve)]
    [DataRow(OutlookAttachmentMethod.ByReferenceOnly)]
    [DataRow(OutlookAttachmentMethod.EmbeddedMessage)]
    [DataRow(OutlookAttachmentMethod.Ole)]
    [DataRow(OutlookAttachmentMethod.ByWebReference)]
    public void OpenContentStream_WhenMethodIsNotByValueAndPayloadAbsent_ShouldThrowNotSupportedException(OutlookAttachmentMethod method)
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(b =>
        {
            b.AttachMethodOverride = (uint)method;
            b.OmitAttachData = true;
        });

        OutlookMailAttachment attachment = GetAttachments(store)[0];

        var ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = attachment.OpenContentStream();
        });

        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, OutlookPstResourceStrings.Op_NotSupported_PstAttachmentContent, method),
            ex.Message);
    }

    /// <summary>
    /// Verifies that content access on an attachment that declares <c>afByValue</c> but carries no
    /// <c>PidTagAttachDataBinary</c> property throws <see cref="OutlookPstFormatException" />: the payload its method
    /// promises is missing.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenByValuePayloadAbsent_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b => b.OmitAttachData = true);

        OutlookMailAttachment attachment = GetAttachments(store)[0];

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = attachment.OpenContentStream();
        });
    }

    /// <summary>
    /// Verifies that an attachment that declares no method but carries a by-value payload reports
    /// <see cref="OutlookAttachmentMethod.ByValue" />, and that its content stream serves that payload.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenMethodAbsentAndPayloadPresent_ShouldReturnStoredPayload()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b => b.OmitAttachMethod = true);

        OutlookMailAttachment attachment = GetAttachments(store)[0];
        using Stream content = attachment.OpenContentStream();
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);

        Assert.AreEqual(OutlookAttachmentMethod.ByValue, attachment.Method);
        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.AttachmentContent, buffer.ToArray());
    }
}
