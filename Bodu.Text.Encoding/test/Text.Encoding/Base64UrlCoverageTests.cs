// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64UrlCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Coverage tests for the <see cref="Base64Url" /> wrapper. The coverage report flagged
/// <see cref="Base64Url.Decode(ReadOnlySpan{char})" />, <see cref="Base64Url.Encode(ReadOnlySpan{byte})" />,
/// <see cref="Base64Url.EncodeToUtf8(ReadOnlySpan{byte})" />, <see cref="Base64Url.GetMaxDecodedLength(int)" />, and
/// <see cref="Base64Url.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> at 0% block and line coverage.
/// </summary>
[TestClass]
public sealed class Base64UrlCoverageTests
{

    private static readonly byte[] Payload = [0xFB, 0xFF, 0xFE, 0x00];

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(ReadOnlySpan{char})" /> round-trips an encoded payload.
    /// </summary>
    [TestMethod]
    public void Decode_CharSpan_ShouldRoundTrip()
    {
        string encoded = Base64Url.Encode(Payload);

        byte[] decoded = Base64Url.Decode(encoded.AsSpan());

        CollectionAssert.AreEqual(Payload, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(ReadOnlySpan{byte})" /> round-trips a UTF-8 encoded payload.
    /// </summary>
    [TestMethod]
    public void Decode_Utf8_ShouldRoundTrip()
    {
        byte[] utf8 = Base64Url.EncodeToUtf8(Payload);

        byte[] decoded = Base64Url.Decode(utf8.AsSpan());

        CollectionAssert.AreEqual(Payload, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void Decode_Utf8WithEmpty_ShouldReturnEmptyArray()
    {
        byte[] decoded = Base64Url.Decode(ReadOnlySpan<byte>.Empty);

        Assert.IsEmpty(decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(ReadOnlySpan{byte})" /> throws <see cref="FormatException" /> for
    /// invalid input bytes.
    /// </summary>
    [TestMethod]
    public void Decode_Utf8WithInvalidByte_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64Url.Decode([0xFF, 0xFF, 0xFF, 0xFF]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Encode(byte[])" /> produces the same URL-safe output as the equivalent
    /// variant call.
    /// </summary>
    [TestMethod]
    public void Encode_ArrayOverload_ShouldMatchBase64UrlSafeVariant() => Assert.AreEqual(Base64.Encode(Payload, Base64Variant.UrlSafe), Base64Url.Encode(Payload));

    /// <summary>
    /// Verifies that <see cref="Base64Url.Encode(ReadOnlySpan{byte})" /> produces the same URL-safe output as the
    /// equivalent <c>Base64.Encode(..., Base64Variant.UrlSafe)</c> call.
    /// </summary>
    [TestMethod]
    public void Encode_Span_ShouldMatchBase64UrlSafeVariant()
    {
        string encoded = Base64Url.Encode(Payload.AsSpan());

        Assert.AreEqual(Base64.Encode(Payload, Base64Variant.UrlSafe), encoded);
        Assert.IsFalse(encoded.Contains('+', StringComparison.Ordinal));
        Assert.IsFalse(encoded.Contains('/', StringComparison.Ordinal));
        Assert.IsFalse(encoded.Contains('=', StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.EncodeToUtf8(ReadOnlySpan{byte})" /> emits ASCII bytes of the URL-safe form.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_Span_ShouldMatchAsciiOfUrlSafeForm()
    {
        byte[] utf8 = Base64Url.EncodeToUtf8(Payload.AsSpan());

        Assert.AreEqual(Base64Url.Encode(Payload.AsSpan()), System.Text.Encoding.ASCII.GetString(utf8));
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.GetMaxDecodedLength(int)" /> delegates to
    /// <see cref="Base64.GetMaxDecodedLength(int)" />.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_ShouldDelegateToBase64()
    {
        Assert.AreEqual(Base64.GetMaxDecodedLength(20), Base64Url.GetMaxDecodedLength(20));
        Assert.AreEqual(Base64.GetMaxDecodedLength(0), Base64Url.GetMaxDecodedLength(0));
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes the
    /// expected URL-safe bytes when the destination is sized correctly.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_ShouldWriteUrlSafeBytes()
    {
        byte[] expected = Base64Url.EncodeToUtf8(Payload);
        byte[] destination = new byte[expected.Length];

        bool ok = Base64Url.TryEncodeToUtf8(Payload, destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(expected.Length, bytesWritten);
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> returns
    /// <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WithTooSmallDestination_ShouldReturnFalse()
    {
        byte[] destination = new byte[1];

        bool ok = Base64Url.TryEncodeToUtf8(Payload, destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

}
