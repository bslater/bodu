// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvWriterTests.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Verifies the stream-backed mode of <see cref="Utf8DotEnvWriter" />.
/// </summary>
/// <remarks>
/// Writing to an <see cref="IBufferWriter{T}" /> hands bytes straight to the caller's buffer, so nothing is ever
/// pending and a flush does nothing. Writing to a <see cref="Stream" /> stages them internally until a flush, which
/// is what makes the byte counters meaningful and makes an unflushed writer a way to lose a document. Only the
/// buffer-writer mode was covered.
/// </remarks>
public partial class Utf8DotEnvWriterTests
{
    /// <summary>
    /// Writes a small document through the supplied writer.
    /// </summary>
    /// <param name="writer">The writer.</param>
    private static void WriteSample(ref Utf8DotEnvWriter writer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("HOST");
        writer.WriteString("localhost");
        writer.WritePropertyName("PORT");
        writer.WriteString("8080");
        writer.WriteEndObject();
    }

    /// <summary>
    /// Verifies that a document written to a stream produces the same bytes as the buffer-writer mode, so choosing a
    /// destination does not change the output.
    /// </summary>
    [TestMethod]
    public void Write_WhenDestinationIsAStream_ShouldEmitTheSameBytesAsBufferWriterMode()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var bufferWriter = new Utf8DotEnvWriter(buffer);
        WriteSample(ref bufferWriter);
        bufferWriter.Flush();

        using var destination = new MemoryStream();
        var streamWriter = new Utf8DotEnvWriter(destination);
        WriteSample(ref streamWriter);
        streamWriter.Flush();

        CollectionAssert.AreEqual(buffer.WrittenSpan.ToArray(), destination.ToArray());
    }

    /// <summary>
    /// Verifies that the stream constructor honors the supplied options rather than silently replacing them with the
    /// defaults.
    /// </summary>
    [TestMethod]
    public void Write_WhenStreamConstructorTakesOptions_ShouldHonorThem()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DotEnvWriter(destination, new DotEnvWriterOptions { WriteExportPrefix = true });

        WriteSample(ref writer);
        writer.Flush();

        Assert.StartsWith("export ", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that bytes stage in the writer and reach the stream only on a flush, so an unflushed writer is
    /// understood to have written nothing.
    /// </summary>
    [TestMethod]
    public void Flush_WhenDestinationIsAStream_ShouldMovePendingBytesToCommitted()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DotEnvWriter(destination);

        Assert.AreEqual(0L, writer.BytesPending);
        Assert.AreEqual(0L, writer.BytesCommitted);

        WriteSample(ref writer);

        Assert.IsGreaterThan(0L, writer.BytesPending);
        Assert.AreEqual(0L, destination.Length);

        long pending = writer.BytesPending;
        writer.Flush();

        Assert.AreEqual(0L, writer.BytesPending);
        Assert.AreEqual(pending, writer.BytesCommitted);
        Assert.AreEqual(pending, destination.Length);
    }

    /// <summary>
    /// Verifies that flushing with nothing outstanding leaves the destination untouched.
    /// </summary>
    [TestMethod]
    public void Flush_WhenNothingIsPending_ShouldBeANoOp()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DotEnvWriter(destination);
        WriteSample(ref writer);
        writer.Flush();

        long length = destination.Length;
        writer.Flush();

        Assert.AreEqual(length, destination.Length);
    }

    /// <summary>
    /// Verifies that disposing flushes what is outstanding, so a writer used in a <c>using</c> block cannot silently
    /// drop the tail of a document.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenBytesArePending_ShouldFlushThem()
    {
        using var destination = new MemoryStream();

        var writer = new Utf8DotEnvWriter(destination);
        WriteSample(ref writer);
        writer.Dispose();

        Assert.IsGreaterThan(0L, destination.Length);
        Assert.AreEqual(destination.Length, writer.BytesCommitted);
    }

    /// <summary>
    /// Verifies that both stream constructors reject a <see langword="null" /> destination.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Utf8DotEnvWriter((Stream)null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Utf8DotEnvWriter((Stream)null!, DotEnvWriterOptions.Default));
    }

    /// <summary>
    /// Verifies that both buffer-writer constructors reject a <see langword="null" /> destination, naming the
    /// <c>output</c> parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOutputIsNull_ShouldThrowArgumentNullExceptionNamingIt()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8DotEnvWriter((IBufferWriter<byte>)null!);
        });

        var optionsEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8DotEnvWriter((IBufferWriter<byte>)null!, DotEnvWriterOptions.Default);
        });

        Assert.AreEqual("output", ex.ParamName);
        Assert.AreEqual("output", optionsEx.ParamName);
    }

    /// <summary>
    /// Verifies that both stream constructors name the <c>stream</c> parameter when it is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStreamIsNull_ShouldNameTheStreamParameter()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8DotEnvWriter((Stream)null!);
        });

        var optionsEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Utf8DotEnvWriter((Stream)null!, DotEnvWriterOptions.Default);
        });

        Assert.AreEqual("stream", ex.ParamName);
        Assert.AreEqual("stream", optionsEx.ParamName);
    }

    /// <summary>
    /// Verifies that in buffer-writer mode every byte counts as committed as soon as it is written, with nothing
    /// pending, and that a flush there changes neither count.
    /// </summary>
    [TestMethod]
    public void BytesCommitted_WhenDestinationIsABufferWriter_ShouldCountEveryByteAsItIsWritten()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DotEnvWriter(buffer);
        writer.WritePropertyName("HOST");
        writer.WriteString("localhost");

        Assert.IsGreaterThan(0, buffer.WrittenCount);
        Assert.AreEqual(buffer.WrittenCount, writer.BytesCommitted);
        Assert.AreEqual(0L, writer.BytesPending);

        writer.Flush();

        Assert.AreEqual(buffer.WrittenCount, writer.BytesCommitted);
        Assert.AreEqual(0L, writer.BytesPending);
    }

    /// <summary>
    /// Verifies that text outside ASCII is written as its UTF-8 bytes, the same in stream mode as in buffer-writer mode,
    /// and that the committed count of each mode is the number of those bytes.
    /// </summary>
    [TestMethod]
    public void Write_WhenTextIsOutsideAscii_ShouldEmitTheSameUtf8BytesInBothModes()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var bufferWriter = new Utf8DotEnvWriter(buffer);
        bufferWriter.WriteComment("na\u00EFve \u2615");
        bufferWriter.WritePropertyName("DRINK");
        bufferWriter.WriteString("caf\u00E9");
        bufferWriter.WritePropertyName("QUOTED");
        bufferWriter.WriteString(" \"\u2615\" ");

        using var destination = new MemoryStream();
        var streamWriter = new Utf8DotEnvWriter(destination);
        streamWriter.WriteComment("na\u00EFve \u2615");
        streamWriter.WritePropertyName("DRINK");
        streamWriter.WriteString("caf\u00E9");
        streamWriter.WritePropertyName("QUOTED");
        streamWriter.WriteString(" \"\u2615\" ");
        streamWriter.Flush();

        CollectionAssert.AreEqual(buffer.WrittenSpan.ToArray(), destination.ToArray());
        Assert.AreEqual(buffer.WrittenCount, bufferWriter.BytesCommitted);
        Assert.AreEqual(destination.Length, streamWriter.BytesCommitted);
        Assert.Contains("\u2615", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that successive flushes accumulate into the committed count rather than resetting it, so the count
    /// describes the whole document.
    /// </summary>
    [TestMethod]
    public void Flush_WhenCalledBetweenEntries_ShouldAccumulateCommittedBytes()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DotEnvWriter(destination);

        writer.WritePropertyName("A");
        writer.WriteString("1");
        writer.Flush();
        long first = writer.BytesCommitted;

        writer.WritePropertyName("B");
        writer.WriteString("2");
        writer.Flush();

        Assert.IsGreaterThan(first, writer.BytesCommitted);
        Assert.AreEqual(destination.Length, writer.BytesCommitted);
        Assert.AreEqual("A=1\nB=2\n", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that a stream-backed writer disposed twice neither throws nor writes its pending bytes a second time.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldWriteTheDocumentOnce()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8DotEnvWriter(destination);
        writer.WritePropertyName("A");
        writer.WriteString("1");

        writer.Dispose();
        writer.Dispose();

        Assert.AreEqual("A=1\n", Encoding.UTF8.GetString(destination.ToArray()));
        Assert.AreEqual(destination.Length, writer.BytesCommitted);
    }
}
