// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public partial class Base64Tests
{
    /// <summary>
    /// Verifies that decoding an empty UTF-8 source reports completion with nothing consumed or written.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenSourceEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base64.DecodeFromUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int consumed, out int written);

        Assert.AreEqual((OperationStatus.Done, 0, 0), (status, consumed, written));
    }

    /// <summary>
    /// Verifies that the OperationStatus character decode reports completion for an empty source.
    /// </summary>
    [TestMethod]
    public void FromBase64String_WhenSourceEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base64.FromBase64String(ReadOnlySpan<char>.Empty, Span<byte>.Empty, out int consumed, out int written);

        Assert.AreEqual((OperationStatus.Done, 0, 0), (status, consumed, written));
    }

    /// <summary>
    /// Verifies that attempting to UTF-8 encode an empty source succeeds and writes nothing.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenSourceEmpty_ShouldReturnTrueAndWriteNothing()
    {
        bool encoded = Base64.TryEncodeToUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int written);

        Assert.IsTrue(encoded);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that interior whitespace is skipped when decoding with the ignore-whitespace style.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenWhitespaceWithIgnoreStyle_ShouldDecodeAroundIt()
    {
        Span<byte> destination = stackalloc byte[3];

        OperationStatus status = Base64.DecodeFromUtf8(
            "TW Fu"u8, destination, out _, out int written, Base64Variant.Standard, BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual((OperationStatus.Done, 3), (status, written));
    }

    /// <summary>
    /// Verifies that a non-canonical final group with non-zero unused bits is rejected when canonical encoding is
    /// required.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenNonCanonicalAndCanonicalRequired_ShouldReturnInvalidData()
    {
        Span<byte> destination = stackalloc byte[1];

        OperationStatus status = Base64.DecodeFromUtf8(
            "TR"u8, destination, out _, out _, Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding | BaseFormatStyles.RequireCanonicalEncoding);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that the BCL-compatible UTF-8 decode alias trims its result when fewer bytes are produced than the
    /// estimated maximum.
    /// </summary>
    [TestMethod]
    public void FromBase64String_WhenDecodedShorterThanEstimate_ShouldTrim()
    {
        byte[] bytes = Base64.FromBase64String("TQ=="u8);

        Assert.AreEqual(1, bytes.Length);
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> character encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void Encode_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<char> writer = new();

        int written = Base64.Encode(ReadOnlySpan<byte>.Empty, writer);

        Assert.AreEqual((0, 0), (written, writer.WrittenCount));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 encode overload writes the encoded bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ToBufferWriter_ShouldWriteEncodedBytes()
    {
        ArrayBufferWriter<byte> writer = new();

        int written = Base64.EncodeToUtf8("Man"u8, writer);

        Assert.AreEqual("TWFu", System.Text.Encoding.ASCII.GetString(writer.WrittenSpan));
        Assert.AreEqual(4, written);
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 encode overload writes nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ToBufferWriter_WhenSourceEmpty_ShouldWriteNothing()
    {
        ArrayBufferWriter<byte> writer = new();

        Assert.AreEqual(0, Base64.EncodeToUtf8(ReadOnlySpan<byte>.Empty, writer));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsValid(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> rejects a source
    /// with a data character following a padding character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSymbolFollowsPadding_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("AB=C"));
    }
}
