// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.StringHelpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Verifies that <see cref="PercentEncoding.EncodeString(string, System.Text.Encoding, PercentEncodingMode)" />
    /// UTF-8 encodes representative non-ASCII characters to their percent-encoded octets.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <param name="expected">The expected percent-encoded UTF-8 octets.</param>
    [TestMethod]
    [DataRow("é", "%C3%A9")]
    [DataRow("雪", "%E9%9B%AA")]
    [DataRow("‽", "%E2%80%BD")]
    public void EncodeString_WhenUtf8NonAscii_ShouldPercentEncodeOctets(string value, string expected)
    {
        Assert.AreEqual(expected, PercentEncoding.EncodeString(value));
    }

    /// <summary>
    /// Verifies that UTF-8 string round-trips through <see cref="PercentEncoding.EncodeString(string, System.Text.Encoding, PercentEncodingMode)" />
    /// and <see cref="PercentEncoding.DecodeString(ReadOnlySpan{char}, System.Text.Encoding, PercentEncodingMode, PercentDecodingOptions)" />.
    /// </summary>
    /// <param name="value">The source string.</param>
    [TestMethod]
    [DataRow("é")]
    [DataRow("雪")]
    [DataRow("‽")]
    [DataRow("a/b?c=d é 雪")]
    public void EncodeDecodeString_WhenUtf8_ShouldRoundTrip(string value)
    {
        string encoded = PercentEncoding.EncodeString(value);
        string decoded = PercentEncoding.DecodeString(encoded.AsSpan());

        Assert.AreEqual(value, decoded);
    }

    /// <summary>
    /// Verifies that an explicit text encoding is honoured by the string helpers.
    /// </summary>
    [TestMethod]
    public void EncodeString_WhenAsciiEncodingSupplied_ShouldUseIt()
    {
        // ASCII encoding maps non-representable characters to '?', which is unreserved-adjacent but encoded here.
        string encoded = PercentEncoding.EncodeString("A B", System.Text.Encoding.ASCII);

        Assert.AreEqual("A%20B", encoded);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.DecodeString(ReadOnlySpan{char}, System.Text.Encoding, PercentEncodingMode, PercentDecodingOptions)" />
    /// follows the supplied decoder's replacement fallback for invalid UTF-8.
    /// </summary>
    [TestMethod]
    public void DecodeString_WhenInvalidUtf8AndReplacementFallback_ShouldUseReplacementCharacter()
    {
        string decoded = PercentEncoding.DecodeString("%FF".AsSpan());

        Assert.AreEqual("�", decoded);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.DecodeString(ReadOnlySpan{char}, System.Text.Encoding, PercentEncodingMode, PercentDecodingOptions)" />
    /// honours a throwing decoder fallback for invalid UTF-8.
    /// </summary>
    [TestMethod]
    public void DecodeString_WhenInvalidUtf8AndThrowingFallback_ShouldThrowDecoderFallbackException()
    {
        var strictUtf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        Assert.ThrowsExactly<System.Text.DecoderFallbackException>(() =>
        {
            _ = PercentEncoding.DecodeString("%FF".AsSpan(), strictUtf8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.EncodeString(string, System.Text.Encoding, PercentEncodingMode)" />
    /// rejects a <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void EncodeString_WhenValueNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = PercentEncoding.EncodeString(null!);
        });
    }
}
