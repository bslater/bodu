// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

public partial class OutlookAttachmentTests
{
    /// <summary>The payload length the deferral tests use.</summary>
    private const int LargePayloadLength = 4 * 1024 * 1024;

    /// <summary>The tag of the by-value payload property.</summary>
    private static readonly MapiPropertyTag AttachDataTag = new(MapiPropertyIds.AttachData, MapiPropertyType.Binary);

    /// <summary>
    /// Opens a message with one large by-value attachment under the streaming container strategy, so the only way the
    /// payload can reach memory is through the reader.
    /// </summary>
    /// <param name="maxInlineAttachmentBytes">The inline limit to open with.</param>
    /// <returns>The container and the open message.</returns>
    private static (MemoryStream Container, OutlookMessage Message) OpenLargeAttachmentMessage(int maxInlineAttachmentBytes)
    {
        MemoryStream container = MsgFixtureBuilder.CreateMinimal().AddLargeAttachment(LargePayloadLength).Build();
        OutlookMessage message = OutlookMessage.Open(
            container,
            new OutlookMessageReaderOptions { ReadStrategy = CompoundReadStrategy.Streaming, MaxInlineAttachmentBytes = maxInlineAttachmentBytes },
            leaveOpen: true);
        return (container, message);
    }

    /// <summary>
    /// Verifies that a by-value payload larger than <see cref="OutlookMessageReaderOptions.MaxInlineAttachmentBytes" />
    /// is not decoded into the property collection: the property stays present with a null value, the conveniences
    /// still work, and decoding the attachment stays under a memory ceiling far below the payload size.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Properties_WhenPayloadExceedsInlineLimit_ShouldDeferPayload()
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
            MapiPropertyCollection properties = attachment.Properties;
            OutlookAttachmentMethod method = attachment.Method;
            string? fileName = attachment.FileName;

            long delta = GC.GetTotalMemory(forceFullCollection: false) - baseline;
            Assert.IsTrue(delta < CeilingBytes, $"Decoding the attachment allocated {delta / 1024} KB — the payload is being materialized.");
            Assert.IsTrue(properties.Contains(AttachDataTag), "The payload property must remain present.");
            Assert.IsNull(properties.GetBinary(MapiPropertyIds.AttachData), "A deferred payload must not be decoded inline.");
            Assert.AreEqual(OutlookAttachmentMethod.ByValue, method);
            Assert.AreEqual("large.bin", fileName);
        }
    }

    /// <summary>
    /// Verifies that a by-value payload within the inline limit is decoded into the property collection as before.
    /// </summary>
    [TestMethod]
    public void Properties_WhenPayloadWithinInlineLimit_ShouldKeepPayloadInline()
    {
        byte[] payload = [1, 2, 3, 4];
        using MemoryStream container = CreateWithByValueAttachment(payload);
        using var message = OutlookMessage.OpenRead(container);

        CollectionAssert.AreEqual(payload, message.Attachments[0].Properties.GetBinary(MapiPropertyIds.AttachData)!.Value.ToArray());
    }

    /// <summary>
    /// Verifies that raising the inline limit above a large payload restores inline decoding.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Properties_WhenInlineLimitRaisedAbovePayload_ShouldKeepPayloadInline()
    {
        (MemoryStream container, OutlookMessage message) = OpenLargeAttachmentMessage(LargePayloadLength + 1);
        using (container)
        using (message)
        {
            ReadOnlyMemory<byte>? payload = message.Attachments[0].Properties.GetBinary(MapiPropertyIds.AttachData);

            Assert.IsNotNull(payload);
            Assert.AreEqual(LargePayloadLength, payload.Value.Length);
            Assert.AreEqual(MsgFixtureBuilder.PatternByte(LargePayloadLength - 1), payload.Value.Span[LargePayloadLength - 1]);
        }
    }
}
