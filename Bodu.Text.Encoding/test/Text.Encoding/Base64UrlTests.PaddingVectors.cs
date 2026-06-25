// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64UrlTests.PaddingVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Pins the URL-safe Base64 contract for the padding and alphabet edge cases that JWT, OAuth, and related
/// consumers rely on. Each vector is documented explicitly so a regression that changed Base64Url's default
/// leniency or canonicalisation would be caught.
/// </summary>
[TestClass]
public sealed class Base64UrlTests_PaddingVectors
{
    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> accepts the unpadded form "AQ" — the canonical
    /// URL-safe encoding of the single byte 0x01.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputIsUnpaddedAQ_ShouldReturnSingleByte()
    {
        byte[] decoded = Base64Url.Decode("AQ");

        CollectionAssert.AreEqual(new byte[] { 0x01 }, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> accepts the fully-padded form "AQ==" and produces
    /// the same byte as the unpadded form. The lenient default permits either shape.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputIsFullyPaddedAQ_ShouldReturnSingleByte()
    {
        byte[] decoded = Base64Url.Decode("AQ==");

        CollectionAssert.AreEqual(new byte[] { 0x01 }, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> accepts "AQ=" — the lenient padding rules
    /// re-pad a non-multiple-of-four input by appending the missing padding characters before delegating to the
    /// BCL decoder, so a single trailing '=' is treated as partial padding rather than rejected.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputHasSinglePadding_ShouldNormaliseAndReturnSingleByte()
    {
        byte[] decoded = Base64Url.Decode("AQ=");

        CollectionAssert.AreEqual(new byte[] { 0x01 }, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> decodes the URL-safe alphabet characters '-' and
    /// '_' that replace '+' and '/' from the standard alphabet.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputUsesUrlSafeAlphabet_ShouldReturnHighBytes()
    {
        byte[] decoded = Base64Url.Decode("A-B_");

        Assert.HasCount(3, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Decode(string)" /> rejects the standard-alphabet characters '+' and
    /// '/' which are not part of the URL-safe alphabet.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInputUsesStandardAlphabet_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64Url.Decode("A+B/");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with
    /// <see cref="Base64Variant.UrlSafe" /> accepts unpadded input even when no explicit
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> flag is supplied. The URL-safe variant is the
    /// idiomatic unpadded form (JWT, OAuth), so the decoder treats AllowMissingPadding as implicit.
    /// </summary>
    [TestMethod]
    public void Base64Decode_WhenUrlSafeAndNoStyle_ShouldStillAcceptUnpaddedInput()
    {
        byte[] decoded = Base64.Decode("AQ", Base64Variant.UrlSafe, BaseFormatStyles.None);

        CollectionAssert.AreEqual(new byte[] { 0x01 }, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with
    /// <see cref="Base64Variant.Standard" /> rejects unpadded input — the standard variant requires canonical
    /// padding unless <see cref="BaseFormatStyles.AllowMissingPadding" /> is supplied explicitly.
    /// </summary>
    [TestMethod]
    public void Base64Decode_WhenStandardAndNoStyle_ShouldRejectUnpaddedInput()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("AQ", Base64Variant.Standard, BaseFormatStyles.None);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.Encode(byte[])" /> emits no '=' padding by default and matches the
    /// output of the underlying <see cref="Base64.Encode(byte[], Base64Variant)" /> with the URL-safe variant.
    /// </summary>
    [TestMethod]
    public void Encode_ShouldOmitPaddingAndMatchUnderlyingVariant()
    {
        byte[] source = [0x01];

        string actual = Base64Url.Encode(source);
        string viaVariant = Base64.Encode(source, Base64Variant.UrlSafe);

        Assert.AreEqual("AQ", actual);
        Assert.AreEqual(viaVariant, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64Url.IsValid(ReadOnlySpan{char})" /> accepts the canonical padded and
    /// unpadded URL-safe forms and rejects characters from the standard alphabet.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptCanonicalShapesAndRejectStandardAlphabet()
    {
        Assert.IsTrue(Base64Url.IsValid("AQ".AsSpan()));
        Assert.IsTrue(Base64Url.IsValid("AQ==".AsSpan()));
        Assert.IsTrue(Base64Url.IsValid("A-B_".AsSpan()));
        Assert.IsFalse(Base64Url.IsValid("A+B/".AsSpan()));
    }

    /// <summary>
    /// Documents that <see cref="Base64Url.IsValid(ReadOnlySpan{char})" /> applies a stricter padding-length
    /// check than the decoder, so the single-padding shape "AQ=" returns <see langword="false" /> from
    /// IsValid even though <see cref="Base64Url.Decode(string)" /> accepts and normalises the same input. The
    /// divergence is intentional pinning: any future reconciliation between IsValid and Decode should update
    /// this test along with the implementation.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputHasSinglePadding_ShouldReturnFalseEvenThoughDecodeSucceeds()
    {
        Assert.IsFalse(Base64Url.IsValid("AQ=".AsSpan()));

        // Decode still accepts the same input — the asymmetry is what the test pins.
        byte[] decoded = Base64Url.Decode("AQ=");
        CollectionAssert.AreEqual(new byte[] { 0x01 }, decoded);
    }
}
