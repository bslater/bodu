// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Variants.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that the Standard variant alphabet (<c>A-Z 2-7</c>) is disjoint from the Crockford alphabet's lower
    /// half (<c>0-9</c>) in a way that the Crockford variant accepts digits the Standard variant rejects.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCrockfordDigit_ShouldRejectInStandardVariant()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("01234567", Base32Variant.Standard);
        });

        var crockfordResult = Base32.Decode("01234567", Base32Variant.Crockford);
        Assert.AreEqual(5, crockfordResult.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> in the Crockford variant
    /// aliases <c>I</c>, <c>L</c> to <c>1</c> and <c>O</c> to <c>0</c>. Uses 4-character groups (one valid terminal
    /// quantum) so the comparison is well-defined.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCrockfordVariantWithAmbiguousAliases_ShouldNormaliseToCanonicalDigits()
    {
        var canonical = Base32.Decode("D1G2", Base32Variant.Crockford);
        var aliasedI = Base32.Decode("DIG2", Base32Variant.Crockford);
        var aliasedL = Base32.Decode("DLG2", Base32Variant.Crockford);

        CollectionAssert.AreEqual(canonical, aliasedI);
        CollectionAssert.AreEqual(canonical, aliasedL);

        var canonicalZero = Base32.Decode("D0G2", Base32Variant.Crockford);
        var aliasedO = Base32.Decode("DOG2", Base32Variant.Crockford);

        CollectionAssert.AreEqual(canonicalZero, aliasedO);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> in the Crockford variant
    /// rejects the excluded character <c>U</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCrockfordVariantWithExcludedU_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("DUG2", Base32Variant.Crockford);
        });
    }

    /// <summary>
    /// Verifies that Z-Base32 encoded output does NOT decode meaningfully when treated as the Standard variant —
    /// the alphabets are disjoint enough that decoding produces wrong bytes or rejects.
    /// </summary>
    [TestMethod]
    public void Decode_WhenZBase32OutputDecodedAsStandard_ShouldProduceDifferentValueOrThrow()
    {
        var original = Ascii("foobar");
        var zEncoded = Base32.Encode(original, Base32Variant.ZBase32);

        try
        {
            var decodedAsStandard = Base32.Decode(zEncoded, Base32Variant.Standard, BaseFormatStyles.AllowMissingPadding);
            Assert.IsFalse(decodedAsStandard.SequenceEqual(original),
                "Decoding Z-Base32 output as Standard should not produce the original.");
        }
        catch (FormatException)
        {
            // Acceptable - Z-Base32 alphabet contains characters outside the Standard alphabet.
        }
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> in the Crockford
    /// variant omits padding by default.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCrockfordVariant_ShouldOmitPaddingByDefault()
    {
        var actual = Base32.Encode(Ascii("foo"), Base32Variant.Crockford);

        Assert.IsFalse(actual.Contains('='), "Crockford output should not include padding characters by default.");
    }
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
        var actual = Base32.Encode(Ascii(input), Base32Variant.HexExtended);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the HexExtended variant is sorted such that its alphabet ordering matches Base16's hex ordering
    /// for the first 16 symbols.
    /// </summary>
    [TestMethod]
    public void Encode_WhenHexExtendedVariant_ShouldOrderFirstSixteenSymbolsAsHex()
    {
        // The first 16 symbols of base32hex match Base16 digits: '0'-'9' then 'A'-'F'.
        // Encoding a byte 0x00 should produce 'CO======' (per RFC 4648 §10). Verify the first symbol is '0' for an
        // all-zero input.
        var zero = new byte[1];

        var actual = Base32.Encode(zero, Base32Variant.HexExtended);

        Assert.AreEqual('0', actual[0], "First HexExtended symbol of 0x00 byte should be '0'.");
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> in the Z-Base32
    /// variant emits the canonical lowercase alphabet.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZBase32Variant_ShouldEmitCanonicalLowerCaseAlphabet()
    {
        var actual = Base32.Encode(Ascii("foo"), Base32Variant.ZBase32);

        Assert.AreEqual(actual, actual.ToLowerInvariant(), "Z-Base32 output should be lower case.");
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> in the Z-Base32
    /// variant omits padding by default.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZBase32Variant_ShouldOmitPaddingByDefault()
    {
        var actual = Base32.Encode(Ascii("foo"), Base32Variant.ZBase32);

        Assert.IsFalse(actual.Contains('='), "Z-Base32 output should not include padding characters by default.");
    }

}
