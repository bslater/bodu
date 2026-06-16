// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingExtensionsTests.Base64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class BinaryEncodingExtensionsTests
{
    /// <summary>
    /// A canonical payload used as the reference input across Base64 encode/decode tests.
    /// </summary>
    private static readonly byte[] Payload = System.Text.Encoding.UTF8.GetBytes("Hello, World!");

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.GetBase64EncodedLength(ReadOnlySpan{byte})" /> returns
    /// the same value as <see cref="System.Convert.ToBase64String(byte[])" /> string length.
    /// </summary>
    [TestMethod]
    public void GetBase64EncodedLength_WhenInvoked_ShouldMatchConvertToBase64StringLength()
    {
        int actual = ((ReadOnlySpan<byte>)Payload).GetBase64EncodedLength();

        Assert.AreEqual(Convert.ToBase64String(Payload).Length, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.GetBase64EncodedLength(ReadOnlySpan{byte})" /> returns
    /// zero for an empty input span.
    /// </summary>
    [TestMethod]
    public void GetBase64EncodedLength_WhenSpanIsEmpty_ShouldReturnZero() => Assert.AreEqual(0, ReadOnlySpan<byte>.Empty.GetBase64EncodedLength());

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryEncodeBase64ToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// produces UTF-8 bytes that round-trip back to the canonical Base64 string when decoded as ASCII.
    /// </summary>
    [TestMethod]
    public void TryEncodeBase64ToUtf8_WhenDestinationFits_ShouldWriteUtf8BytesEqualToBase64String()
    {
        Span<byte> destination = new byte[Payload.Length * 2];

        bool ok = ((ReadOnlySpan<byte>)Payload).TryEncodeBase64ToUtf8(destination, out int written);

        Assert.IsTrue(ok);
        string asAscii = System.Text.Encoding.ASCII.GetString(destination.Slice(0, written));
        Assert.AreEqual(Convert.ToBase64String(Payload), asAscii);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryEncodeBase64ToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeBase64ToUtf8_WhenDestinationIsTooSmall_ShouldReturnFalse()
    {
        byte[] backing = new byte[1];

        bool ok = ((ReadOnlySpan<byte>)Payload).TryEncodeBase64ToUtf8(backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryDecodeBase64FromUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// decodes back to the original payload when the input is a valid UTF-8 Base64 sequence.
    /// </summary>
    [TestMethod]
    public void TryDecodeBase64FromUtf8_WhenInputIsValid_ShouldRoundTrip()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(Payload));
        Span<byte> destination = new byte[Payload.Length];

        bool ok = ((ReadOnlySpan<byte>)utf8).TryDecodeBase64FromUtf8(destination, out int written);

        Assert.IsTrue(ok);
        Assert.AreEqual(Payload.Length, written);
        CollectionAssert.AreEqual(Payload, destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryDecodeBase64FromUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryDecodeBase64FromUtf8_WhenDestinationIsTooSmall_ShouldReturnFalse()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(Payload));
        byte[] backing = new byte[1];

        bool ok = ((ReadOnlySpan<byte>)utf8).TryDecodeBase64FromUtf8(backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase64UrlString(ReadOnlySpan{byte})" /> matches the
    /// canonical <see cref="Base64Url" /> encode output.
    /// </summary>
    [TestMethod]
    public void ToBase64UrlString_WhenInvoked_ShouldMatchBase64UrlEncode()
    {
        string actual = ((ReadOnlySpan<byte>)Payload).ToBase64UrlString();

        Assert.AreEqual(Base64Url.Encode(Payload), actual);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryEncodeBase64UrlToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// writes UTF-8 bytes that match the canonical <see cref="Base64Url.EncodeToUtf8(ReadOnlySpan{byte})" />
    /// output.
    /// </summary>
    [TestMethod]
    public void TryEncodeBase64UrlToUtf8_WhenDestinationFits_ShouldWriteUtf8BytesEqualToCanonicalEncoder()
    {
        byte[] expected = Base64Url.EncodeToUtf8(Payload);
        Span<byte> destination = new byte[Payload.Length * 2];

        bool ok = ((ReadOnlySpan<byte>)Payload).TryEncodeBase64UrlToUtf8(destination, out int written);

        Assert.IsTrue(ok);
        Assert.AreEqual(expected.Length, written);
        CollectionAssert.AreEqual(expected, destination.Slice(0, written).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryEncodeBase64UrlToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeBase64UrlToUtf8_WhenDestinationIsTooSmall_ShouldReturnFalse()
    {
        byte[] backing = new byte[1];

        bool ok = ((ReadOnlySpan<byte>)Payload).TryEncodeBase64UrlToUtf8(backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryDecodeBase64UrlFromUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// decodes back to the original payload when the input is a valid UTF-8 URL-safe Base64 sequence.
    /// </summary>
    [TestMethod]
    public void TryDecodeBase64UrlFromUtf8_WhenInputIsValid_ShouldRoundTrip()
    {
        byte[] utf8 = Base64Url.EncodeToUtf8(Payload);
        Span<byte> destination = new byte[Payload.Length];

        bool ok = ((ReadOnlySpan<byte>)utf8).TryDecodeBase64UrlFromUtf8(destination, out int written);

        Assert.IsTrue(ok);
        Assert.AreEqual(Payload.Length, written);
        CollectionAssert.AreEqual(Payload, destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryDecodeBase64UrlFromUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryDecodeBase64UrlFromUtf8_WhenDestinationIsTooSmall_ShouldReturnFalse()
    {
        byte[] utf8 = Base64Url.EncodeToUtf8(Payload);
        byte[] backing = new byte[1];

        bool ok = ((ReadOnlySpan<byte>)utf8).TryDecodeBase64UrlFromUtf8(backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.TryDecodeBase64FromUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the input contains characters outside the Base64 alphabet.
    /// </summary>
    [TestMethod]
    public void TryDecodeBase64FromUtf8_WhenInputIsInvalid_ShouldReturnFalse()
    {
        byte[] invalid = System.Text.Encoding.ASCII.GetBytes("not^valid^base64");
        byte[] backing = new byte[64];

        bool ok = ((ReadOnlySpan<byte>)invalid).TryDecodeBase64FromUtf8(backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }
}
