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

    /// <summary>
    /// Regression: verifies that <see cref="Base64.IsValid" /> rejects excessive padding even in strict mode.
    /// <c>TQ===</c> has three padding characters where the canonical count for a 2-symbol terminal quantum is one.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenExcessivePadding_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("TQ===".AsSpan()), "Three padding chars after 2 data chars is invalid.");
        Assert.IsFalse(Base64.IsValid("T===".AsSpan()), "Three padding chars after 1 data char is invalid (and 1 data char itself).");
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base64.IsValid" /> rejects excessive padding even when
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> is set. AllowMissingPadding permits absent padding but
    /// never partial or excessive padding.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAllowMissingPaddingAndExcessivePadding_ShouldStillReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("TQ===".AsSpan(), Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding));
        Assert.IsFalse(Base64.IsValid("Zm9vYmE==".AsSpan(), Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding),
            "Two padding chars after 5 data chars is invalid (canonical is one).");
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base64.IsValid" /> rejects single-symbol terminal quantum
    /// (one Base64 character cannot represent any whole byte).
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSingleSymbolTerminalQuantum_ShouldReturnFalse()
    {
        Assert.IsFalse(Base64.IsValid("A".AsSpan()));
        Assert.IsFalse(Base64.IsValid("A".AsSpan(), Base64Variant.UrlSafe));
        Assert.IsFalse(Base64.IsValid("Zm9vA".AsSpan(), Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding));
    }
}
