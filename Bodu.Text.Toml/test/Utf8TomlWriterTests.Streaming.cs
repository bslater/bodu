// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that a stream-backed writer buffers the rendered document and delivers it on
    /// <see cref="Utf8TomlWriter.Flush" />, with <see cref="Utf8TomlWriter.BytesPending" /> and
    /// <see cref="Utf8TomlWriter.BytesCommitted" /> tracking the transition.
    /// </summary>
    [TestMethod]
    public void Flush_WhenStreamDestination_ShouldDeliverBufferedBytes()
    {
        using var stream = new MemoryStream();
        var writer = new Utf8TomlWriter(stream);

        writer.WriteStartTable();
        writer.WriteInteger("a", 1);
        Assert.AreEqual(0, writer.BytesPending);

        writer.WriteEndTable();
        Assert.AreEqual(0, stream.Length);
        Assert.AreEqual(6, writer.BytesPending);
        Assert.AreEqual(0, writer.BytesCommitted);

        writer.Flush();
        Assert.AreEqual("a = 1\n", Encoding.UTF8.GetString(stream.ToArray()));
        Assert.AreEqual(0, writer.BytesPending);
        Assert.AreEqual(6, writer.BytesCommitted);
    }

    /// <summary>
    /// Verifies that disposing a stream-backed writer flushes the buffered document, enabling the
    /// <see langword="using" /> pattern.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenStreamDestination_ShouldFlush()
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8TomlWriter(stream))
        {
            writer.WriteStartTable();
            writer.WriteString("s", "x");
            writer.WriteEndTable();
        }

        Assert.AreEqual("s = \"x\"\n", Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlWriter.Reset" /> re-arms the writer for a second document and clears the
    /// byte counts.
    /// </summary>
    [TestMethod]
    public void Reset_WhenDocumentComplete_ShouldAllowSecondDocument()
    {
        using var stream = new MemoryStream();
        var writer = new Utf8TomlWriter(stream);

        writer.WriteStartTable();
        writer.WriteInteger("a", 1);
        writer.WriteEndTable();
        writer.Flush();

        writer.Reset();
        Assert.AreEqual(0, writer.BytesCommitted);
        Assert.AreEqual(0, writer.BytesPending);

        writer.WriteStartTable();
        writer.WriteInteger("b", 2);
        writer.WriteEndTable();
        writer.Flush();

        Assert.AreEqual("a = 1\nb = 2\n", Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>
    /// Verifies that a buffer-writer destination commits bytes as the root table closes, with nothing pending and
    /// <see cref="Utf8TomlWriter.Flush" /> a no-op.
    /// </summary>
    [TestMethod]
    public void BytesCommitted_WhenBufferWriterDestination_ShouldCountAtRootClose()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteInteger("a", 1);
        Assert.AreEqual(0, writer.BytesCommitted);

        writer.WriteEndTable();
        Assert.AreEqual(buffer.WrittenCount, writer.BytesCommitted);
        Assert.AreEqual(0, writer.BytesPending);

        writer.Flush();
        Assert.AreEqual(buffer.WrittenCount, writer.BytesCommitted);
    }

    /// <summary>
    /// Verifies that constructing a stream-backed writer over a non-writable stream throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStreamNotWritable_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using var stream = new MemoryStream([], writable: false);
            _ = new Utf8TomlWriter(stream);
        });
    }
}
