// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public partial class Base85Tests
{
    /// <summary>
    /// Verifies that the offset/count decode overload decodes the addressed subrange, round-tripping the input.
    /// </summary>
    [TestMethod]
    public void Decode_WithOffsetAndCount_ShouldRoundTrip()
    {
        byte[] data = { 1, 2, 3, 4 };
        char[] encoded = Base85.Encode(data).ToCharArray();

        byte[] decoded = Base85.Decode(encoded, 0, encoded.Length);

        CollectionAssert.AreEqual(data, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base85Variant,
    /// BaseFormatStyles)" /> returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] data = { 1, 2, 3, 4 };
        char[] encoded = Base85.Encode(data).ToCharArray();

        Assert.IsFalse(Base85.TryDecode(encoded, Span<byte>.Empty, out _));
    }

    /// <summary>
    /// Verifies that encoding an empty source into a span writes nothing.
    /// </summary>
    [TestMethod]
    public void Encode_ToSpan_WhenSourceEmpty_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base85.Encode(ReadOnlySpan<byte>.Empty, Span<char>.Empty));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> character encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void Encode_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<char> writer = new();

        Assert.AreEqual((0, 0), (Base85.Encode(ReadOnlySpan<byte>.Empty, writer), writer.WrittenCount));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<byte> writer = new();

        Assert.AreEqual(0, Base85.EncodeToUtf8(ReadOnlySpan<byte>.Empty, writer));
    }

    /// <summary>
    /// Verifies that decoding an empty UTF-8 source reports completion.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenSourceEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base85.DecodeFromUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out _, out _);

        Assert.AreEqual(OperationStatus.Done, status);
    }

    /// <summary>
    /// Verifies that Z85 encoding rejects an input whose length is not a multiple of four bytes.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZ85InputNotMultipleOfFour_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.Encode(new byte[] { 1, 2, 3 }, Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that the span encode overload also rejects a Z85 input whose length is not a multiple of four bytes.
    /// </summary>
    [TestMethod]
    public void Encode_ToSpan_WhenZ85InputNotMultipleOfFour_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<char> destination = stackalloc char[32];
            _ = Base85.Encode(new byte[] { 1, 2, 3 }, destination, Base85Variant.Z85);
        });
    }
}
