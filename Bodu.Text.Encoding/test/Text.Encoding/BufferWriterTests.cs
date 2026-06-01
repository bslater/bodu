// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Tests for the <see cref="IBufferWriter{T}" /> based encode overloads across every encoding. Verifies that each
/// encoder writes the same output it would produce via the string-returning Encode and correctly advances the
/// writer.
/// </summary>
[TestClass]
public sealed class BufferWriterTests
{

    private static readonly byte[] Payload = System.Text.Encoding.ASCII.GetBytes("Hello, World!");

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, BaseFormattingOptions)" />
    /// writes the same characters as the string-returning Encode and advances the writer by the written count.
    /// </summary>
    [TestMethod]
    public void Base16_Encode_IntoBufferWriter_ShouldMatchStringOutput()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base16.Encode(Payload, writer);

        Assert.AreEqual(Payload.Length * 2, written);
        Assert.AreEqual(Base16.Encode(Payload), new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that an empty payload writes zero characters to the buffer writer.
    /// </summary>
    [TestMethod]
    public void Base16_Encode_IntoBufferWriterWithEmptyPayload_ShouldWriteZeroChars()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base16.Encode([], writer);

        Assert.AreEqual(0, written);
        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that passing a null buffer writer throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Base16_Encode_IntoBufferWriterWithNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base16.Encode(Payload, (IBufferWriter<char>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, BaseFormattingOptions)" />
    /// writes the same bytes as the array-returning EncodeToUtf8 and advances the writer by the written count.
    /// </summary>
    [TestMethod]
    public void Base16_EncodeToUtf8_IntoBufferWriter_ShouldMatchArrayOutput()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base16.EncodeToUtf8(Payload, writer);
        var expected = Base16.EncodeToUtf8(Payload);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base32Variant, BaseFormattingOptions)" />
    /// writes the same characters as the string-returning Encode.
    /// </summary>
    [TestMethod]
    public void Base32_Encode_IntoBufferWriter_ShouldMatchStringOutput()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base32.Encode(Payload, writer);

        Assert.AreEqual(Base32.GetEncodedLength(Payload.Length), written);
        Assert.AreEqual(Base32.Encode(Payload), new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base32Variant, BaseFormattingOptions)" />
    /// writes the same UTF-8 bytes as the byte-array EncodeToUtf8.
    /// </summary>
    [TestMethod]
    public void Base32_EncodeToUtf8_IntoBufferWriter_ShouldMatchArrayOutput()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base32.EncodeToUtf8(Payload, writer);
        var expected = Base32.EncodeToUtf8(Payload);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base58Variant)" /> writes the
    /// same characters as the string-returning Encode.
    /// </summary>
    [TestMethod]
    public void Base58_Encode_IntoBufferWriter_ShouldMatchStringOutput()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base58.Encode(Payload, writer);

        Assert.AreEqual(Base58.Encode(Payload).Length, written);
        Assert.AreEqual(Base58.Encode(Payload), new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base58Variant)" /> writes
    /// the same UTF-8 bytes as the byte-array EncodeToUtf8.
    /// </summary>
    [TestMethod]
    public void Base58_EncodeToUtf8_IntoBufferWriter_ShouldMatchArrayOutput()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base58.EncodeToUtf8(Payload, writer);
        var expected = Base58.EncodeToUtf8(Payload);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base58.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base58Variant)" /> writes
    /// zero bytes and does not advance the writer when the source is empty.
    /// </summary>
    [TestMethod]
    public void Base58_EncodeToUtf8_IntoBufferWriterWithEmptyPayload_ShouldWriteZeroBytes()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base58.EncodeToUtf8([], writer);

        Assert.AreEqual(0, written);
        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base58Variant)" /> with a
    /// <see langword="null" /> writer throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Base58_EncodeToUtf8_IntoNullBufferWriter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base58.EncodeToUtf8(Payload, (IBufferWriter<byte>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base64Variant, BaseFormattingOptions)" />
    /// writes the same characters as the string-returning Encode.
    /// </summary>
    [TestMethod]
    public void Base64_Encode_IntoBufferWriter_ShouldMatchStringOutput()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base64.Encode(Payload, writer);

        Assert.AreEqual(Base64.Encode(Payload).Length, written);
        Assert.AreEqual(Base64.Encode(Payload), new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base64Variant, BaseFormattingOptions)" />
    /// writes the same UTF-8 bytes as the byte-array EncodeToUtf8.
    /// </summary>
    [TestMethod]
    public void Base64_EncodeToUtf8_IntoBufferWriter_ShouldMatchArrayOutput()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base64.EncodeToUtf8(Payload, writer);
        var expected = Base64.EncodeToUtf8(Payload);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base85Variant, BaseFormattingOptions)" />
    /// writes the same characters as the string-returning Encode.
    /// </summary>
    [TestMethod]
    public void Base85_Encode_IntoBufferWriter_ShouldMatchStringOutput()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base85.Encode(Payload, writer);

        Assert.AreEqual(Base85.Encode(Payload).Length, written);
        Assert.AreEqual(Base85.Encode(Payload), new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base85Variant, BaseFormattingOptions)" />
    /// emits the Ascii85 delimiter pair when <see cref="BaseFormattingOptions.IncludePrefix" /> is set, matching the
    /// string-returning Encode with the same option.
    /// </summary>
    [TestMethod]
    public void Base85_Encode_IntoBufferWriterWithDelimiters_ShouldEmitDelimitedOutput()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base85.Encode(Payload, writer, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        var expected = Base85.Encode(Payload, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual(expected.Length, written);
        Assert.AreEqual(expected, new string(writer.WrittenSpan));
        Assert.IsTrue(expected.StartsWith("<~", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base85Variant)" /> writes
    /// the same UTF-8 bytes as the byte-array EncodeToUtf8 for the default Ascii85 variant.
    /// </summary>
    [TestMethod]
    public void Base85_EncodeToUtf8_IntoBufferWriter_ShouldMatchArrayOutput()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base85.EncodeToUtf8(Payload, writer);
        var expected = Base85.EncodeToUtf8(Payload);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base85Variant)" /> writes
    /// zero bytes and does not advance the writer when the source is empty.
    /// </summary>
    [TestMethod]
    public void Base85_EncodeToUtf8_IntoBufferWriterWithEmptyPayload_ShouldWriteZeroBytes()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base85.EncodeToUtf8([], writer);

        Assert.AreEqual(0, written);
        Assert.AreEqual(0, writer.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base85Variant)" /> with a
    /// <see langword="null" /> writer throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Base85_EncodeToUtf8_IntoNullBufferWriter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base85.EncodeToUtf8(Payload, (IBufferWriter<byte>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, Base85Variant)" /> emits
    /// the Z85 variant output when that variant is selected (4-byte aligned input).
    /// </summary>
    [TestMethod]
    public void Base85_EncodeToUtf8_WithZ85Variant_ShouldMatchArrayOutput()
    {
        // Z85 requires 4-byte aligned input.
        var aligned = new byte[] { 0x86, 0x4F, 0xD2, 0x6F, 0xB5, 0x59, 0xF7, 0x5B };
        ArrayBufferWriter<byte> writer = new();

        var written = Base85.EncodeToUtf8(aligned, writer, Base85Variant.Z85);
        var expected = Base85.EncodeToUtf8(aligned, Base85Variant.Z85);

        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, writer.WrittenSpan.ToArray());
    }

}
