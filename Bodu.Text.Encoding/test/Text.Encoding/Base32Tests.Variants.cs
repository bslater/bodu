// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Variants.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> produces the RFC 4648
    /// §7 base32hex reference vectors when <see cref="Base32Variant.HexExtended" /> is selected.
    /// </summary>
    /// <param name="input">The input ASCII string.</param>
    /// <param name="expected">The expected base32hex output.</param>
    [DataTestMethod]
    [DataRow("", "")]
    [DataRow("f", "CO======")]
    [DataRow("fo", "CPNG====")]
    [DataRow("foo", "CPNMU===")]
    [DataRow("foob", "CPNMUOG=")]
    [DataRow("fooba", "CPNMUOJ1")]
    [DataRow("foobar", "CPNMUOJ1E8======")]
    public void Encode_WhenHexExtendedVariant_ShouldMatchRfc4648Section7Vectors(string input, string expected)
    {
        string actual = Base32.Encode(Ascii(input), Base32Variant.HexExtended);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> in the Crockford variant
    /// aliases <c>I</c>, <c>L</c> to <c>1</c> and <c>O</c> to <c>0</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCrockfordVariantWithAmbiguousAliases_ShouldNormaliseToCanonicalDigits()
    {
        byte[] canonical = Base32.Decode("D1G", Base32Variant.Crockford);
        byte[] aliasedI = Base32.Decode("DIG", Base32Variant.Crockford);
        byte[] aliasedL = Base32.Decode("DLG", Base32Variant.Crockford);

        CollectionAssert.AreEqual(canonical, aliasedI);
        CollectionAssert.AreEqual(canonical, aliasedL);

        byte[] canonicalZero = Base32.Decode("D0G", Base32Variant.Crockford);
        byte[] aliasedO = Base32.Decode("DOG", Base32Variant.Crockford);

        CollectionAssert.AreEqual(canonicalZero, aliasedO);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> in the Crockford variant
    /// rejects the excluded character <c>U</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCrockfordVariantWithExcludedU_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("DUG", Base32Variant.Crockford);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> in the Crockford
    /// variant omits padding by default.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCrockfordVariant_ShouldOmitPaddingByDefault()
    {
        string actual = Base32.Encode(Ascii("foo"), Base32Variant.Crockford);

        Assert.IsFalse(actual.Contains('='), "Crockford output should not include padding characters by default.");
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> in the Z-Base32
    /// variant emits the canonical lowercase alphabet.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZBase32Variant_ShouldEmitCanonicalLowerCaseAlphabet()
    {
        string actual = Base32.Encode(Ascii("foo"), Base32Variant.ZBase32);

        Assert.AreEqual(actual, actual.ToLowerInvariant(), "Z-Base32 output should be lower case.");
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> in the Z-Base32
    /// variant omits padding by default.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZBase32Variant_ShouldOmitPaddingByDefault()
    {
        string actual = Base32.Encode(Ascii("foo"), Base32Variant.ZBase32);

        Assert.IsFalse(actual.Contains('='), "Z-Base32 output should not include padding characters by default.");
    }
}
