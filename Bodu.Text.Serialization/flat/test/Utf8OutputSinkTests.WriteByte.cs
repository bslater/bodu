// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8OutputSinkTests.WriteByte.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8OutputSinkTests
{
    /// <summary>
    /// Verifies that a byte is written as itself, whatever its value, and counted once, in either mode.
    /// </summary>
    [TestMethod]
    public void WriteByte_WhenBytesAreWritten_ShouldWriteEachAsItselfInEitherMode()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var bufferSink = new Utf8OutputSink(buffer);
        using var destination = new MemoryStream();
        var streamSink = new Utf8OutputSink(destination);

        foreach (byte value in new byte[] { (byte)',', (byte)'\t', 0x00, 0xFF })
        {
            bufferSink.WriteByte(value);
            streamSink.WriteByte(value);
        }

        Assert.AreEqual(4L, streamSink.BytesPending);
        streamSink.Flush();

        CollectionAssert.AreEqual(new byte[] { (byte)',', (byte)'\t', 0x00, 0xFF }, buffer.WrittenSpan.ToArray());
        CollectionAssert.AreEqual(buffer.WrittenSpan.ToArray(), destination.ToArray());
        Assert.AreEqual(4L, bufferSink.BytesCommitted);
        Assert.AreEqual(4L, streamSink.BytesCommitted);
    }
}
