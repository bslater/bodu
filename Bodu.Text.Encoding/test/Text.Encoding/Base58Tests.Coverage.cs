// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public partial class Base58Tests
{
    /// <summary>
    /// Verifies that decoding an empty UTF-8 source reports completion with nothing consumed or written.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenSourceEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base58.DecodeFromUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out var consumed, out var written);

        Assert.AreEqual((OperationStatus.Done, 0, 0), (status, consumed, written));
    }

    /// <summary>
    /// Verifies that encoding an empty source into a span writes nothing.
    /// </summary>
    [TestMethod]
    public void Encode_ToSpan_WhenSourceEmpty_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base58.Encode(ReadOnlySpan<byte>.Empty, Span<char>.Empty));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> character encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void Encode_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<char> writer = new();

        Assert.AreEqual((0, 0), (Base58.Encode(ReadOnlySpan<byte>.Empty, writer), writer.WrittenCount));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<byte> writer = new();

        Assert.AreEqual(0, Base58.EncodeToUtf8(ReadOnlySpan<byte>.Empty, writer));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base58Variant,
    /// BaseFormatStyles)" /> returns <see langword="false" /> for a character outside the Base58 alphabet.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenCharacterOutsideAlphabet_ShouldReturnFalse()
    {
        Span<byte> destination = stackalloc byte[8];

        Assert.IsFalse(Base58.TryDecode("0OIl", destination, out _));
    }
}
