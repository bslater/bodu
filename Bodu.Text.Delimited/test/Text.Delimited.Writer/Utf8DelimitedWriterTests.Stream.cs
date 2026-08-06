// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriterTests.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Verifies the stream-backed mode of <see cref="Utf8DelimitedWriter" />.
/// </summary>
/// <remarks>
/// The writer has two modes and they differ in more than the destination. Writing to an
/// <see cref="IBufferWriter{T}" /> hands bytes straight to the caller's buffer, so nothing is ever pending and
/// flushing does nothing. Writing to a <see cref="Stream" /> stages bytes in an internal buffer that only reaches the
/// destination on a flush - which makes the two byte counters meaningful, and makes an unflushed writer a way to lose
/// a document. Only the buffer-writer mode was covered.
/// </remarks>
public partial class Utf8DelimitedWriterTests
{
    /// <summary>
    /// Verifies that a document written to a stream produces the same bytes as the buffer-writer mode, so choosing a
    /// destination does not change the output.
    /// </summary>
    [TestMethod]
    public void Write_WhenDestinationIsAStream_ShouldEmitTheSameBytesAsBufferWriterMode()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var bufferWriter = new Utf8DelimitedWriter(buffer);
        bufferWriter.WriteStartArray();
        WriteRecord(ref bufferWriter, ("name", "Ada"), ("age", "36"));
        bufferWriter.WriteEndArray();
        bufferWriter.Flush();

        using var destination = new MemoryStream();
        var streamWriter = new Utf8DelimitedWriter(destination);
        streamWriter.WriteStartArray();
        WriteRecord(ref streamWriter, ("name", "Ada"), ("age", "36"));
        streamWriter.WriteEndArray();
        streamWriter.Flush();

        CollectionAssert.AreEqual(buffer.WrittenSpan.ToArray(), destination.ToArray());
    }

    /// <summary>
    /// Verifies that the stream constructor honors the supplied options, so a dialect selected for a stream
    /// destination is not silently replaced by the defaults.
    /// </summary>
    [TestMethod]
    public void Write_WhenStreamConstructorTakesOptions_ShouldHonorThem()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination, new DelimitedWriterOptions { NoHeader = true, Delimiter = '\t' });

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"), ("age", "36"));
        writer.WriteEndArray();
        writer.Flush();

        Assert.AreEqual("Ada\t36\r\n", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that bytes stage in the writer's buffer and reach the stream only on a flush, so a caller can see
    /// what is outstanding - and so an unflushed writer is understood to have written nothing.
    /// </summary>
    [TestMethod]
    public void Flush_WhenDestinationIsAStream_ShouldMovePendingBytesToCommitted()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination);

        Assert.AreEqual(0L, writer.BytesPending);
        Assert.AreEqual(0L, writer.BytesCommitted);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"));
        writer.WriteEndArray();

        Assert.IsGreaterThan(0L, writer.BytesPending);
        Assert.AreEqual(0L, writer.BytesCommitted);
        Assert.AreEqual(0L, destination.Length);

        long pending = writer.BytesPending;
        writer.Flush();

        Assert.AreEqual(0L, writer.BytesPending);
        Assert.AreEqual(pending, writer.BytesCommitted);
        Assert.AreEqual(pending, destination.Length);
    }

    /// <summary>
    /// Verifies that flushing with nothing outstanding leaves the counters and the destination untouched, so a
    /// defensive flush is free rather than emitting a stray record separator.
    /// </summary>
    [TestMethod]
    public void Flush_WhenNothingIsPending_ShouldBeANoOp()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"));
        writer.WriteEndArray();
        writer.Flush();

        long committed = writer.BytesCommitted;
        long length = destination.Length;

        writer.Flush();

        Assert.AreEqual(committed, writer.BytesCommitted);
        Assert.AreEqual(length, destination.Length);
    }

    /// <summary>
    /// Verifies that successive flushes accumulate into the committed counter rather than resetting it, so the count
    /// describes the whole document.
    /// </summary>
    [TestMethod]
    public void Flush_WhenCalledBetweenRecords_ShouldAccumulateCommittedBytes()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DelimitedWriter(destination);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"));
        writer.Flush();
        long first = writer.BytesCommitted;

        WriteRecord(ref writer, ("name", "Grace"));
        writer.WriteEndArray();
        writer.Flush();

        Assert.IsGreaterThan(first, writer.BytesCommitted);
        Assert.AreEqual(writer.BytesCommitted, destination.Length);
        Assert.AreEqual("name\r\nAda\r\nGrace\r\n", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that flushing in buffer-writer mode is a no-op for the counters: bytes are handed to the caller's
    /// buffer as they are written, so nothing is ever pending there.
    /// </summary>
    [TestMethod]
    public void BytesPending_WhenDestinationIsABufferWriter_ShouldAlwaysBeZero()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer);

        writer.WriteStartArray();
        WriteRecord(ref writer, ("name", "Ada"));
        writer.WriteEndArray();

        Assert.AreEqual(0L, writer.BytesPending);
        Assert.IsGreaterThan(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that both stream constructors reject a <see langword="null" /> destination.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8DelimitedWriter((Stream)null!);
        });

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8DelimitedWriter((Stream)null!, DelimitedWriterOptions.Default);
        });
    }
}
