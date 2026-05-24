// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.IsValid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that <see cref="Base32.IsBase32Digit" /> handles variant-specific alphabets.
    /// </summary>
    [TestMethod]
    public void IsBase32Digit_ForCrockfordVariant_ShouldRecognizeAlphabet()
    {
        Assert.IsTrue(Base32.IsBase32Digit('0', Base32Variant.Crockford));
        Assert.IsTrue(Base32.IsBase32Digit('1', Base32Variant.Crockford));
        Assert.IsTrue(Base32.IsBase32Digit('Z', Base32Variant.Crockford));
        Assert.IsFalse(Base32.IsBase32Digit('U', Base32Variant.Crockford));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsBase32Digit" /> rejects characters outside the Standard variant alphabet.
    /// </summary>
    [TestMethod]
    public void IsBase32Digit_WhenNotInStandardAlphabet_ShouldReturnFalse()
    {
        Assert.IsFalse(Base32.IsBase32Digit('0')); // not in standard
        Assert.IsFalse(Base32.IsBase32Digit('1')); // not in standard
        Assert.IsFalse(Base32.IsBase32Digit('='));
        Assert.IsFalse(Base32.IsBase32Digit('!'));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsBase32Digit" /> recognises a Standard variant symbol.
    /// </summary>
    [TestMethod]
    public void IsBase32Digit_WhenStandardSymbol_ShouldReturnTrue()
    {
        Assert.IsTrue(Base32.IsBase32Digit('M'));
        Assert.IsTrue(Base32.IsBase32Digit('m'));
        Assert.IsTrue(Base32.IsBase32Digit('2'));
        Assert.IsTrue(Base32.IsBase32Digit('7'));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base32.IsValid" /> rejects excessive padding even when
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> is set. The AllowMissingPadding leniency permits
    /// absent padding but never partial or excessive padding.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAllowMissingPaddingAndExcessivePadding_ShouldStillReturnFalse()
    {
        // "MY========" — 1 data char + 8 padding chars (canonical is 6). Invalid even with lenient padding.
        Assert.IsFalse(Base32.IsValid("MY========".AsSpan(), Base32Variant.Standard, BaseFormatStyles.AllowMissingPadding));

        // "MZXW6YTBOI=" — 10 data chars + 1 padding char (canonical is 6 for 10%8=2 data). Invalid.
        Assert.IsFalse(Base32.IsValid("MZXW6YTBOI=".AsSpan(), Base32Variant.Standard, BaseFormatStyles.AllowMissingPadding));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base32.IsValid" /> in the Crockford and Z-Base32 variants rejects any
    /// padding character because the specifications do not use padding.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCrockfordOrZBase32WithPaddingCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Base32.IsValid("D1G2=".AsSpan(), Base32Variant.Crockford));
        Assert.IsFalse(Base32.IsValid("ybnd=".AsSpan(), Base32Variant.ZBase32));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> returns <see langword="true" /> for empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Base32.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> accepts decorated input when the matching styles are set.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenIgnoreWhitespace_ShouldAcceptWhitespace()
    {
        Assert.IsTrue(Base32.IsValid(
            "MZ XW\t6Y\nTBOI======".AsSpan(),
            Base32Variant.Standard,
            BaseFormatStyles.IgnoreWhitespace));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> returns <see langword="false" /> for an invalid character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Base32.IsValid("MZXW@YTB".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> returns <see langword="false" /> when padding is interleaved with
    /// data characters.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenPaddingInMiddle_ShouldReturnFalse()
    {
        Assert.IsFalse(Base32.IsValid("MZ=XW6YT".AsSpan()));
    }
    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> returns <see langword="true" /> for a canonical Standard input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStandardCanonicalInput_ShouldReturnTrue()
    {
        Assert.IsTrue(Base32.IsValid("MZXW6YTBOI======".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> uses the variant alphabet for validation; the Standard alphabet
    /// rejects digit <c>0</c> and digit <c>1</c>, while the Crockford alphabet accepts them.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenVariantSpecificCharacters_ShouldRespectAlphabet()
    {
        Assert.IsFalse(Base32.IsValid("0AB".AsSpan(), Base32Variant.Standard));
        Assert.IsTrue(Base32.IsValid("0AB".AsSpan(), Base32Variant.Crockford));
    }

}
