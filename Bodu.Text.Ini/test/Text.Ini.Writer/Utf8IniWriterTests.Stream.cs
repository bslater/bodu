// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniWriterTests.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Ini.Writer;

/// <summary>
/// Verifies the stream-backed mode of <see cref="Utf8IniWriter" />.
/// </summary>
/// <remarks>
/// Writing to an <see cref="IBufferWriter{T}" /> hands bytes straight to the caller's buffer, so nothing is ever
/// pending and a flush does nothing. Writing to a <see cref="Stream" /> stages them internally until a flush, which
/// is what makes the byte counters meaningful and makes an unflushed writer a way to lose a document. Only the
/// buffer-writer mode was covered.
/// </remarks>
public partial class Utf8IniWriterTests
{
    /// <summary>
    /// Writes a small document through the supplied writer.
    /// </summary>
    /// <param name="writer">The writer.</param>
    private static void WriteSample(ref Utf8IniWriter writer)
    {
        writer.WritePropertyName("key0");
        writer.WriteString("a");
        writer.WriteSectionHeader("db");
        writer.WritePropertyName("host");
        writer.WriteString("localhost");
    }

    /// <summary>
    /// Verifies that a document written to a stream produces the same bytes as the buffer-writer mode, so choosing a
    /// destination does not change the output.
    /// </summary>
    [TestMethod]
    public void Write_WhenDestinationIsAStream_ShouldEmitTheSameBytesAsBufferWriterMode()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var bufferWriter = new Utf8IniWriter(buffer);
        WriteSample(ref bufferWriter);
        bufferWriter.Flush();

        using var destination = new MemoryStream();
        var streamWriter = new Utf8IniWriter(destination);
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
        var writer = new Utf8IniWriter(destination, new IniWriterOptions { CommentPrefix = ';' });

        writer.WriteComment(" note");
        writer.Flush();

        Assert.StartsWith(";", Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that bytes stage in the writer and reach the stream only on a flush, so an unflushed writer is
    /// understood to have written nothing.
    /// </summary>
    [TestMethod]
    public void Flush_WhenDestinationIsAStream_ShouldMovePendingBytesToCommitted()
    {
        using var destination = new MemoryStream();
        var writer = new Utf8IniWriter(destination);

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
        var writer = new Utf8IniWriter(destination);
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

        var writer = new Utf8IniWriter(destination);
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
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Utf8IniWriter((Stream)null!));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Utf8IniWriter((Stream)null!, IniWriterOptions.Default));
    }
}
