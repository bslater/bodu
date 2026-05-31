// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.IsValid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that <see cref="Base58.IsBase58Digit" /> recognises Bitcoin/Flickr alphabet characters.
    /// </summary>
    [TestMethod]
    public void IsBase58Digit_WhenBitcoinFlickrSymbol_ShouldReturnTrue()
    {
        Assert.IsTrue(Base58.IsBase58Digit('1'));
        Assert.IsTrue(Base58.IsBase58Digit('Z'));
        Assert.IsTrue(Base58.IsBase58Digit('a'));
        Assert.IsTrue(Base58.IsBase58Digit('z'));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.IsBase58Digit" /> rejects excluded characters.
    /// </summary>
    [TestMethod]
    public void IsBase58Digit_WhenExcludedCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Base58.IsBase58Digit('0'));
        Assert.IsFalse(Base58.IsBase58Digit('O'));
        Assert.IsFalse(Base58.IsBase58Digit('I'));
        Assert.IsFalse(Base58.IsBase58Digit('l'));
        Assert.IsFalse(Base58.IsBase58Digit('!'));
    }
    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> returns <see langword="true" /> for a canonical Bitcoin/Flickr
    /// input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCanonicalInput_ShouldReturnTrue() => Assert.IsTrue(Base58.IsValid("9Ajdvzr".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> returns <see langword="true" /> for empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue() => Assert.IsTrue(Base58.IsValid(ReadOnlySpan<char>.Empty));

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> returns <see langword="false" /> for excluded ambiguous
    /// characters.
    /// </summary>
    /// <param name="excluded">An excluded ambiguous character.</param>
    [DataTestMethod]
    [DataRow('0')]
    [DataRow('O')]
    [DataRow('I')]
    [DataRow('l')]
    public void IsValid_WhenExcludedCharacter_ShouldReturnFalse(char excluded) => Assert.IsFalse(Base58.IsValid(("2" + excluded + "2").AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> with <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates
    /// whitespace anywhere in the input — exercises the whitespace-skip branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenIgnoreWhitespaceAndInputContainsWhitespace_ShouldReturnTrue() => Assert.IsTrue(Base58.IsValid("9 Aj\tdv\rz\nr".AsSpan(), Base58Variant.BitcoinFlickr, BaseFormatStyles.IgnoreWhitespace));

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> returns <see langword="false" /> for input containing a character
    /// past the 128-entry lookup table range — exercises the <c>c &gt;= lookup.Length</c> branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputContainsCharAboveLookupRange_ShouldReturnFalse() =>
        // 'ÿ' (U+00FF) is outside the 128-entry lookup.
        Assert.IsFalse(Base58.IsValid("9Ajdvÿzr".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> without <see cref="BaseFormatStyles.IgnoreWhitespace" /> rejects
    /// input containing whitespace.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInputContainsWhitespaceWithoutFlag_ShouldReturnFalse() => Assert.IsFalse(Base58.IsValid("9 Ajdvzr".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> recognises the Ripple variant alphabet — driving the variant
    /// branch independently from the Bitcoin/Flickr default.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenRippleVariantAndRippleAlphabetInput_ShouldReturnTrue() =>
        // "rpshn" are the first five Ripple alphabet symbols.
        Assert.IsTrue(Base58.IsValid("rpshn".AsSpan(), Base58Variant.Ripple));

}
