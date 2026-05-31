// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingsCoverageTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Coverage tests for the <see cref="BinaryEncodings" /> singleton adapters and the
/// <see cref="BinaryEncodingExtensions" /> fluent extension surface. The coverage report flagged the
/// <c>GetMaxDecodedLength</c>, <c>GetMaxEncodedLength</c>, <c>TryEncode</c>, <c>TryDecode</c>, and <c>IsValid</c>
/// adapter methods at 0% and the <see cref="BinaryEncodingExtensions" /> <c>ToBase*String(ReadOnlySpan{byte})</c>
/// overloads at 0%. These tests exercise each adapter method by name so the per-method coverage targets close.
/// </summary>
[TestClass]
public sealed class BinaryEncodingsCoverageTests
{

    private static readonly byte[] Payload = System.Text.Encoding.ASCII.GetBytes("Hello, World!");

    /// <summary>
    /// Verifies that every <see cref="BinaryEncodings" /> singleton returns a deterministic non-empty Name and
    /// Description (exercising property getters across all six adapter types).
    /// </summary>
    [TestMethod]
    public void AllAdapters_ShouldExposeNameAndDescription()
    {
        IBinaryEncoding[] encodings =
        [
            BinaryEncodings.Base16Lower,
            BinaryEncodings.Base16Upper,
            BinaryEncodings.Base32,
            BinaryEncodings.Base32Crockford,
            BinaryEncodings.Base32Hex,
            BinaryEncodings.Base32ZBase32,
            BinaryEncodings.Base58,
            BinaryEncodings.Base58Ripple,
            BinaryEncodings.Base64,
            BinaryEncodings.Base64Mime,
            BinaryEncodings.Base64UrlSafe,
            BinaryEncodings.Ascii85,
            BinaryEncodings.Z85,
        ];

        foreach (IBinaryEncoding encoding in encodings)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(encoding.Name));
            Assert.IsFalse(string.IsNullOrWhiteSpace(encoding.Description));
        }
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Ascii85" /> variant adapter mirrors the canonical Ascii85 API.
    /// </summary>
    [TestMethod]
    public void Ascii85_AdapterRoundTrip_ShouldMatchCanonicalApi()
    {
        IBinaryEncoding encoding = BinaryEncodings.Ascii85;

        var encoded = encoding.Encode(Payload);

        Assert.AreEqual(Base85.Encode(Payload), encoded);
        Assert.IsTrue(encoding.IsValid(encoded.AsSpan()));
        Assert.IsTrue(encoding.GetMaxEncodedLength(Payload.Length) >= encoded.Length);
        Assert.IsTrue(encoding.GetMaxDecodedLength(encoded.Length) >= Payload.Length);

        var charBuffer = new char[encoding.GetMaxEncodedLength(Payload.Length)];
        Assert.IsTrue(encoding.TryEncode(Payload, charBuffer, out var charsWritten));
        Assert.AreEqual(encoded.Length, charsWritten);

        var byteBuffer = new byte[encoding.GetMaxDecodedLength(encoded.Length)];
        Assert.IsTrue(encoding.TryDecode(encoded.AsSpan(), byteBuffer, out var bytesWritten));
        Assert.AreEqual(Payload.Length, bytesWritten);
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base16Lower" /> singleton exposes the canonical Base16 surface
    /// (Encode / Decode / TryEncode / TryDecode / IsValid / length contracts).
    /// </summary>
    [TestMethod]
    public void Base16Lower_AdapterRoundTrip_ShouldMatchCanonicalApi()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base16Lower;

        var encoded = encoding.Encode(Payload);
        var decoded = encoding.Decode(encoded.AsSpan());

        Assert.AreEqual(Base16.Encode(Payload), encoded);
        CollectionAssert.AreEqual(Payload, decoded);
        Assert.IsTrue(encoding.IsValid(encoded.AsSpan()));
        Assert.IsTrue(encoding.GetMaxEncodedLength(Payload.Length) >= encoded.Length);
        Assert.IsTrue(encoding.GetMaxDecodedLength(encoded.Length) >= decoded.Length);

        var charBuffer = new char[encoding.GetMaxEncodedLength(Payload.Length)];
        Assert.IsTrue(encoding.TryEncode(Payload, charBuffer, out var charsWritten));
        Assert.AreEqual(encoded.Length, charsWritten);

        var byteBuffer = new byte[encoding.GetMaxDecodedLength(encoded.Length)];
        Assert.IsTrue(encoding.TryDecode(encoded.AsSpan(), byteBuffer, out var bytesWritten));
        Assert.AreEqual(decoded.Length, bytesWritten);
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base16Upper" /> singleton emits upper-case hex and recovers via
    /// the same adapter.
    /// </summary>
    [TestMethod]
    public void Base16Upper_AdapterRoundTrip_ShouldMatchCanonicalApi()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base16Upper;

        var encoded = encoding.Encode(Payload);

        Assert.AreEqual(Base16.Encode(Payload, BaseFormattingOptions.UpperCase), encoded);
        Assert.IsTrue(encoding.IsValid(encoded.AsSpan()));
        Assert.IsTrue(encoding.GetMaxEncodedLength(Payload.Length) >= encoded.Length);
        Assert.IsTrue(encoding.GetMaxDecodedLength(encoded.Length) >= Payload.Length);

        var charBuffer = new char[encoding.GetMaxEncodedLength(Payload.Length)];
        Assert.IsTrue(encoding.TryEncode(Payload, charBuffer, out var charsWritten));
        Assert.AreEqual(encoded.Length, charsWritten);

        var byteBuffer = new byte[encoding.GetMaxDecodedLength(encoded.Length)];
        Assert.IsTrue(encoding.TryDecode(encoded.AsSpan(), byteBuffer, out var bytesWritten));
        Assert.AreEqual(Payload.Length, bytesWritten);
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base32" /> variant adapter mirrors the canonical Standard
    /// variant API.
    /// </summary>
    [TestMethod]
    public void Base32_AdapterRoundTrip_ShouldMatchCanonicalApi()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base32;

        var encoded = encoding.Encode(Payload);

        Assert.AreEqual(Base32.Encode(Payload), encoded);
        Assert.IsTrue(encoding.IsValid(encoded.AsSpan()));
        Assert.IsTrue(encoding.GetMaxEncodedLength(Payload.Length) >= encoded.Length);
        Assert.IsTrue(encoding.GetMaxDecodedLength(encoded.Length) >= Payload.Length);

        var charBuffer = new char[encoding.GetMaxEncodedLength(Payload.Length)];
        Assert.IsTrue(encoding.TryEncode(Payload, charBuffer, out var charsWritten));
        Assert.AreEqual(encoded.Length, charsWritten);

        var byteBuffer = new byte[encoding.GetMaxDecodedLength(encoded.Length)];
        Assert.IsTrue(encoding.TryDecode(encoded.AsSpan(), byteBuffer, out var bytesWritten));
        Assert.AreEqual(Payload.Length, bytesWritten);
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base58" /> variant adapter (Bitcoin/Flickr) mirrors the canonical
    /// API for round-trip and validation.
    /// </summary>
    [TestMethod]
    public void Base58_AdapterRoundTrip_ShouldMatchCanonicalApi()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base58;

        var encoded = encoding.Encode(Payload);

        Assert.AreEqual(Base58.Encode(Payload), encoded);
        Assert.IsTrue(encoding.IsValid(encoded.AsSpan()));
        Assert.IsTrue(encoding.GetMaxEncodedLength(Payload.Length) >= encoded.Length);
        Assert.IsTrue(encoding.GetMaxDecodedLength(encoded.Length) >= Payload.Length);

        var charBuffer = new char[encoding.GetMaxEncodedLength(Payload.Length)];
        Assert.IsTrue(encoding.TryEncode(Payload, charBuffer, out var charsWritten));
        Assert.AreEqual(encoded.Length, charsWritten);

        var byteBuffer = new byte[encoding.GetMaxDecodedLength(encoded.Length)];
        Assert.IsTrue(encoding.TryDecode(encoded.AsSpan(), byteBuffer, out var bytesWritten));
        Assert.AreEqual(Payload.Length, bytesWritten);
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base64" /> variant adapter (Standard) mirrors the canonical API.
    /// </summary>
    [TestMethod]
    public void Base64_AdapterRoundTrip_ShouldMatchCanonicalApi()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base64;

        var encoded = encoding.Encode(Payload);

        Assert.AreEqual(Base64.Encode(Payload), encoded);
        Assert.IsTrue(encoding.IsValid(encoded.AsSpan()));
        Assert.IsTrue(encoding.GetMaxEncodedLength(Payload.Length) >= encoded.Length);
        Assert.IsTrue(encoding.GetMaxDecodedLength(encoded.Length) >= Payload.Length);

        var charBuffer = new char[encoding.GetMaxEncodedLength(Payload.Length)];
        Assert.IsTrue(encoding.TryEncode(Payload, charBuffer, out var charsWritten));
        Assert.AreEqual(encoded.Length, charsWritten);

        var byteBuffer = new byte[encoding.GetMaxDecodedLength(encoded.Length)];
        Assert.IsTrue(encoding.TryDecode(encoded.AsSpan(), byteBuffer, out var bytesWritten));
        Assert.AreEqual(Payload.Length, bytesWritten);
    }

    /// <summary>
    /// Verifies that the generic <see cref="BinaryEncodingExtensions.Encode(byte[], IBinaryEncoding)" /> and
    /// <see cref="BinaryEncodingExtensions.Decode(string, IBinaryEncoding)" /> route through
    /// <see cref="IBinaryEncoding" /> and round-trip the payload.
    /// </summary>
    [TestMethod]
    public void GenericExtensions_ShouldRoundTripThroughIBinaryEncoding()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base16Lower;

        var encoded = Payload.Encode(encoding);
        var decoded = encoded.Decode(encoding);

        Assert.AreEqual(Base16.Encode(Payload), encoded);
        CollectionAssert.AreEqual(Payload, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> resolves every supported alias and returns the same
    /// singleton instance as the static property.
    /// </summary>
    [TestMethod]
    public void Get_ShouldResolveEveryNameToTheSingletonInstance()
    {
        Assert.AreSame(BinaryEncodings.Base16Lower, BinaryEncodings.Get("base16"));
        Assert.AreSame(BinaryEncodings.Base16Lower, BinaryEncodings.Get("base16-lower"));
        Assert.AreSame(BinaryEncodings.Base16Upper, BinaryEncodings.Get("base16-upper"));
        Assert.AreSame(BinaryEncodings.Base32, BinaryEncodings.Get("base32"));
        Assert.AreSame(BinaryEncodings.Base32Hex, BinaryEncodings.Get("base32hex"));
        Assert.AreSame(BinaryEncodings.Base32Crockford, BinaryEncodings.Get("base32-crockford"));
        Assert.AreSame(BinaryEncodings.Base32ZBase32, BinaryEncodings.Get("z-base-32"));
        Assert.AreSame(BinaryEncodings.Base64, BinaryEncodings.Get("base64"));
        Assert.AreSame(BinaryEncodings.Base64UrlSafe, BinaryEncodings.Get("base64-urlsafe"));
        Assert.AreSame(BinaryEncodings.Base64Mime, BinaryEncodings.Get("base64-mime"));
        Assert.AreSame(BinaryEncodings.Base58, BinaryEncodings.Get("base58"));
        Assert.AreSame(BinaryEncodings.Base58Ripple, BinaryEncodings.Get("base58-ripple"));
        Assert.AreSame(BinaryEncodings.Ascii85, BinaryEncodings.Get("ascii85"));
        Assert.AreSame(BinaryEncodings.Z85, BinaryEncodings.Get("z85"));
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> throws <see cref="ArgumentException" /> for unknown
    /// encoding names — the default branch of the switch expression.
    /// </summary>
    [TestMethod]
    public void Get_WhenUnknownName_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BinaryEncodings.Get("base42");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase16String(ReadOnlySpan{byte})" /> matches
    /// <see cref="Base16.ToHexStringLower(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public void ToBase16String_SpanExtension_ShouldMatchCanonicalEncoder()
    {
        Assert.AreEqual(Base16.ToHexStringLower(Payload.AsSpan()), ((ReadOnlySpan<byte>)Payload).ToBase16String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase32String(ReadOnlySpan{byte})" /> matches
    /// <see cref="Base32.ToBase32String(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public void ToBase32String_SpanExtension_ShouldMatchCanonicalEncoder()
    {
        Assert.AreEqual(Base32.ToBase32String(Payload.AsSpan()), ((ReadOnlySpan<byte>)Payload).ToBase32String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase58String(ReadOnlySpan{byte})" /> matches
    /// <see cref="Base58.ToBase58String(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public void ToBase58String_SpanExtension_ShouldMatchCanonicalEncoder()
    {
        Assert.AreEqual(Base58.ToBase58String(Payload.AsSpan()), ((ReadOnlySpan<byte>)Payload).ToBase58String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase64String(ReadOnlySpan{byte})" /> matches
    /// <see cref="Base64.ToBase64String(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public void ToBase64String_SpanExtension_ShouldMatchCanonicalEncoder()
    {
        Assert.AreEqual(Base64.ToBase64String(Payload.AsSpan()), ((ReadOnlySpan<byte>)Payload).ToBase64String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase85String(ReadOnlySpan{byte})" /> matches
    /// <see cref="Base85.ToBase85String(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public void ToBase85String_SpanExtension_ShouldMatchCanonicalEncoder()
    {
        Assert.AreEqual(Base85.ToBase85String(Payload.AsSpan()), ((ReadOnlySpan<byte>)Payload).ToBase85String());
    }

}
