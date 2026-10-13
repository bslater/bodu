// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8OutputSinkTests.Flush.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8OutputSinkTests
{
    /// <summary>
    /// Verifies that a flush writes the staged bytes to the stream, commits them and clears them, and that successive
    /// flushes add to the committed count.
    /// </summary>
    [TestMethod]
    public void Flush_WhenDestinationIsAStream_ShouldWriteCommitAndClearTheStagedBytes()
    {
        using var destination = new MemoryStream();
        var sink = new Utf8OutputSink(destination);

        sink.WriteRaw("ab"u8);
        sink.Flush();
        sink.WriteText("cd");
        sink.Flush();

        CollectionAssert.AreEqual("abcd"u8.ToArray(), destination.ToArray());
        Assert.AreEqual(4L, sink.BytesCommitted);
        Assert.AreEqual(0L, sink.BytesPending);
    }

    /// <summary>
    /// Verifies that a flush with nothing staged writes nothing to the stream.
    /// </summary>
    [TestMethod]
    public void Flush_WhenNothingIsPending_ShouldLeaveTheStreamUnchanged()
    {
        using var destination = new MemoryStream();
        var sink = new Utf8OutputSink(destination);

        sink.Flush();
        sink.WriteRaw("a"u8);
        sink.Flush();
        sink.Flush();

        Assert.AreEqual(1L, destination.Length);
        Assert.AreEqual(1L, sink.BytesCommitted);
    }

    /// <summary>
    /// Verifies that a flush in buffer-writer mode, and on a default sink, does nothing.
    /// </summary>
    [TestMethod]
    public void Flush_WhenDestinationIsABufferWriterOrNone_ShouldDoNothing()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var sink = new Utf8OutputSink(buffer);
        sink.WriteRaw("ab"u8);
        Utf8OutputSink empty = default;

        sink.Flush();
        empty.Flush();

        Assert.AreEqual(2, buffer.WrittenCount);
        Assert.AreEqual(2L, sink.BytesCommitted);
        Assert.AreEqual(0L, empty.BytesCommitted);
        Assert.AreEqual(0L, empty.BytesPending);
    }
}
