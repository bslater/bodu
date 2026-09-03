// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedRtfTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class CompressedRtfTests
{
    /// <summary>The dictionary write position after the format's seeded prologue.</summary>
    private const int SeededWritePosition = 207;

    /// <summary>
    /// Encodes a literal-only token stream of the given length, terminated by the write-position reference the
    /// format uses as its end marker.
    /// </summary>
    /// <param name="literalCount">The number of literal bytes.</param>
    /// <returns>The encoded body, without the 16-byte header.</returns>
    private static byte[] BuildLiteralBody(int literalCount)
    {
        using var body = new MemoryStream();
        int position = 0;
        while (position < literalCount)
        {
            int chunk = Math.Min(8, literalCount - position);
            body.WriteByte(chunk == 8 ? (byte)0x00 : (byte)(0xFF << chunk));
            for (int i = 0; i < chunk; i++)
                body.WriteByte((byte)('A' + ((position + i) % 26)));
            position += chunk;

            if (chunk < 8)
            {
                WriteTerminator(body, literalCount);
                return body.ToArray();
            }
        }

        body.WriteByte(0x01);
        WriteTerminator(body, literalCount);
        return body.ToArray();

        static void WriteTerminator(MemoryStream stream, int literalCount)
        {
            int writePosition = (SeededWritePosition + literalCount) % 4096;
            Span<byte> token = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(token, (ushort)(writePosition << 4));
            stream.Write(token);
        }
    }

    /// <summary>
    /// Verifies that decoding a multi-megabyte body allocates no more than one and a half times its output — a
    /// growable buffer plus a final copy would double it.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decompress_WhenBodyIsLarge_ShouldAllocateAtMostOnePointFiveTimesOutput()
    {
        const int LiteralCount = 4 * 1024 * 1024;
        byte[] payload = BuildPayload(BuildLiteralBody(LiteralCount), LiteralCount);

        // Warm the code path so JIT and static-initialization allocations are excluded from the measurement.
        _ = CompressedRtf.Decompress(payload);

        long before = GC.GetAllocatedBytesForCurrentThread();
        byte[] decoded = CompressedRtf.Decompress(payload);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(LiteralCount, decoded.Length);
        Assert.IsTrue(
            allocated <= (long)decoded.Length * 3 / 2,
            $"Decoding a {decoded.Length / (1024 * 1024)} MB body allocated {allocated / (1024 * 1024)} MB — the output is being copied.");
    }
}
