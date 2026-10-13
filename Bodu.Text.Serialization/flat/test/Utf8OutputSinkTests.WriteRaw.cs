// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8OutputSinkTests.WriteRaw.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8OutputSinkTests
{
    /// <summary>
    /// Verifies that bytes written to a buffer writer reach it at once and are committed as they are written.
    /// </summary>
    [TestMethod]
    public void WriteRaw_WhenDestinationIsABufferWriter_ShouldWriteAndCommitTheBytes()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var sink = new Utf8OutputSink(buffer);

        sink.WriteRaw("ab"u8);
        sink.WriteRaw([]);
        sink.WriteRaw("c\n"u8);

        CollectionAssert.AreEqual("abc\n"u8.ToArray(), buffer.WrittenSpan.ToArray());
        Assert.AreEqual(4L, sink.BytesCommitted);
        Assert.AreEqual(0L, sink.BytesPending);
    }

    /// <summary>
    /// Verifies that bytes written to a stream are staged, pending and uncommitted, until a flush.
    /// </summary>
    [TestMethod]
    public void WriteRaw_WhenDestinationIsAStream_ShouldStageTheBytesUntilAFlush()
    {
        using var destination = new MemoryStream();
        var sink = new Utf8OutputSink(destination);

        sink.WriteRaw("abc"u8);

        Assert.AreEqual(0L, destination.Length);
        Assert.AreEqual(0L, sink.BytesCommitted);
        Assert.AreEqual(3L, sink.BytesPending);
    }
}
