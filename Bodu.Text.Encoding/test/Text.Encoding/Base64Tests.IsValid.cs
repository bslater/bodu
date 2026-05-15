// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.IsValid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.IsValid" /> returns <see langword="true" /> for canonical Standard input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStandardCanonicalInput_ShouldReturnTrue()
    {
        Assert.IsTrue(Base64.IsValid("Zm9vYmFy".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsValid" /> returns <see langword="false" /> for invalid characters.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("Zm@v".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsValid" /> in the UrlSafe variant rejects <c>+</c> and <c>/</c>.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenUrlSafeVariantRejectsStandardSymbols_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("+//+".AsSpan(), Base64Variant.UrlSafe));
        Assert.IsTrue(Base64.IsValid("-__-".AsSpan(), Base64Variant.UrlSafe));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsValid" /> in the Standard variant rejects <c>-</c> and <c>_</c>.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStandardVariantRejectsUrlSafeSymbols_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("-__-".AsSpan(), Base64Variant.Standard));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsValid" /> returns <see langword="true" /> for empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Base64.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsValid" /> in the MIME variant accepts embedded whitespace implicitly.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenMimeVariantWithLineBreaks_ShouldAcceptWhitespace()
    {
        Assert.IsTrue(Base64.IsValid("Zm9v\r\nYmFy".AsSpan(), Base64Variant.Mime));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsBase64Digit" /> recognises Standard alphabet characters.
    /// </summary>
    [TestMethod]
    public void IsBase64Digit_WhenStandardSymbol_ShouldReturnTrue()
    {
        Assert.IsTrue(Base64.IsBase64Digit('A'));
        Assert.IsTrue(Base64.IsBase64Digit('z'));
        Assert.IsTrue(Base64.IsBase64Digit('0'));
        Assert.IsTrue(Base64.IsBase64Digit('+'));
        Assert.IsTrue(Base64.IsBase64Digit('/'));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsBase64Digit" /> rejects characters outside the Standard alphabet.
    /// </summary>
    [TestMethod]
    public void IsBase64Digit_WhenNotInStandardAlphabet_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsBase64Digit('-'));
        Assert.IsFalse(Base64.IsBase64Digit('_'));
        Assert.IsFalse(Base64.IsBase64Digit('='));
        Assert.IsFalse(Base64.IsBase64Digit('!'));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.IsBase64Digit" /> with the UrlSafe variant accepts <c>-</c> and <c>_</c> but
    /// rejects <c>+</c> and <c>/</c>.
    /// </summary>
    [TestMethod]
    public void IsBase64Digit_WhenUrlSafeVariant_ShouldAcceptSubstitutedSymbols()
    {
        Assert.IsTrue(Base64.IsBase64Digit('-', Base64Variant.UrlSafe));
        Assert.IsTrue(Base64.IsBase64Digit('_', Base64Variant.UrlSafe));
        Assert.IsFalse(Base64.IsBase64Digit('+', Base64Variant.UrlSafe));
        Assert.IsFalse(Base64.IsBase64Digit('/', Base64Variant.UrlSafe));
    }
}
