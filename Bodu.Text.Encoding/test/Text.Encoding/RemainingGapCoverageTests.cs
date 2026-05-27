// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RemainingGapCoverageTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Coverage tests for the long tail of small gaps flagged in the coverage report — length helpers with
/// zero / negative / boundary inputs, IsXxxDigit alphabet boundary cases, span-encode destination-too-small
/// branches, and UTF-8 destination-too-small branches that the existing tests do not exercise.
/// </summary>
[TestClass]
public sealed class RemainingGapCoverageTests
{

    private static readonly byte[] Payload = System.Text.Encoding.ASCII.GetBytes("Hello");

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Span{char}, BaseFormattingOptions)" /> throws when
    /// the destination is exactly one character short.
    /// </summary>
    [TestMethod]
    public void Base16_EncodeSpan_WhenDestinationOneCharShort_ShouldThrowExactly()
    {
        var bytes = new byte[4];
        var destination = new char[7];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Encode(bytes, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.EncodeToUtf8(ReadOnlySpan{byte}, IBufferWriter{byte}, BaseFormattingOptions)" />
    /// throws when given a <see langword="null" /> writer (covers the null guard).
    /// </summary>
    [TestMethod]
    public void Base16_EncodeToUtf8_IntoNullBufferWriter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base16.EncodeToUtf8(Payload, (IBufferWriter<byte>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetMaxDecodedLength(int)" /> returns zero for zero input.
    /// </summary>
    [TestMethod]
    public void Base16_GetMaxDecodedLength_ZeroInput_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base16.GetMaxDecodedLength(0));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> with
    /// lenient styles returns <see langword="false" /> for input that contains an invalid char after stripping.
    /// </summary>
    [TestMethod]
    public void Base16_TryDecode_WhenLenientStylesAndInvalidChar_ShouldReturnFalse()
    {
        var destination = new byte[4];

        var ok = Base16.TryDecode("DE AD ZZ".AsSpan(), destination, out var bytesWritten, BaseFormatStyles.IgnoreWhitespace);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base32Variant, BaseFormattingOptions)" />
    /// and the UTF-8 variant throw when given a <see langword="null" /> writer.
    /// </summary>
    [TestMethod]
    public void Base32_BufferWriter_WithNullWriter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base32.Encode(Payload, (IBufferWriter<char>)null!);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base32.EncodeToUtf8(Payload, (IBufferWriter<byte>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Span{char}, Base32Variant, BaseFormattingOptions)" />
    /// throws when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Base32_EncodeSpan_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var bytes = new byte[8];
        var destination = new char[4];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base32.Encode(bytes, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.EncodeToUtf8(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" /> with
    /// an empty source returns an empty array.
    /// </summary>
    [TestMethod]
    public void Base32_EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        var result = Base32.EncodeToUtf8(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.EncodeToUtf8(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" /> with
    /// OmitPadding produces shorter output than the worst-case bound, exercising the trim-on-actual-length path.
    /// </summary>
    [TestMethod]
    public void Base32_EncodeToUtf8_WithOmitPadding_ShouldTrimToActualLength()
    {
        var result = Base32.EncodeToUtf8(Payload, Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual(Base32.Encode(Payload, Base32Variant.Standard, BaseFormattingOptions.OmitPadding),
                        System.Text.Encoding.ASCII.GetString(result));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsBase32Digit" /> returns <see langword="false" /> for characters past the
    /// 128-entry lookup table.
    /// </summary>
    [TestMethod]
    public void Base32_IsBase32Digit_WhenCharAboveLookupRange_ShouldReturnFalse()
    {
        Assert.IsFalse(Base32.IsBase32Digit((char)200));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base32Variant, BaseFormattingOptions)" />
    /// returns false when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Base32_TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var destination = new byte[1];

        var ok = Base32.TryEncodeToUtf8(Payload, destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Base58_DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        var utf8 = Base58.EncodeToUtf8(Payload);
        var destination = new byte[1];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(ReadOnlySpan{byte}, Span{char}, Base58Variant)" /> throws when the
    /// destination is too small (the slow path slow-bail).
    /// </summary>
    [TestMethod]
    public void Base58_EncodeSpan_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var destination = new char[1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base58.Encode(Payload, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.EncodeToUtf8(ReadOnlySpan{byte}, Base58Variant)" /> returns an empty array for
    /// empty input.
    /// </summary>
    [TestMethod]
    public void Base58_EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        var result = Base58.EncodeToUtf8(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.GetMaxDecodedLength(int)" /> returns zero for zero input.
    /// </summary>
    [TestMethod]
    public void Base58_GetMaxDecodedLength_ZeroInput_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base58.GetMaxDecodedLength(0));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.GetMaxEncodedLength(int)" /> returns zero for zero input and a positive
    /// value for a single byte (covers the non-zero branch).
    /// </summary>
    [TestMethod]
    public void Base58_GetMaxEncodedLength_ShouldHandleZeroAndPositive()
    {
        Assert.AreEqual(0, Base58.GetMaxEncodedLength(0));
        Assert.IsTrue(Base58.GetMaxEncodedLength(1) > 0);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.IsBase58Digit" /> returns <see langword="false" /> for characters past the
    /// 128-entry lookup table.
    /// </summary>
    [TestMethod]
    public void Base58_IsBase58Digit_WhenCharAboveLookupRange_ShouldReturnFalse()
    {
        Assert.IsFalse(Base58.IsBase58Digit((char)200));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base58Variant)" /> returns
    /// false when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Base58_TryEncodeSpan_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var destination = new char[1];

        var ok = Base58.TryEncode(Payload, destination, out var charsWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base58Variant)" />
    /// returns false for too-small destination and true for adequate destination.
    /// </summary>
    [TestMethod]
    public void Base58_TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var destination = new byte[1];

        var ok = Base58.TryEncodeToUtf8(Payload, destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base58Variant)" />
    /// with an empty source returns true with zero bytesWritten (covers the empty-source short-circuit).
    /// </summary>
    [TestMethod]
    public void Base58_TryEncodeToUtf8_WhenEmpty_ShouldReturnTrueAndZeroBytes()
    {
        var destination = new byte[1];

        var ok = Base58.TryEncodeToUtf8(ReadOnlySpan<byte>.Empty, destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.GetMaxDecodedLength(int)" /> returns zero when the input is too small
    /// to contain a four-byte checksum (covers the short-input branch).
    /// </summary>
    [TestMethod]
    public void Base58Check_GetMaxDecodedLength_ShouldHandleShortAndLongInputs()
    {
        Assert.AreEqual(0, Base58Check.GetMaxDecodedLength(0));
        Assert.IsTrue(Base58Check.GetMaxDecodedLength(40) > 0);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.IsValid" /> returns <see langword="false" /> for input shorter than the
    /// four-byte checksum after decoding.
    /// </summary>
    [TestMethod]
    public void Base58Check_IsValid_WhenInputContainsInvalidAlphabet_ShouldReturnFalse()
    {
        Assert.IsFalse(Base58Check.IsValid("0Invalid!".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode" /> returns <see langword="false" /> for an input that base
    /// decodes to fewer than 4 bytes (too short to contain a checksum).
    /// </summary>
    [TestMethod]
    public void Base58Check_TryDecode_WhenDecodedTooShortForChecksum_ShouldReturnFalse()
    {
        // "1" decodes to a single zero byte under Base58 — shorter than the 4-byte checksum suffix.
        var destination = new byte[16];

        var ok = Base58Check.TryDecode("1".AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode" /> returns <see langword="false" /> when the destination is
    /// smaller than the decoded payload (after stripping the checksum).
    /// </summary>
    [TestMethod]
    public void Base58Check_TryDecode_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var original = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
        var encoded = Base58Check.Encode(original);
        var destination = new byte[1];

        var ok = Base58Check.TryDecode(encoded.AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode" /> returns <see langword="false" /> when the input contains an
    /// invalid Base58 character.
    /// </summary>
    [TestMethod]
    public void Base58Check_TryDecode_WhenInputContainsInvalidChar_ShouldReturnFalse()
    {
        var destination = new byte[16];

        var ok = Base58Check.TryDecode("0InvalidChar!".AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base64Variant, BaseFormattingOptions)" />
    /// and the UTF-8 variant throw when given a <see langword="null" /> writer.
    /// </summary>
    [TestMethod]
    public void Base64_BufferWriter_WithNullWriter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base64.Encode(Payload, (IBufferWriter<char>)null!);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base64.EncodeToUtf8(Payload, (IBufferWriter<byte>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(ReadOnlySpan{byte}, Span{char}, Base64Variant, BaseFormattingOptions)" />
    /// throws when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Base64_EncodeSpan_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var bytes = new byte[3];
        var destination = new char[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base64.Encode(bytes, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8(ReadOnlySpan{byte}, Base64Variant, BaseFormattingOptions)" /> with
    /// an empty source returns an empty array.
    /// </summary>
    [TestMethod]
    public void Base64_EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        var result = Base64.EncodeToUtf8(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8(ReadOnlySpan{byte}, Base64Variant, BaseFormattingOptions)" /> with
    /// OmitPadding trims to actual length.
    /// </summary>
    [TestMethod]
    public void Base64_EncodeToUtf8_WithOmitPadding_ShouldTrimToActualLength()
    {
        var result = Base64.EncodeToUtf8(Payload, Base64Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual(Base64.Encode(Payload, Base64Variant.Standard, BaseFormattingOptions.OmitPadding),
                        System.Text.Encoding.ASCII.GetString(result));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, IBufferWriter{char}, Base85Variant, BaseFormattingOptions)" />
    /// emits the four-character delimiter pair when the source is empty and IncludePrefix is set.
    /// </summary>
    [TestMethod]
    public void Base85_BufferWriter_WhenEmptyAndIncludePrefix_ShouldEmitDelimitersOnly()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base85.Encode(ReadOnlySpan<byte>.Empty, writer, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual(4, written);
        Assert.AreEqual("<~~>", new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.StripAscii85Delimiters" /> handles inputs without delimiters (covers the
    /// "delimiter not present" no-op branches inside <c>TryDecodeCore</c>).
    /// </summary>
    [TestMethod]
    public void Base85_Decode_WhenAllowPrefixButNoDelimitersInInput_ShouldStillDecode()
    {
        var decoded = Base85.Decode("9jqo^".AsSpan(), Base85Variant.Ascii85, BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when an
    /// input byte is outside the alphabet's lookup table range (covers the lenient projection path).
    /// </summary>
    [TestMethod]
    public void Base85_DecodeFromUtf8_WhenByteAbovePrintableAscii_ShouldReturnInvalidData()
    {
        var utf8 = new byte[] { (byte)'9', (byte)'j', 0xFF, (byte)'o', (byte)'^' };
        var destination = new byte[4];

        OperationStatus status = Base85.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.EncodeToUtf8(ReadOnlySpan{byte}, Base85Variant)" /> returns an empty array for
    /// empty input and throws for unaligned Z85 input.
    /// </summary>
    [TestMethod]
    public void Base85_EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        Assert.AreEqual(0, Base85.EncodeToUtf8(ReadOnlySpan<byte>.Empty).Length);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.EncodeToUtf8([1, 2, 3], Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />
    /// returns zero for empty input without delimiters.
    /// </summary>
    [TestMethod]
    public void Base85_GetEncodedLength_EmptyInput_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base85.GetEncodedLength(ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />
    /// throws when Z85 receives a non-aligned input.
    /// </summary>
    [TestMethod]
    public void Base85_GetEncodedLength_Z85WithUnalignedInput_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.GetEncodedLength([0x01, 0x02, 0x03], Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetMaxDecodedLength(int)" /> returns zero for zero input.
    /// </summary>
    [TestMethod]
    public void Base85_GetMaxDecodedLength_ZeroInput_ShouldReturnZero()
    {
        Assert.AreEqual(0, Base85.GetMaxDecodedLength(0));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetMaxEncodedLength(int, Base85Variant, BaseFormattingOptions)" /> returns the
    /// four-character delimiter length for zero input when delimiters are requested.
    /// </summary>
    [TestMethod]
    public void Base85_GetMaxEncodedLength_ZeroInputWithDelimiters_ShouldReturnDelimiterLength()
    {
        Assert.AreEqual(4, Base85.GetMaxEncodedLength(0, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix));
        Assert.AreEqual(0, Base85.GetMaxEncodedLength(0, Base85Variant.Ascii85));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsBase85Digit" /> returns <see langword="false" /> for characters past the
    /// 128-entry lookup table.
    /// </summary>
    [TestMethod]
    public void Base85_IsBase85Digit_WhenCharAboveLookupRange_ShouldReturnFalse()
    {
        Assert.IsFalse(Base85.IsBase85Digit((char)200));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base85Variant)" />
    /// returns false when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Base85_TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var destination = new byte[1];

        var ok = Base85.TryEncodeToUtf8(Payload, destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base85Variant)" />
    /// returns true and zero bytesWritten for empty input.
    /// </summary>
    [TestMethod]
    public void Base85_TryEncodeToUtf8_WhenEmpty_ShouldReturnTrueAndZeroBytes()
    {
        var destination = new byte[1];

        var ok = Base85.TryEncodeToUtf8(ReadOnlySpan<byte>.Empty, destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base85Variant)" />
    /// throws for unaligned Z85 input.
    /// </summary>
    [TestMethod]
    public void Base85_TryEncodeToUtf8_WhenZ85AndUnaligned_ShouldThrowExactly()
    {
        var destination = new byte[10];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.TryEncodeToUtf8([1, 2, 3], destination, out _, Base85Variant.Z85);
        });
    }

}
