// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that <c>=3D</c> decodes to the equals sign.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEscapedEquals_ShouldReturnEqualsByte()
    {
        CollectionAssert.AreEqual(new byte[] { 0x3D }, QuotedPrintable.Decode("=3D".AsSpan()));
    }

    /// <summary>
    /// Verifies that <c>=0C</c> decodes to a form feed.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEscapedFormFeed_ShouldReturnFormFeedByte()
    {
        CollectionAssert.AreEqual(new byte[] { 0x0C }, QuotedPrintable.Decode("=0C".AsSpan()));
    }

    /// <summary>
    /// Verifies that a soft line break is removed from the output.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSoftLineBreak_ShouldRemoveIt()
    {
        CollectionAssert.AreEqual(Ascii("abcdef"), QuotedPrintable.Decode("abc=\r\ndef".AsSpan()));
    }

    /// <summary>
    /// Verifies that a hard CRLF line break decodes to the two bytes <c>0x0D 0x0A</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenHardCrlf_ShouldReturnCrLfBytes()
    {
        CollectionAssert.AreEqual(new byte[] { 0x61, 0x0D, 0x0A, 0x62 }, QuotedPrintable.Decode("a\r\nb".AsSpan()));
    }

    /// <summary>
    /// Verifies that lowercase hex is rejected by default.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLowercaseHexAndDefault_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = QuotedPrintable.Decode("=3d".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that lowercase hex is accepted when <see cref="QuotedPrintableDecodingOptions.AllowLowercaseHex" /> is
    /// set.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLowercaseHexAndAllowed_ShouldDecode()
    {
        byte[] decoded = QuotedPrintable.Decode("=3d".AsSpan(), QuotedPrintableDecodingOptions.AllowLowercaseHex);

        CollectionAssert.AreEqual(new byte[] { 0x3D }, decoded);
    }

    /// <summary>
    /// Verifies that a bare LF is rejected by default.
    /// </summary>
    [TestMethod]
    public void Decode_WhenBareLineFeedAndDefault_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = QuotedPrintable.Decode("a\nb".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that a bare LF is accepted as a hard break when
    /// <see cref="QuotedPrintableDecodingOptions.AllowBareLineFeed" /> is set.
    /// </summary>
    [TestMethod]
    public void Decode_WhenBareLineFeedAndAllowed_ShouldDecode()
    {
        byte[] decoded = QuotedPrintable.Decode("a\nb".AsSpan(), QuotedPrintableDecodingOptions.AllowBareLineFeed);

        CollectionAssert.AreEqual(new byte[] { 0x61, 0x0A, 0x62 }, decoded);
    }

    /// <summary>
    /// Verifies that a soft break using a bare LF (<c>=\n</c>) is accepted when
    /// <see cref="QuotedPrintableDecodingOptions.AllowBareLineFeed" /> is set.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSoftBreakBareLineFeedAndAllowed_ShouldRemoveIt()
    {
        byte[] decoded = QuotedPrintable.Decode("abc=\ndef".AsSpan(), QuotedPrintableDecodingOptions.AllowBareLineFeed);

        CollectionAssert.AreEqual(Ascii("abcdef"), decoded);
    }

    /// <summary>
    /// Verifies that trailing literal whitespace before a hard break is rejected by default.
    /// </summary>
    [TestMethod]
    public void Decode_WhenTrailingWhitespaceAndDefault_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = QuotedPrintable.Decode("abc \r\ndef".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that trailing whitespace is deleted when
    /// <see cref="QuotedPrintableDecodingOptions.IgnoreTrailingWhitespace" /> is set.
    /// </summary>
    [TestMethod]
    public void Decode_WhenTrailingWhitespaceAndIgnored_ShouldDropIt()
    {
        byte[] decoded = QuotedPrintable.Decode("abc \r\ndef".AsSpan(), QuotedPrintableDecodingOptions.IgnoreTrailingWhitespace);

        CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x63, 0x0D, 0x0A, 0x64, 0x65, 0x66 }, decoded);
    }

    /// <summary>
    /// Verifies that interior whitespace before a data escape is preserved.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInteriorWhitespaceBeforeEscape_ShouldPreserveIt()
    {
        CollectionAssert.AreEqual(new byte[] { 0x61, 0x20, 0x3D }, QuotedPrintable.Decode("a =3D".AsSpan()));
    }

    /// <summary>
    /// Verifies that empty input decodes to an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmpty_ShouldReturnEmptyArray()
    {
        Assert.AreEqual(0, QuotedPrintable.Decode(ReadOnlySpan<char>.Empty).Length);
    }

    /// <summary>
    /// Verifies that every malformed input throws <see cref="FormatException" />.
    /// </summary>
    /// <param name="malformed">The malformed Quoted-Printable input.</param>
    [TestMethod]
    [DataRow("=")]
    [DataRow("=A")]
    [DataRow("=GG")]
    [DataRow("=\rX")]
    [DataRow("=\n")]
    [DataRow("a\rb")]
    [DataRow("é")]
    [DataRow("\x01")]
    public void Decode_WhenMalformed_ShouldThrowFormatException(string malformed)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = QuotedPrintable.Decode(malformed.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, QuotedPrintableDecodingOptions)" />
    /// returns <see langword="false" /> for malformed input without throwing.
    /// </summary>
    /// <param name="malformed">The malformed Quoted-Printable input.</param>
    [TestMethod]
    [DataRow("=")]
    [DataRow("=A")]
    [DataRow("=GG")]
    [DataRow("=\rX")]
    [DataRow("=\n")]
    [DataRow("a\rb")]
    public void TryDecode_WhenMalformed_ShouldReturnFalse(string malformed)
    {
        Span<byte> destination = new byte[malformed.Length + 8];

        bool success = QuotedPrintable.TryDecode(malformed.AsSpan(), destination, out _);

        Assert.IsFalse(success);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, QuotedPrintableDecodingOptions)" />
    /// returns <see langword="false" /> and writes zero when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalseAndWriteZero()
    {
        Span<byte> destination = new byte[2];

        bool success = QuotedPrintable.TryDecode("abcdef".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }
}
