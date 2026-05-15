// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64UrlTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base64Url" /> — the first-class URL-safe Base64 wrapper.
/// </summary>
[TestClass]
public sealed class Base64UrlTests
{

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(ReadOnlySpan{byte})" /> decodes a UTF-8 input directly without
    /// allocating an intermediate string.
    /// </summary>
    [TestMethod]
    public void Decode_FromUtf8_ShouldRoundTrip()
    {
        byte[] original = System.Text.Encoding.ASCII.GetBytes("foobar");
        byte[] encodedUtf8 = Base64Url.EncodeToUtf8(original);

        byte[] decoded = Base64Url.Decode(encodedUtf8);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> also accepts URL-safe input that includes canonical
    /// padding — the lenient AllowMissingPadding semantics permit either form.
    /// </summary>
    [TestMethod]
    public void Decode_ShouldAcceptPaddedUrlSafeInput()
    {
        byte[] expected = System.Text.Encoding.ASCII.GetBytes("foob");

        byte[] withoutPadding = Base64Url.Decode("Zm9vYg");
        byte[] withPadding = Base64Url.Decode("Zm9vYg==");

        CollectionAssert.AreEqual(expected, withoutPadding);
        CollectionAssert.AreEqual(expected, withPadding);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> recovers the original bytes from an unpadded URL-safe
    /// input (the JWT / OAuth convention).
    /// </summary>
    [TestMethod]
    public void Decode_ShouldAcceptUnpaddedInput()
    {
        byte[] expected = new byte[] { 0xFB, 0xFF, 0xFE };

        byte[] actual = Base64Url.Decode("-__-");

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> reproduces a real JWT header decoding scenario —
    /// URL-safe alphabet, padding omitted.
    /// </summary>
    [TestMethod]
    public void Decode_ShouldDecodeRealJwtHeaderSegment()
    {
        // Header segment of a JWT — {"alg":"HS256","typ":"JWT"} encoded URL-safe, no padding.
        const string segment = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        byte[] decoded = Base64Url.Decode(segment);
        string json = System.Text.Encoding.UTF8.GetString(decoded);

        Assert.AreEqual("{\"alg\":\"HS256\",\"typ\":\"JWT\"}", json);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> rejects characters from the Standard alphabet that are
    /// not part of the URL-safe alphabet.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputContainsStandardOnlyCharacter_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = Base64Url.Decode("Zm+v"));
        Assert.ThrowsExactly<FormatException>(() => _ = Base64Url.Decode("Zm/v"));
    }
    /// <summary>
    /// Verifies that <see cref="Base64Url.Encode(byte[])" /> produces the URL-safe alphabet output of the underlying
    /// <see cref="Base64" /> variant, with padding omitted by default.
    /// </summary>
    [TestMethod]
    public void Encode_ShouldProduceUrlSafeOutputWithoutPadding()
    {
        byte[] bytes = new byte[] { 0xFB, 0xFF, 0xFE };

        string actual = Base64Url.Encode(bytes);

        Assert.AreEqual("-__-", actual);
        Assert.IsFalse(actual.Contains('='));
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.IsValid(ReadOnlySpan{char})" /> accepts URL-safe input and rejects
    /// Standard alphabet characters that are not in the URL-safe alphabet.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptUrlSafeAndRejectStandardAlphabet()
    {
        Assert.IsTrue(Base64Url.IsValid("-__-".AsSpan()));
        Assert.IsTrue(Base64Url.IsValid("Zm9vYg".AsSpan()));
        Assert.IsFalse(Base64Url.IsValid("+//+".AsSpan()), "URL-safe must reject the Standard +/ chars.");
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.TryEncode" /> and <see cref="Base64Url.TryDecode" /> round-trip through a
    /// pre-sized span without allocation on the success path.
    /// </summary>
    [TestMethod]
    public void TryEncodeAndTryDecode_ShouldRoundTripThroughSpans()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        Span<char> charBuffer = stackalloc char[Base64Url.GetEncodedLength(original.Length)];
        Span<byte> byteBuffer = stackalloc byte[original.Length];

        Assert.IsTrue(Base64Url.TryEncode(original, charBuffer, out int charsWritten));
        Assert.IsTrue(Base64Url.TryDecode(charBuffer[..charsWritten], byteBuffer, out int bytesWritten));

        Assert.AreEqual(original.Length, bytesWritten);
        Assert.IsTrue(original.AsSpan().SequenceEqual(byteBuffer[..bytesWritten]));
    }

}
