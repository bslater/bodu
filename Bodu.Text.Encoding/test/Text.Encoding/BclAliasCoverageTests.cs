// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BclAliasCoverageTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Coverage-focused tests for the BCL-shaped alias methods across every encoding family
/// (<c>ToBase*String</c>, <c>FromBase*String</c>, <c>TryTo*String</c>). These wrappers delegate to the canonical
/// static API; the goal is to assert exact delegation behaviour and exercise the success / failure branches that
/// the coverage report flagged at 0% to 70%.
/// </summary>
[TestClass]
public sealed class BclAliasCoverageTests
{

    private static readonly byte[] SamplePayload = System.Text.Encoding.ASCII.GetBytes("Hello, World!");

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{char})" /> decodes a valid hex char span.
    /// </summary>
    [TestMethod]
    public void Base16_FromHexString_CharSpan_ShouldDecode()
    {
        var decoded = Base16.FromHexString("DEADBEEF".AsSpan());

        CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void Base16_FromHexString_Utf8WithEmpty_ShouldReturnEmptyArray()
    {
        var decoded = Base16.FromHexString(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{byte})" /> throws <see cref="FormatException" />
    /// when a byte is outside the hex alphabet.
    /// </summary>
    [TestMethod]
    public void Base16_FromHexString_Utf8WithInvalidChar_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.FromHexString(System.Text.Encoding.ASCII.GetBytes("DEADXZ01"));
        });

        Assert.IsTrue(ex.Message.Contains("non-hex", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{byte})" /> throws <see cref="FormatException" />
    /// when the byte count is odd.
    /// </summary>
    [TestMethod]
    public void Base16_FromHexString_Utf8WithOddLength_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.FromHexString(System.Text.Encoding.ASCII.GetBytes("DEADBE0"));
        });

        Assert.IsTrue(ex.Message.Contains("not even", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.ToHexStringLower(byte[], int, int)" /> encodes a slice of the array.
    /// </summary>
    [TestMethod]
    public void Base16_ToHexStringLower_ArrayOffsetCount_ShouldEncodeSlice()
    {
        var bytes = new byte[] { 0xAA, 0xDE, 0xAD, 0xBE, 0xEF, 0xBB };

        var encoded = Base16.ToHexStringLower(bytes, 1, 4);

        Assert.AreEqual("deadbeef", encoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.ToHexStringLower(ReadOnlySpan{byte})" /> produces lower-case hex.
    /// </summary>
    [TestMethod]
    public void Base16_ToHexStringLower_Span_ShouldReturnLowerCaseHex()
    {
        Assert.AreEqual("deadbeef", Base16.ToHexStringLower(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryToHexStringLower(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes
    /// UTF-8 lower-case hex bytes.
    /// </summary>
    [TestMethod]
    public void Base16_TryToHexStringLower_Utf8_ShouldWriteAsciiHex()
    {
        var destination = new byte[8];

        var ok = Base16.TryToHexStringLower([0xDE, 0xAD, 0xBE, 0xEF], destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
        Assert.AreEqual("deadbeef", System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{char})" /> decodes a valid Base32 char span.
    /// </summary>
    [TestMethod]
    public void Base32_FromBase32String_CharSpan_ShouldDecode()
    {
        var decoded = Base32.FromBase32String("MZXW6YTBOI======".AsSpan());

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("foobar"), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void Base32_FromBase32String_Utf8_ShouldDecode()
    {
        var decoded = Base32.FromBase32String(System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======"));

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("foobar"), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void Base32_FromBase32String_Utf8WithEmpty_ShouldReturnEmptyArray()
    {
        var decoded = Base32.FromBase32String(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{byte})" /> throws on invalid bytes.
    /// </summary>
    [TestMethod]
    public void Base32_FromBase32String_Utf8WithInvalidByte_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.FromBase32String([(byte)'M', (byte)'Z', 0xFF, (byte)'W', (byte)'6', (byte)'Y', (byte)'T', (byte)'B']);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{char})" /> decodes a valid Base58 char span.
    /// </summary>
    [TestMethod]
    public void Base58_FromBase58String_CharSpan_ShouldDecode()
    {
        var payload = System.Text.Encoding.ASCII.GetBytes("Hello");
        var encoded = Base58.Encode(payload);

        var decoded = Base58.FromBase58String(encoded.AsSpan());

        CollectionAssert.AreEqual(payload, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void Base58_FromBase58String_Utf8_ShouldDecode()
    {
        var payload = System.Text.Encoding.ASCII.GetBytes("Hello");
        var encoded = Base58.EncodeToUtf8(payload);

        var decoded = Base58.FromBase58String(encoded);

        CollectionAssert.AreEqual(payload, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void Base58_FromBase58String_Utf8WithEmpty_ShouldReturnEmptyArray()
    {
        var decoded = Base58.FromBase58String(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{byte})" /> throws on invalid bytes.
    /// </summary>
    [TestMethod]
    public void Base58_FromBase58String_Utf8WithInvalidByte_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58.FromBase58String([(byte)'0', (byte)'1', (byte)'2']);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.ToBase58String(byte[], int, int)" /> encodes a slice.
    /// </summary>
    [TestMethod]
    public void Base58_ToBase58String_ArrayOffsetCount_ShouldEncodeSlice()
    {
        var bytes = new byte[] { 0xAA, 0x01, 0x02, 0x03, 0xBB };

        var encoded = Base58.ToBase58String(bytes, 1, 3);

        Assert.AreEqual(Base58.Encode([0x01, 0x02, 0x03]), encoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.ToBase58String(ReadOnlySpan{byte})" /> aliases the canonical encoder.
    /// </summary>
    [TestMethod]
    public void Base58_ToBase58String_Span_ShouldMatchCanonicalEncoder()
    {
        var payload = SamplePayload;

        Assert.AreEqual(Base58.Encode(payload), Base58.ToBase58String(payload.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{char})" /> decodes a valid Base64 char span.
    /// </summary>
    [TestMethod]
    public void Base64_FromBase64String_CharSpan_ShouldDecode()
    {
        var decoded = Base64.FromBase64String("Zm9vYmFy".AsSpan());

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("foobar"), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void Base64_FromBase64String_Utf8_ShouldDecode()
    {
        var decoded = Base64.FromBase64String(System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy"));

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("foobar"), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void Base64_FromBase64String_Utf8WithEmpty_ShouldReturnEmptyArray()
    {
        var decoded = Base64.FromBase64String(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{byte})" /> throws on invalid bytes.
    /// </summary>
    [TestMethod]
    public void Base64_FromBase64String_Utf8WithInvalidByte_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.FromBase64String([0xFF, 0xFF, 0xFF, 0xFF]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{char})" /> decodes a valid Ascii85 char span.
    /// </summary>
    [TestMethod]
    public void Base85_FromBase85String_CharSpan_ShouldDecode()
    {
        var decoded = Base85.FromBase85String("9jqo^".AsSpan());

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void Base85_FromBase85String_Utf8_ShouldDecode()
    {
        var decoded = Base85.FromBase85String(System.Text.Encoding.ASCII.GetBytes("9jqo^"));

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), decoded);
    }

    /// <summary>
    /// Verifies that the four-parameter <see cref="Base85.FromBase85String(ReadOnlySpan{byte}, Span{byte}, out int, out int)" />
    /// streams a decode and reports consumption.
    /// </summary>
    [TestMethod]
    public void Base85_FromBase85String_Utf8Streaming_ShouldReportProgress()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("9jqo^");
        var destination = new byte[4];

        OperationStatus status = Base85.FromBase85String(utf8, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(utf8.Length, bytesConsumed);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void Base85_FromBase85String_Utf8WithEmpty_ShouldReturnEmptyArray()
    {
        var decoded = Base85.FromBase85String(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{byte})" /> throws on invalid bytes.
    /// </summary>
    [TestMethod]
    public void Base85_FromBase85String_Utf8WithInvalidByte_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.FromBase85String([0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.ToBase85String(byte[], int, int)" /> encodes a slice.
    /// </summary>
    [TestMethod]
    public void Base85_ToBase85String_ArrayOffsetCount_ShouldEncodeSlice()
    {
        var bytes = new byte[] { 0xAA, 0x01, 0x02, 0x03, 0x04, 0xBB };

        var encoded = Base85.ToBase85String(bytes, 1, 4);

        Assert.AreEqual(Base85.Encode([0x01, 0x02, 0x03, 0x04]), encoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.ToBase85String(ReadOnlySpan{byte})" /> aliases the canonical encoder.
    /// </summary>
    [TestMethod]
    public void Base85_ToBase85String_Span_ShouldMatchCanonicalEncoder()
    {
        Assert.AreEqual(Base85.Encode(SamplePayload), Base85.ToBase85String(SamplePayload.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryToBase85String(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes UTF-8
    /// Ascii85 bytes for a payload.
    /// </summary>
    [TestMethod]
    public void Base85_TryToBase85String_Utf8_ShouldWriteAsciiBytes()
    {
        var payload = System.Text.Encoding.ASCII.GetBytes("Man ");
        var destination = new byte[16];

        var ok = Base85.TryToBase85String(payload, destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(5, bytesWritten);
        Assert.AreEqual("9jqo^", System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

}
