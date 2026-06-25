// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Variants.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with the
    /// <see cref="Base64Variant.UrlSafe" /> variant accepts <c>-</c> and <c>_</c> as substitutes for <c>+</c> and
    /// <c>/</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUrlSafeVariantWithMinusAndUnderscore_ShouldDecodeAsPlusAndSlash()
    {
        // Standard "+//+" - encoded form of 0xFB, 0xFF, 0xFE; URL-safe equivalent is "-__-".
        byte[] urlSafe = Base64.Decode("-__-", Base64Variant.UrlSafe);
        byte[] standard = Base64.Decode("+//+");

        CollectionAssert.AreEqual(standard, urlSafe);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with the
    /// <see cref="Base64Variant.UrlSafe" /> variant recovers bytes encoded with the URL-safe alphabet, without
    /// padding.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUrlSafeVariantWithoutPadding_ShouldRecoverOriginalBytes()
    {
        byte[] original = new byte[] { 0xFB, 0xFF, 0xFE, 0xFB, 0xFF };

        string encoded = Base64.Encode(original, Base64Variant.UrlSafe);
        byte[] decoded = Base64.Decode(encoded, Base64Variant.UrlSafe);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> with the
    /// <see cref="Base64Variant.Mime" /> variant emits padding (MIME mandates it).
    /// </summary>
    [TestMethod]
    public void Encode_WhenMimeVariant_ShouldEmitPaddingByDefault()
    {
        string actual = Base64.Encode(Ascii("foo"), Base64Variant.Mime);

        Assert.AreEqual("Zm9v", actual);

        string actualPadded = Base64.Encode(Ascii("foob"), Base64Variant.Mime);
        Assert.EndsWith("==", actualPadded, "MIME output should retain padding for non-aligned inputs.");
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> with
    /// <see cref="Base64Variant.UrlSafe" /> omits padding by default.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUrlSafeVariant_ShouldOmitPaddingByDefault()
    {
        string actual = Base64.Encode(Ascii("foo"), Base64Variant.UrlSafe);

        Assert.IsFalse(actual.Contains('='), "URL-safe output should not include padding by default.");
    }
    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> with
    /// <see cref="Base64Variant.UrlSafe" /> swaps the <c>+</c> and <c>/</c> characters for <c>-</c> and <c>_</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUrlSafeVariant_ShouldSwapPlusAndSlash()
    {
        byte[] bytes = new byte[] { 0xFB, 0xFF, 0xFE };

        string standard = Base64.Encode(bytes);
        string urlSafe = Base64.Encode(bytes, Base64Variant.UrlSafe);

        Assert.IsTrue(standard.Contains('+') || standard.Contains('/'),
            "Test vector should produce at least one '+' or '/' character in the Standard variant.");
        Assert.IsFalse(urlSafe.Contains('+'), "URL-safe output must not contain '+'.");
        Assert.IsFalse(urlSafe.Contains('/'), "URL-safe output must not contain '/'.");
    }

}
