// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuidEncodingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Tests for the <see cref="Guid" /> convenience helpers across every encoding. Verifies that
/// <c>Encode(Guid)</c> and <c>DecodeGuid</c> round-trip every <see cref="Guid" /> value byte-for-byte using the
/// <see cref="Guid.TryWriteBytes(Span{byte})" /> mixed-endian layout.
/// </summary>
[TestClass]
public sealed class GuidEncodingTests
{

    /// <summary>
    /// A fixed, recognisable test <see cref="Guid" />.
    /// </summary>
    private static readonly Guid TestGuid = new("01020304-0506-0708-090A-0B0C0D0E0F10");

    /// <summary>
    /// Verifies the Base16 GUID round trip.
    /// </summary>
    [TestMethod]
    public void Base16_EncodeGuid_RoundTrip_ShouldRecoverOriginalGuid()
    {
        string encoded = Base16.Encode(TestGuid);
        Guid decoded = Base16.DecodeGuid(encoded.AsSpan());

        Assert.AreEqual(TestGuid, decoded);
    }

    /// <summary>
    /// Verifies that Base16 produces 32 hex characters for any GUID.
    /// </summary>
    [TestMethod]
    public void Base16_EncodeGuid_ShouldProduceThirtyTwoCharacters()
    {
        Assert.AreEqual(32, Base16.Encode(TestGuid).Length);
    }

    /// <summary>
    /// Verifies the Base32 GUID round trip.
    /// </summary>
    [TestMethod]
    public void Base32_EncodeGuid_RoundTrip_ShouldRecoverOriginalGuid()
    {
        string encoded = Base32.Encode(TestGuid);
        Guid decoded = Base32.DecodeGuid(encoded.AsSpan());

        Assert.AreEqual(TestGuid, decoded);
    }

    /// <summary>
    /// Verifies the Base32 GUID round trip with OmitPadding option produces 26-char output (no padding).
    /// </summary>
    [TestMethod]
    public void Base32_EncodeGuidWithOmitPadding_ShouldProduceTwentySixCharacters()
    {
        string encoded = Base32.Encode(TestGuid, Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual(26, encoded.Length);
    }

    /// <summary>
    /// Verifies the Base58 GUID round trip.
    /// </summary>
    [TestMethod]
    public void Base58_EncodeGuid_RoundTrip_ShouldRecoverOriginalGuid()
    {
        string encoded = Base58.Encode(TestGuid);
        Guid decoded = Base58.DecodeGuid(encoded.AsSpan());

        Assert.AreEqual(TestGuid, decoded);
    }

    /// <summary>
    /// Verifies the Base64 GUID round trip.
    /// </summary>
    [TestMethod]
    public void Base64_EncodeGuid_RoundTrip_ShouldRecoverOriginalGuid()
    {
        string encoded = Base64.Encode(TestGuid);
        Guid decoded = Base64.DecodeGuid(encoded.AsSpan());

        Assert.AreEqual(TestGuid, decoded);
    }

    /// <summary>
    /// Verifies the Base64Url GUID round trip — the standard 22-char URL-safe GUID convention.
    /// </summary>
    [TestMethod]
    public void Base64Url_EncodeGuid_ShouldProduceTwentyTwoUrlSafeCharacters()
    {
        string encoded = Base64.Encode(TestGuid, Base64Variant.UrlSafe);

        Assert.AreEqual(22, encoded.Length);
        Assert.IsFalse(encoded.Contains('+', StringComparison.Ordinal));
        Assert.IsFalse(encoded.Contains('/', StringComparison.Ordinal));
        Assert.IsFalse(encoded.Contains('=', StringComparison.Ordinal));

        Guid decoded = Base64.DecodeGuid(encoded.AsSpan(), Base64Variant.UrlSafe);
        Assert.AreEqual(TestGuid, decoded);
    }

    /// <summary>
    /// Verifies the Base85 (Ascii85) GUID round trip produces exactly 20 characters.
    /// </summary>
    [TestMethod]
    public void Base85_EncodeGuid_ShouldProduceTwentyCharactersAndRoundTrip()
    {
        string encoded = Base85.Encode(TestGuid);
        Guid decoded = Base85.DecodeGuid(encoded.AsSpan());

        Assert.AreEqual(20, encoded.Length);
        Assert.AreEqual(TestGuid, decoded);
    }

    /// <summary>
    /// Verifies that <c>DecodeGuid</c> throws <see cref="FormatException" /> for input that does not decode to
    /// 16 bytes.
    /// </summary>
    [TestMethod]
    public void DecodeGuid_WhenInputNotSixteenBytes_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.DecodeGuid("DEADBEEF".AsSpan());
        });
    }

    /// <summary>
    /// Verifies <see cref="Guid.Empty" /> round-trips through all encodings.
    /// </summary>
    [TestMethod]
    public void EncodeGuidEmpty_ShouldRoundTripThroughEveryEncoding()
    {
        Guid empty = Guid.Empty;

        Assert.AreEqual(empty, Base16.DecodeGuid(Base16.Encode(empty).AsSpan()));
        Assert.AreEqual(empty, Base32.DecodeGuid(Base32.Encode(empty).AsSpan()));
        Assert.AreEqual(empty, Base64.DecodeGuid(Base64.Encode(empty).AsSpan()));
        Assert.AreEqual(empty, Base58.DecodeGuid(Base58.Encode(empty).AsSpan()));
        Assert.AreEqual(empty, Base85.DecodeGuid(Base85.Encode(empty).AsSpan()));
    }

    /// <summary>
    /// Verifies that a random GUID round-trips through every encoding 100 times.
    /// </summary>
    [TestMethod]
    public void EncodeGuidRandom_ShouldRoundTripThroughEveryEncoding()
    {
        Random rng = new(0x12345);
        Span<byte> buffer = stackalloc byte[16];
        for (int i = 0; i < 100; i++)
        {
            rng.NextBytes(buffer);
            Guid g = new(buffer);

            Assert.AreEqual(g, Base16.DecodeGuid(Base16.Encode(g).AsSpan()));
            Assert.AreEqual(g, Base32.DecodeGuid(Base32.Encode(g).AsSpan()));
            Assert.AreEqual(g, Base64.DecodeGuid(Base64.Encode(g).AsSpan()));
            Assert.AreEqual(g, Base58.DecodeGuid(Base58.Encode(g).AsSpan()));
            Assert.AreEqual(g, Base85.DecodeGuid(Base85.Encode(g).AsSpan()));
        }
    }

    /// <summary>
    /// Verifies that <c>TryDecodeGuid</c> returns <see langword="false" /> for input that does not decode to 16 bytes.
    /// </summary>
    [TestMethod]
    public void TryDecodeGuid_WhenInputNotSixteenBytes_ShouldReturnFalse()
    {
        // "DEADBEEF" decodes to 4 bytes — not a GUID.
        bool ok = Base16.TryDecodeGuid("DEADBEEF".AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecodeGuid" /> returns <see langword="true" /> and recovers the GUID for a
    /// valid encoded 16-byte value.
    /// </summary>
    [TestMethod]
    public void Base32_TryDecodeGuid_WhenValidEncodedGuid_ShouldReturnTrueAndRecoverValue()
    {
        string encoded = Base32.Encode(TestGuid);

        bool ok = Base32.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsTrue(ok);
        Assert.AreEqual(TestGuid, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecodeGuid" /> returns <see langword="false" /> and emits
    /// <see cref="Guid.Empty" /> when the input is shorter or longer than a 16-byte encoding.
    /// </summary>
    [TestMethod]
    public void Base32_TryDecodeGuid_WhenInputNotSixteenBytes_ShouldReturnFalseAndEmptyGuid()
    {
        // 5-byte payload encodes to "MFRGGZDF" (no padding), well short of 16 bytes.
        string encoded = Base32.Encode(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65 });

        bool ok = Base32.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecodeGuid" /> returns <see langword="false" /> for input containing a
    /// character outside the Base32 alphabet.
    /// </summary>
    [TestMethod]
    public void Base32_TryDecodeGuid_WhenInvalidAlphabet_ShouldReturnFalse()
    {
        // '!' is not in the Base32 alphabet.
        bool ok = Base32.TryDecodeGuid("AAAAAAAAAAAAAAAAAAAAAAAAAAA!".AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryDecodeGuid" /> returns <see langword="true" /> and recovers the GUID for a
    /// valid encoded 16-byte value.
    /// </summary>
    [TestMethod]
    public void Base58_TryDecodeGuid_WhenValidEncodedGuid_ShouldReturnTrueAndRecoverValue()
    {
        string encoded = Base58.Encode(TestGuid);

        bool ok = Base58.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsTrue(ok);
        Assert.AreEqual(TestGuid, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryDecodeGuid" /> returns <see langword="false" /> and emits
    /// <see cref="Guid.Empty" /> when the decoded payload is not exactly 16 bytes.
    /// </summary>
    [TestMethod]
    public void Base58_TryDecodeGuid_WhenInputNotSixteenBytes_ShouldReturnFalseAndEmptyGuid()
    {
        // 4-byte payload encodes to a short Base58 string.
        string encoded = Base58.Encode(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        bool ok = Base58.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryDecodeGuid" /> returns <see langword="false" /> for input containing a
    /// character outside the variant alphabet (e.g. <c>'0'</c> or <c>'O'</c> in Bitcoin/Flickr).
    /// </summary>
    [TestMethod]
    public void Base58_TryDecodeGuid_WhenInvalidAlphabet_ShouldReturnFalse()
    {
        // '0' and 'O' are intentionally excluded from the Bitcoin/Flickr alphabet.
        bool ok = Base58.TryDecodeGuid("0000O".AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecodeGuid" /> returns <see langword="true" /> and recovers the GUID for a
    /// valid encoded 16-byte value.
    /// </summary>
    [TestMethod]
    public void Base64_TryDecodeGuid_WhenValidEncodedGuid_ShouldReturnTrueAndRecoverValue()
    {
        string encoded = Base64.Encode(TestGuid);

        bool ok = Base64.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsTrue(ok);
        Assert.AreEqual(TestGuid, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecodeGuid" /> returns <see langword="true" /> for the URL-safe variant
    /// (22-char output, no padding) and recovers the original GUID.
    /// </summary>
    [TestMethod]
    public void Base64_TryDecodeGuid_WhenValidUrlSafeEncodedGuid_ShouldReturnTrueAndRecoverValue()
    {
        string encoded = Base64.Encode(TestGuid, Base64Variant.UrlSafe);

        bool ok = Base64.TryDecodeGuid(encoded.AsSpan(), out Guid value, Base64Variant.UrlSafe);

        Assert.IsTrue(ok);
        Assert.AreEqual(TestGuid, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecodeGuid" /> returns <see langword="false" /> when the decoded payload is
    /// not exactly 16 bytes.
    /// </summary>
    [TestMethod]
    public void Base64_TryDecodeGuid_WhenInputNotSixteenBytes_ShouldReturnFalseAndEmptyGuid()
    {
        // 4-byte payload encodes to "3q2+7w==" — 4 bytes, not 16.
        string encoded = Base64.Encode(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        bool ok = Base64.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecodeGuid" /> returns <see langword="false" /> for input that fails to
    /// decode under the standard variant.
    /// </summary>
    [TestMethod]
    public void Base64_TryDecodeGuid_WhenInvalidAlphabet_ShouldReturnFalse()
    {
        // '@' is not in the Base64 alphabet.
        bool ok = Base64.TryDecodeGuid("@AAAAAAAAAAAAAAAAAAAAA==".AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecodeGuid" /> returns <see langword="true" /> and recovers the GUID for a
    /// valid encoded 16-byte value (Ascii85 variant emits exactly 20 chars).
    /// </summary>
    [TestMethod]
    public void Base85_TryDecodeGuid_WhenValidEncodedGuid_ShouldReturnTrueAndRecoverValue()
    {
        string encoded = Base85.Encode(TestGuid);

        bool ok = Base85.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsTrue(ok);
        Assert.AreEqual(TestGuid, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecodeGuid" /> works through the Z85 variant on a properly aligned
    /// (16-byte) GUID payload.
    /// </summary>
    [TestMethod]
    public void Base85_TryDecodeGuid_WhenZ85Variant_ShouldRoundTripGuid()
    {
        string encoded = Base85.Encode(TestGuid, Base85Variant.Z85);

        bool ok = Base85.TryDecodeGuid(encoded.AsSpan(), out Guid value, Base85Variant.Z85);

        Assert.IsTrue(ok);
        Assert.AreEqual(TestGuid, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecodeGuid" /> returns <see langword="false" /> when the decoded payload is
    /// not exactly 16 bytes.
    /// </summary>
    [TestMethod]
    public void Base85_TryDecodeGuid_WhenInputNotSixteenBytes_ShouldReturnFalseAndEmptyGuid()
    {
        // 4-byte payload encodes to a 5-character Ascii85 group, not the 20 chars a GUID would produce.
        string encoded = Base85.Encode(new byte[] { 0x86, 0x4F, 0xD2, 0x6F });

        bool ok = Base85.TryDecodeGuid(encoded.AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecodeGuid" /> returns <see langword="false" /> for input containing a
    /// character outside the Ascii85 alphabet.
    /// </summary>
    [TestMethod]
    public void Base85_TryDecodeGuid_WhenInvalidAlphabet_ShouldReturnFalse()
    {
        // Tilde '~' is outside the Ascii85 alphabet ('!' through 'u').
        bool ok = Base85.TryDecodeGuid("~~~~~~~~~~~~~~~~~~~~".AsSpan(), out Guid value);

        Assert.IsFalse(ok);
        Assert.AreEqual(Guid.Empty, value);
    }

}
