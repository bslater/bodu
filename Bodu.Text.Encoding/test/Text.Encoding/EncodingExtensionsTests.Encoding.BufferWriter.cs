// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.BufferWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WritePreamble(System.Text.Encoding, IBufferWriter{byte})" />
    /// writes the UTF-8 byte order mark when invoked on an encoding constructed with the BOM enabled.
    /// </summary>
    [TestMethod]
    public void WritePreamble_WhenEncodingHasPreamble_ShouldWriteBomBytes()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        using var writer = new PooledBufferBuilder<byte>(16);

        utf8WithBom.WritePreamble(writer);

        CollectionAssert.AreEqual(utf8WithBom.Preamble.ToArray(), writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WritePreamble(System.Text.Encoding, IBufferWriter{byte})" />
    /// writes no bytes when the encoding does not emit a preamble.
    /// </summary>
    [TestMethod]
    public void WritePreamble_WhenEncodingHasNoPreamble_ShouldNotWriteAnyBytes()
    {
        System.Text.Encoding utf8NoBom = new System.Text.UTF8Encoding(false);
        using var writer = new PooledBufferBuilder<byte>(16);

        utf8NoBom.WritePreamble(writer);

        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WritePreamble(System.Text.Encoding, IBufferWriter{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WritePreamble_WhenEncodingIsNull_ShouldThrowExactly()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            EncodingExtensions.WritePreamble(null!, writer);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WritePreamble(System.Text.Encoding, IBufferWriter{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>writer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WritePreamble_WhenWriterIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            System.Text.Encoding.UTF8.WritePreamble(null!);
        });

        Assert.AreEqual("writer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteBytes(System.Text.Encoding, ReadOnlySpan{char}, IBufferWriter{byte})" />
    /// writes the encoded bytes into the supplied writer.
    /// </summary>
    [TestMethod]
    public void WriteBytes_WhenInvoked_ShouldWriteEncodedBytesIntoWriter()
    {
        var expected = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        using var writer = new PooledBufferBuilder<byte>(16);

        System.Text.Encoding.UTF8.WriteBytes(MultiByteText, writer);

        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteBytes(System.Text.Encoding, ReadOnlySpan{char}, IBufferWriter{byte})" />
    /// writes nothing when the input span is empty.
    /// </summary>
    [TestMethod]
    public void WriteBytes_WhenSpanIsEmpty_ShouldNotWriteAnyBytes()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        System.Text.Encoding.UTF8.WriteBytes(ReadOnlySpan<char>.Empty, writer);

        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteBytes(System.Text.Encoding, ReadOnlySpan{char}, IBufferWriter{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteBytes_WhenEncodingIsNull_ShouldThrowExactly()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            EncodingExtensions.WriteBytes(null!, SampleText, writer);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteBytes(System.Text.Encoding, ReadOnlySpan{char}, IBufferWriter{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>writer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteBytes_WhenWriterIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            System.Text.Encoding.UTF8.WriteBytes(SampleText, null!);
        });

        Assert.AreEqual("writer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteBytesWithPreamble(System.Text.Encoding, ReadOnlySpan{char}, IBufferWriter{byte})" />
    /// writes the preamble followed by the encoded bytes for a BOM-enabled UTF-8 encoding.
    /// </summary>
    [TestMethod]
    public void WriteBytesWithPreamble_WhenEncodingHasPreamble_ShouldWriteBomThenEncodedBytes()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);
        var expectedTail = utf8WithBom.GetBytes(SampleText);
        using var writer = new PooledBufferBuilder<byte>(64);

        utf8WithBom.WriteBytesWithPreamble(SampleText, writer);

        var writtenArray = writer.WrittenSpan.ToArray();
        CollectionAssert.AreEqual(
            utf8WithBom.Preamble.ToArray(),
            writtenArray[..utf8WithBom.Preamble.Length]);
        CollectionAssert.AreEqual(
            expectedTail,
            writtenArray[utf8WithBom.Preamble.Length..]);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteBytesWithPreamble(System.Text.Encoding, ReadOnlySpan{char}, IBufferWriter{byte})" />
    /// throws <see cref="ArgumentNullException" /> for null encoding.
    /// </summary>
    [TestMethod]
    public void WriteBytesWithPreamble_WhenEncodingIsNull_ShouldThrowExactly()
    {
        using var writer = new PooledBufferBuilder<byte>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            EncodingExtensions.WriteBytesWithPreamble(null!, SampleText, writer);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteChars(System.Text.Encoding, ReadOnlySpan{byte}, IBufferWriter{char})" />
    /// writes the decoded characters into the supplied writer.
    /// </summary>
    [TestMethod]
    public void WriteChars_WhenInvoked_ShouldWriteDecodedCharsIntoWriter()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        using var writer = new PooledBufferBuilder<char>(16);

        System.Text.Encoding.UTF8.WriteChars(bytes, writer);

        Assert.AreEqual(MultiByteText, new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteChars(System.Text.Encoding, ReadOnlySpan{byte}, IBufferWriter{char})" />
    /// writes nothing when the input span is empty.
    /// </summary>
    [TestMethod]
    public void WriteChars_WhenSpanIsEmpty_ShouldNotWriteAnyChars()
    {
        using var writer = new PooledBufferBuilder<char>(16);

        System.Text.Encoding.UTF8.WriteChars(ReadOnlySpan<byte>.Empty, writer);

        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteChars(System.Text.Encoding, ReadOnlySpan{byte}, IBufferWriter{char})" />
    /// throws <see cref="ArgumentNullException" /> for null encoding.
    /// </summary>
    [TestMethod]
    public void WriteChars_WhenEncodingIsNull_ShouldThrowExactly()
    {
        using var writer = new PooledBufferBuilder<char>(16);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            EncodingExtensions.WriteChars(null!, new byte[] { 0x68 }, writer);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WriteChars(System.Text.Encoding, ReadOnlySpan{byte}, IBufferWriter{char})" />
    /// throws <see cref="ArgumentNullException" /> for null writer.
    /// </summary>
    [TestMethod]
    public void WriteChars_WhenWriterIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            System.Text.Encoding.UTF8.WriteChars(new byte[] { 0x68 }, null!);
        });

        Assert.AreEqual("writer", ex.ParamName);
    }
}
