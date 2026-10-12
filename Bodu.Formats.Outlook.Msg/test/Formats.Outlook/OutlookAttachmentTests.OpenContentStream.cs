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

    /// <summary>
    /// Verifies that content access on a by-reference attachment throws <see cref="NotSupportedException" /> - the
    /// method carries no by-value payload, so the absence of a content stream is not a format error.
    /// </summary>
    /// <param name="method">The declared <c>PidTagAttachMethod</c> value.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void OpenContentStream_WhenByReference_ShouldThrowNotSupportedException(int method)
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddFixedEntry(0x37050003, (ulong)method)
                .AddUnicode(MapiPropertyIds.AttachLongFilename, "linked.docx"))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = message.Attachments[0].OpenContentStream();
        });
    }

    /// <summary>
    /// Verifies that a deferred payload streams every byte from the container under a memory ceiling far below its
    /// size when the container is read with the streaming strategy.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void OpenContentStream_WhenPayloadDeferred_ShouldStreamPatternedBytesWithoutMaterializing()
    {
        const long CeilingBytes = 1L * 1024 * 1024;
        (MemoryStream container, OutlookMessage message) = OpenLargeAttachmentMessage(64 * 1024);
        using (container)
        using (message)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long baseline = GC.GetTotalMemory(forceFullCollection: true);

            OutlookAttachment attachment = message.Attachments[0];
            Assert.AreEqual(OutlookAttachmentMethod.ByValue, attachment.Method);

            long total = 0;
            long maxDelta = GC.GetTotalMemory(forceFullCollection: false) - baseline;
            var chunk = new byte[64 * 1024];
            using (Stream content = attachment.OpenContentStream())
            {
                int read;
                int chunkIndex = 0;
                while ((read = content.Read(chunk, 0, chunk.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        if (chunk[i] != MsgFixtureBuilder.PatternByte(total + i))
                            Assert.Fail($"Byte {total + i} does not match the pattern.");
                    }

                    total += read;
                    if ((chunkIndex++ & 7) == 0)
                        maxDelta = Math.Max(maxDelta, GC.GetTotalMemory(forceFullCollection: false) - baseline);
                }
            }

            Assert.AreEqual((long)LargePayloadLength, total);
            Assert.IsTrue(maxDelta < CeilingBytes, $"Streaming the attachment peaked {maxDelta / 1024} KB above baseline - the payload is being materialized.");
        }
    }

    /// <summary>
    /// Verifies that content access on an <c>afByWebReference</c> attachment throws <see cref="NotSupportedException" />:
    /// the content is held by a web service, so a by-value stream the writer left beside the reference is not served.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenByWebReference_ShouldThrowNotSupportedException()
    {
        using MemoryStream container = CreateWithWebReferenceAttachment(withPayload: true);

        using var message = OutlookMessage.OpenRead(container);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = message.Attachments[0].OpenContentStream();
        });
    }

    /// <summary>
    /// Verifies that a container fault met after the message is open, while a payload above the inline threshold is
    /// streamed under <see cref="Bodu.IO.Compound.CompoundReadStrategy.Streaming" /> and its sector chain points past
    /// the end of the file, is reported as a malformed container with the container's exception as the inner exception.
    /// </summary>
    [TestMethod]
    public void OpenContentStream_WhenAStreamedPayloadMeetsABrokenSectorChain_ShouldReportAMalformedContainer()
    {
        using MemoryStream built = MsgFixtureBuilder.CreateMinimal().AddLargeAttachment(8192).Build();
        byte[] file = built.ToArray();
        MsgFixtureBuilder.BreakStreamChain(file, MsgStreamNames.GetSubstgStreamName(0x37010102));
        var options = new OutlookMessageReaderOptions
        {
            ReadStrategy = Bodu.IO.Compound.CompoundReadStrategy.Streaming,
            MaxInlineAttachmentBytes = 1024,
        };
        using var message = OutlookMessage.Open(new MemoryStream(file), options);
        OutlookAttachment attachment = message.Attachments[0];

        var ex = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            using Stream content = attachment.OpenContentStream();
            content.CopyTo(Stream.Null);
        });

        Assert.AreEqual(OutlookMsgResourceStrings.Format_Invalid_MsgCompoundMalformed, ex.Message);
        Assert.IsInstanceOfType<Bodu.IO.Compound.CompoundFileException>(ex.InnerException);
    }
}
