// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.TryDecodeGuid(ReadOnlySpan{char}, out System.Guid, BaseFormatStyles)" /> returns
    /// <see langword="false" /> when the source decodes to a length other than sixteen bytes.
    /// </summary>
    [TestMethod]
    public void TryDecodeGuid_WhenDecodedLengthIsNotSixteen_ShouldReturnFalse()
    {
        bool decoded = Base16.TryDecodeGuid("00", out Guid value);

        Assert.IsFalse(decoded);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecodeGuid(ReadOnlySpan{char}, out System.Guid, BaseFormatStyles)" /> round-
    /// trips a sixteen-byte GUID encoding.
    /// </summary>
    [TestMethod]
    public void TryDecodeGuid_WhenValidGuidEncoding_ShouldReturnTrueAndValue()
    {
        var guid = Guid.NewGuid();
        string encoded = Base16.Encode(guid);

        bool decoded = Base16.TryDecodeGuid(encoded, out Guid value);

        Assert.IsTrue(decoded);
        Assert.AreEqual(guid, value);
    }

    /// <summary>
    /// Verifies that encoding an empty source into a span writes nothing.
    /// </summary>
    [TestMethod]
    public void Encode_ToSpan_WhenSourceEmpty_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base16.Encode(ReadOnlySpan<byte>.Empty, Span<char>.Empty));
    }

    /// <summary>
    /// Verifies that UTF-8 encoding an empty source returns an empty array.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenSourceEmpty_ShouldReturnEmpty()
    {
        Assert.IsEmpty(Base16.EncodeToUtf8(ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<byte> writer = new();

        Assert.AreEqual(0, Base16.EncodeToUtf8(ReadOnlySpan<byte>.Empty, writer));
    }

    /// <summary>
    /// Verifies that attempting to UTF-8 encode an empty source succeeds and writes nothing.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenSourceEmpty_ShouldReturnTrueAndWriteNothing()
    {
        bool encoded = Base16.TryEncodeToUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int written);

        Assert.IsTrue(encoded);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that decoding UTF-8 hex digits resolves the numeric digit branch.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDigitChars_ShouldDecode()
    {
        Span<byte> destination = stackalloc byte[2];

        OperationStatus status = Base16.DecodeFromUtf8("3132"u8, destination, out _, out int written);

        Assert.AreEqual((OperationStatus.Done, 2), (status, written));
    }

    /// <summary>
    /// Verifies that decoding a UTF-8 source containing a non-hex character reports invalid data.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenInvalidChar_ShouldReturnInvalidData()
    {
        Span<byte> destination = stackalloc byte[1];

        OperationStatus status = Base16.DecodeFromUtf8("GG"u8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> returns
    /// <see langword="false" /> for a non-hex character.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenInvalidChar_ShouldReturnFalse()
    {
        Span<byte> destination = stackalloc byte[1];

        Assert.IsFalse(Base16.TryDecode("GG", destination, out _));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> returns
    /// <see langword="false" /> for an odd-length source.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenLengthIsOdd_ShouldReturnFalse()
    {
        Span<byte> destination = stackalloc byte[2];

        Assert.IsFalse(Base16.TryDecode("ABC", destination, out _));
    }

    /// <summary>
    /// Verifies that the variant UTF-8 encode overload returns an empty array for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WithVariant_WhenSourceEmpty_ShouldReturnEmpty()
    {
        Assert.IsEmpty(Base16.EncodeToUtf8(ReadOnlySpan<byte>.Empty, Base16Variant.Upper));
    }
}
