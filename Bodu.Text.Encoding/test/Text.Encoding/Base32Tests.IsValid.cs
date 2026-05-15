// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.IsValid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that <see cref="Base32.IsValid" /> returns <see langword="true" /> for a canonical Standard input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStandardCanonicalInput_ShouldReturnTrue()
    {
        Assert.IsTrue(Base32.IsValid("MZXW6YTBOI======".AsSpan()));
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
    /// Verifies that <see cref="Base32.IsValid" /> returns <see langword="true" /> for empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Base32.IsValid(ReadOnlySpan<char>.Empty));
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
}
