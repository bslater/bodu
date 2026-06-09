// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public partial class Base32Tests
{
    /// <summary>
    /// Verifies that decoding an empty UTF-8 source reports completion with nothing consumed or written.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenSourceEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base32.DecodeFromUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out var consumed, out var written);

        Assert.AreEqual((OperationStatus.Done, 0, 0), (status, consumed, written));
    }

    /// <summary>
    /// Verifies that UTF-8 encoding an empty source returns an empty array.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenSourceEmpty_ShouldReturnEmpty()
    {
        var encoded = Base32.EncodeToUtf8(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, encoded.Length);
    }

    /// <summary>
    /// Verifies that attempting to UTF-8 encode an empty source succeeds and writes nothing.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenSourceEmpty_ShouldReturnTrueAndWriteNothing()
    {
        var encoded = Base32.TryEncodeToUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out var written);

        Assert.IsTrue(encoded);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> character encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void Encode_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base32.Encode(ReadOnlySpan<byte>.Empty, writer);

        Assert.AreEqual((0, 0), (written, writer.WrittenCount));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 encode overload writes the encoded bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ToBufferWriter_ShouldWriteEncodedBytes()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base32.EncodeToUtf8("foob"u8, writer);

        Assert.IsTrue(written > 0);
        Assert.AreEqual(written, writer.WrittenCount);
    }
}
