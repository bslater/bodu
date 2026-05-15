// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="BinaryEncodings" /> and the <see cref="IBinaryEncoding" /> contract.
/// </summary>
[TestClass]
public sealed class BinaryEncodingsTests
{
    /// <summary>
    /// Verifies that every pre-configured encoding round-trips a canonical input.
    /// </summary>
    /// <param name="encodingName">The encoding name resolved via <see cref="BinaryEncodings.Get(string)" />.</param>
    [DataTestMethod]
    [DataRow("base16-lower")]
    [DataRow("base16-upper")]
    [DataRow("base32")]
    [DataRow("base32hex")]
    [DataRow("base32-crockford")]
    [DataRow("z-base-32")]
    [DataRow("base64")]
    [DataRow("base64-urlsafe")]
    [DataRow("base64-mime")]
    [DataRow("base58")]
    [DataRow("base58-ripple")]
    [DataRow("ascii85")]
    public void RoundTrip_ForEveryRegisteredEncoding_ShouldRecoverOriginal(string encodingName)
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        IBinaryEncoding encoding = BinaryEncodings.Get(encodingName);

        string encoded = encoding.Encode(original);
        byte[] decoded = encoding.Decode(encoded);

        CollectionAssert.AreEqual(original, decoded, $"Round trip failed for {encodingName}.");
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Z85" /> requires a 4-byte-aligned input — characterising the
    /// difference from <see cref="BinaryEncodings.Ascii85" />.
    /// </summary>
    [TestMethod]
    public void RoundTrip_ForZ85_ShouldRecoverAlignedInput()
    {
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE };

        string encoded = BinaryEncodings.Z85.Encode(original);
        byte[] decoded = BinaryEncodings.Z85.Decode(encoded);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> recognises both the canonical name and any common
    /// aliases.
    /// </summary>
    /// <param name="alias">An alias for an encoding.</param>
    /// <param name="canonical">The canonical name the alias must resolve to.</param>
    [DataTestMethod]
    [DataRow("hex", "base16-lower")]
    [DataRow("HEX", "base16-lower")]
    [DataRow("base16", "base16-lower")]
    [DataRow("hex-upper", "base16-upper")]
    [DataRow("zbase32", "z-base-32")]
    [DataRow("base64url", "base64-urlsafe")]
    [DataRow("base58-bitcoin", "base58")]
    [DataRow("base58-flickr", "base58")]
    [DataRow("base85", "ascii85")]
    public void Get_WhenAliasProvided_ShouldResolveToCanonicalEncoding(string alias, string canonical)
    {
        IBinaryEncoding fromAlias = BinaryEncodings.Get(alias);
        IBinaryEncoding fromCanonical = BinaryEncodings.Get(canonical);

        Assert.AreSame(fromCanonical, fromAlias);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> rejects an unknown name with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Get_WhenUnknownName_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BinaryEncodings.Get("base-unknown");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> rejects a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void Get_WhenNullName_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BinaryEncodings.Get(null!);
        });
    }

    /// <summary>
    /// Verifies that each encoding exposes a non-empty <see cref="IBinaryEncoding.Name" /> and
    /// <see cref="IBinaryEncoding.Description" />.
    /// </summary>
    [TestMethod]
    public void Name_AndDescription_ShouldBeNonEmptyForEveryRegisteredEncoding()
    {
        IBinaryEncoding[] encodings =
        {
            BinaryEncodings.Base16Lower,
            BinaryEncodings.Base16Upper,
            BinaryEncodings.Base32,
            BinaryEncodings.Base32Hex,
            BinaryEncodings.Base32Crockford,
            BinaryEncodings.Base32ZBase32,
            BinaryEncodings.Base64,
            BinaryEncodings.Base64UrlSafe,
            BinaryEncodings.Base64Mime,
            BinaryEncodings.Base58,
            BinaryEncodings.Base58Ripple,
            BinaryEncodings.Ascii85,
            BinaryEncodings.Z85,
        };

        foreach (IBinaryEncoding e in encodings)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(e.Name), $"{e.GetType().Name}.Name is empty.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(e.Description), $"{e.GetType().Name}.Description is empty.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="IBinaryEncoding.TryEncode" /> reports the same character count as
    /// <see cref="IBinaryEncoding.Encode(ReadOnlySpan{byte})" /> for the same input.
    /// </summary>
    [TestMethod]
    public void TryEncode_AndEncode_ShouldAgreeOnLength()
    {
        byte[] bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        foreach (string name in new[] { "base16-lower", "base32", "base64", "base58", "ascii85" })
        {
            IBinaryEncoding encoding = BinaryEncodings.Get(name);
            char[] buffer = new char[encoding.GetMaxEncodedLength(bytes.Length)];

            bool ok = encoding.TryEncode(bytes.AsSpan(), buffer, out int charsWritten);
            string encoded = encoding.Encode(bytes.AsSpan());

            Assert.IsTrue(ok, $"TryEncode returned false for {name}.");
            Assert.AreEqual(encoded.Length, charsWritten, $"Length mismatch for {name}.");
            Assert.AreEqual(encoded, new string(buffer, 0, charsWritten));
        }
    }

    /// <summary>
    /// Verifies that <see cref="IBinaryEncoding.IsValid" /> returns <see langword="true" /> for encoded output of
    /// the same encoding and <see langword="false" /> for invalid input.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptOwnOutputAndRejectBlatantlyInvalid()
    {
        byte[] bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        foreach (string name in new[] { "base16-lower", "base32", "base64", "base58", "ascii85" })
        {
            IBinaryEncoding encoding = BinaryEncodings.Get(name);
            string encoded = encoding.Encode(bytes);

            Assert.IsTrue(encoding.IsValid(encoded.AsSpan()), $"{name} should validate its own output.");
            Assert.IsFalse(encoding.IsValid("\x00\x01\x02".AsSpan()), $"{name} should reject control characters.");
        }
    }
}
