// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Verifies that decoding accepts both uppercase and lowercase hexadecimal digits.
    /// </summary>
    /// <param name="encoded">The percent-encoded input.</param>
    [TestMethod]
    [DataRow("%2F")]
    [DataRow("%2f")]
    public void Decode_WhenHexCaseVaries_ShouldDecodeToSlash(string encoded)
    {
        CollectionAssert.AreEqual(Ascii("/"), PercentEncoding.Decode(encoded.AsSpan()));
    }

    /// <summary>
    /// Verifies that, outside form mode, a literal plus decodes to a plus byte rather than a space.
    /// </summary>
    [TestMethod]
    public void Decode_WhenPlusOutsideFormMode_ShouldReturnPlusByte()
    {
        CollectionAssert.AreEqual(Ascii("a+b"), PercentEncoding.Decode("a+b".AsSpan(), PercentEncodingMode.UriComponent));
    }

    /// <summary>
    /// Verifies that form mode decodes <c>+</c> to a space.
    /// </summary>
    [TestMethod]
    public void Decode_WhenPlusAndFormMode_ShouldReturnSpace()
    {
        CollectionAssert.AreEqual(Ascii("a b"), PercentEncoding.Decode("a+b".AsSpan(), PercentEncodingMode.FormUrlEncoded));
    }

    /// <summary>
    /// Verifies that form mode decodes <c>%2B</c> to a literal plus.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEscapedPlusAndFormMode_ShouldReturnPlus()
    {
        CollectionAssert.AreEqual(Ascii("a+b"), PercentEncoding.Decode("a%2Bb".AsSpan(), PercentEncodingMode.FormUrlEncoded));
    }

    /// <summary>
    /// Verifies that every malformed percent sequence is rejected by default.
    /// </summary>
    /// <param name="malformed">The malformed input.</param>
    [TestMethod]
    [DataRow("%")]
    [DataRow("%A")]
    [DataRow("%GG")]
    [DataRow("%u1234")]
    public void Decode_WhenMalformedPercentAndDefault_ShouldThrowFormatException(string malformed)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = PercentEncoding.Decode(malformed.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that a non-ASCII character in the byte-oriented decoder is rejected.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNonAsciiSource_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = PercentEncoding.Decode("é".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that invalid percent literals pass through as ASCII bytes only when
    /// <see cref="PercentDecodingOptions.AllowInvalidPercentLiterals" /> is set.
    /// </summary>
    /// <param name="input">The relaxed input.</param>
    [TestMethod]
    [DataRow("%")]
    [DataRow("%A")]
    [DataRow("%GG")]
    public void Decode_WhenInvalidPercentLiteralAndRelaxed_ShouldPassThrough(string input)
    {
        byte[] decoded = PercentEncoding.Decode(input.AsSpan(), PercentEncodingMode.UriComponent, PercentDecodingOptions.AllowInvalidPercentLiterals);

        CollectionAssert.AreEqual(Ascii(input), decoded);
    }

    /// <summary>
    /// Verifies that the relaxed option does not change the decoding of valid percent escapes.
    /// </summary>
    [TestMethod]
    public void Decode_WhenValidEscapeAndRelaxed_ShouldDecodeNormally()
    {
        byte[] decoded = PercentEncoding.Decode("a%2Fb".AsSpan(), PercentEncodingMode.UriComponent, PercentDecodingOptions.AllowInvalidPercentLiterals);

        CollectionAssert.AreEqual(Ascii("a/b"), decoded);
    }

    /// <summary>
    /// Verifies that lenient decoding still recovers escaped reserved characters even when the literal form would be
    /// non-canonical for the mode (<c>%2F</c> under <see cref="PercentEncodingMode.PathSegment" /> → <c>/</c>).
    /// </summary>
    [TestMethod]
    public void Decode_ShouldDecodeEscapedReservedCharacters_WhenLiteralWouldBeInvalid()
    {
        byte[] decoded = PercentEncoding.Decode("%2F".AsSpan(), PercentEncodingMode.PathSegment);

        CollectionAssert.AreEqual(new byte[] { (byte)'/' }, decoded);
    }

    /// <summary>
    /// Verifies that lenient decoding accepts a literal reserved character that canonical validation would reject.
    /// </summary>
    [TestMethod]
    public void Decode_ShouldAcceptLiteralReservedCharacters_ThatIsValidRejects()
    {
        byte[] decoded = PercentEncoding.Decode("a#b".AsSpan(), PercentEncodingMode.UriComponent);

        CollectionAssert.AreEqual(Ascii("a#b"), decoded);
        Assert.IsFalse(PercentEncoding.IsValid("a#b".AsSpan(), PercentEncodingMode.UriComponent));
    }

    /// <summary>
    /// Verifies that empty input decodes to an empty array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmpty_ShouldReturnEmptyArray()
    {
        Assert.AreEqual(0, PercentEncoding.Decode(ReadOnlySpan<char>.Empty).Length);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, PercentEncodingMode, PercentDecodingOptions)" />
    /// returns <see langword="false" /> for malformed input without throwing.
    /// </summary>
    /// <param name="malformed">The malformed input.</param>
    [TestMethod]
    [DataRow("%")]
    [DataRow("%A")]
    [DataRow("%GG")]
    [DataRow("é")]
    public void TryDecode_WhenMalformed_ShouldReturnFalse(string malformed)
    {
        Span<byte> destination = new byte[malformed.Length + 8];

        bool success = PercentEncoding.TryDecode(malformed.AsSpan(), destination, out _);

        Assert.IsFalse(success);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, PercentEncodingMode, PercentDecodingOptions)" />
    /// returns <see langword="false" /> and writes zero when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalseAndWriteZero()
    {
        Span<byte> destination = new byte[1];

        bool success = PercentEncoding.TryDecode("abc".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }
}
