// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.IsValid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{
    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> returns <see langword="true" /> for a canonical Bitcoin/Flickr
    /// input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCanonicalInput_ShouldReturnTrue()
    {
        Assert.IsTrue(Base58.IsValid("9Ajdvzr".AsSpan()));
    }

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
    public void IsValid_WhenExcludedCharacter_ShouldReturnFalse(char excluded)
    {
        Assert.IsFalse(Base58.IsValid(("2" + excluded + "2").AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid" /> returns <see langword="true" /> for empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Base58.IsValid(ReadOnlySpan<char>.Empty));
    }

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
}
