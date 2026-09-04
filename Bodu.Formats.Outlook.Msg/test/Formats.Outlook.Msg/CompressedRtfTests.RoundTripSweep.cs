// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedRtfTests.RoundTripSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Test;

#if OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#else
namespace Bodu.Formats.Outlook.Msg;
#endif

public partial class CompressedRtfTests
{
    /// <summary>
    /// Verifies that a hand-encoded all-literal stream long enough to wrap the 4096-byte dictionary decodes intact
    /// and honors the write-position terminator after the wrap.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decompress_WhenLiteralsWrapDictionary_ShouldDecodeAndTerminate()
    {
        const int LiteralCount = 5000;
        const int InitialWritePosition = 207;
        var literals = new byte[LiteralCount];
        for (int i = 0; i < LiteralCount; i++)
            literals[i] = (byte)('A' + (i % 26));

        // Encode: control bytes with all-literal bits, then a final control byte whose single set bit carries the
        // terminator reference at the wrapped write position.
        using var body = new MemoryStream();
        int position = 0;
        while (position < LiteralCount)
        {
            int chunk = Math.Min(8, LiteralCount - position);
            body.WriteByte(chunk == 8 ? (byte)0x00 : (byte)(0xFF << chunk));
            body.Write(literals, position, chunk);
            position += chunk;

            if (chunk < 8)
            {
                WriteTerminator(body, LiteralCount);
                break;
            }
        }

        if (LiteralCount % 8 == 0)
        {
            body.WriteByte(0x01);
            WriteTerminator(body, LiteralCount);
        }

        byte[] payload = BuildPayload(body.ToArray(), LiteralCount);

        byte[] decoded = CompressedRtf.Decompress(payload);

        CollectionAssert.AreEqual(literals, decoded);

        static void WriteTerminator(MemoryStream stream, int literalCount)
        {
            int writePosition = (InitialWritePosition + literalCount) % 4096;
            Span<byte> token = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(token, (ushort)(writePosition << 4));
            stream.Write(token);
        }
    }

    /// <summary>
    /// Verifies that a large uncompressed-mode payload streams back verbatim.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decompress_WhenLargeUncompressedPayload_ShouldReturnVerbatim()
    {
        var raw = new byte[64 * 1024];
        for (int i = 0; i < raw.Length; i++)
            raw[i] = (byte)(i * 31);

        var payload = new byte[16 + raw.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)(raw.Length + 12));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), (uint)raw.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), 0x414C454D);
        raw.CopyTo(payload.AsSpan(16));

        byte[] decoded = CompressedRtf.Decompress(payload);

        CollectionAssert.AreEqual(raw, decoded);
    }

    /// <summary>
    /// Builds a complete compressed payload around an encoded body: header fields plus the body checksum.
    /// </summary>
    /// <param name="body">The encoded token stream.</param>
    /// <param name="rawSize">The declared uncompressed size.</param>
    /// <returns>The payload bytes.</returns>
    private static byte[] BuildPayload(byte[] body, int rawSize)
    {
        var payload = new byte[16 + body.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)(body.Length + 12));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), (uint)rawSize);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), 0x75465A4C);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12), CompressedRtf.ComputeCrc(body));
        body.CopyTo(payload.AsSpan(16));
        return payload;
    }
}
