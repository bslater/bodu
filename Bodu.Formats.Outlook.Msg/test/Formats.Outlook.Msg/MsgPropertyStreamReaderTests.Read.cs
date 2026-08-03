// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyStreamReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyStreamReaderTests
{
    /// <summary>
    /// Verifies that a root stream's 32-byte header fields and records parse correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenRootStream_ShouldParseHeaderAndRecords()
    {
        var payload = new byte[32 + 16];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), 3);   // next recipient id
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12), 5);  // next attachment id
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(16), 2);  // recipient count
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(20), 4);  // attachment count
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(32), 0x0037001F);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(36), 0x6);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(40), 0x1234);

        MsgPropertyEntry[] entries = MsgPropertyStreamReader.Read(payload, MsgPropertyStreamKind.Root, out MsgPropertyStreamHeader header);

        Assert.AreEqual(3u, header.NextRecipientId);
        Assert.AreEqual(5u, header.NextAttachmentId);
        Assert.AreEqual(2u, header.RecipientCount);
        Assert.AreEqual(4u, header.AttachmentCount);
        Assert.AreEqual(1, entries.Length);
        Assert.AreEqual(0x0037001Fu, entries[0].Tag);
        Assert.AreEqual(0x6u, entries[0].Flags);
        Assert.AreEqual(0x1234ul, entries[0].Raw);
    }

    /// <summary>
    /// Verifies that an embedded-message stream uses the 24-byte header and still carries the count fields.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmbeddedMessageStream_ShouldUse24ByteHeader()
    {
        var payload = new byte[24 + 16];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(16), 1);  // recipient count
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(24), 0x3001001F);

        MsgPropertyEntry[] entries = MsgPropertyStreamReader.Read(payload, MsgPropertyStreamKind.EmbeddedMessage, out MsgPropertyStreamHeader header);

        Assert.AreEqual(1u, header.RecipientCount);
        Assert.AreEqual(1, entries.Length);
        Assert.AreEqual(0x3001001Fu, entries[0].Tag);
    }

    /// <summary>
    /// Verifies that a recipient or attachment stream uses the 8-byte header with no fields.
    /// </summary>
    [TestMethod]
    public void Read_WhenRecipientStream_ShouldUse8ByteHeader()
    {
        var payload = new byte[8 + 32];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), 0x3001001F);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(24), 0x0C150003);

        MsgPropertyEntry[] entries = MsgPropertyStreamReader.Read(payload, MsgPropertyStreamKind.RecipientOrAttachment, out MsgPropertyStreamHeader header);

        Assert.AreEqual(default, header);
        Assert.AreEqual(2, entries.Length);
        Assert.AreEqual(0x0C150003u, entries[1].Tag);
    }

    /// <summary>
    /// Verifies that a header-only stream yields no records.
    /// </summary>
    [TestMethod]
    public void Read_WhenHeaderOnly_ShouldReturnNoRecords()
    {
        MsgPropertyEntry[] entries = MsgPropertyStreamReader.Read(new byte[32], MsgPropertyStreamKind.Root, out _);

        Assert.AreEqual(0, entries.Length);
    }
}
