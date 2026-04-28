// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the internal <see cref="Alphanumeric" /> helpers used by ISO 7064 MOD 97-10, IBAN,
/// LEI, ISIN, SEDOL, and CUSIP. The validators are public-internal and reused by multiple check-digit families,
/// so they are tested directly to lock down both the accept and reject paths.
/// </summary>
[TestClass]
public sealed class AlphanumericTests
{
    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandLetterDigit" /> returns 0–9 for ASCII decimal digits.
    /// </summary>
    /// <param name="ch">The digit character.</param>
    /// <param name="expected">The expected numeric value.</param>
    [TestMethod]
    [DataRow('0', 0)]
    [DataRow('5', 5)]
    [DataRow('9', 9)]
    public void ExpandLetterDigit_WhenInputIsDecimalDigit_ShouldReturnZeroThroughNine(char ch, int expected)
    {
        Assert.AreEqual(expected, Alphanumeric.ExpandLetterDigit(ch));
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandLetterDigit" /> returns 10–35 for ASCII uppercase letters.
    /// </summary>
    /// <param name="ch">The letter character.</param>
    /// <param name="expected">The expected numeric value.</param>
    [TestMethod]
    [DataRow('A', 10)]
    [DataRow('M', 22)]
    [DataRow('Z', 35)]
    public void ExpandLetterDigit_WhenInputIsUppercaseLetter_ShouldReturnTenThroughThirtyFive(char ch, int expected)
    {
        Assert.AreEqual(expected, Alphanumeric.ExpandLetterDigit(ch));
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandLetterDigit" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for characters outside <c>0-9</c>/<c>A-Z</c>.
    /// </summary>
    [TestMethod]
    public void ExpandLetterDigit_WhenInputIsNotAlphanumeric_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Alphanumeric.ExpandLetterDigit('a');
        });
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandCusip" /> maps the historical sentinels <c>'*'</c>=36,
    /// <c>'@'</c>=37 and <c>'#'</c>=38.
    /// </summary>
    /// <param name="ch">The sentinel character.</param>
    /// <param name="expected">The expected numeric value.</param>
    [TestMethod]
    [DataRow('*', 36)]
    [DataRow('@', 37)]
    [DataRow('#', 38)]
    public void ExpandCusip_WhenInputIsCusipSentinel_ShouldReturnExpectedValue(char ch, int expected)
    {
        Assert.AreEqual(expected, Alphanumeric.ExpandCusip(ch));
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ExpandCusip" /> throws <see cref="ArgumentOutOfRangeException" />
    /// for any character outside the CUSIP alphabet.
    /// </summary>
    [TestMethod]
    public void ExpandCusip_WhenInputIsNotCusipCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Alphanumeric.ExpandCusip('!');
        });
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric" /> accepts an all-digit, all-letter, or
    /// mixed alphanumeric input without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateAlphanumeric_WhenAllCharsValid_ShouldNotThrow()
    {
        Alphanumeric.ValidateAlphanumeric("0123456789".AsSpan(), "value");
        Alphanumeric.ValidateAlphanumeric("ABCDEFGHIJKLMNOPQRSTUVWXYZ".AsSpan(), "value");
        Alphanumeric.ValidateAlphanumeric("AB12CD34".AsSpan(), "value");
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with the supplied <c>paramName</c> when any character is
    /// outside the alphabet.
    /// </summary>
    [TestMethod]
    public void ValidateAlphanumeric_WhenInputContainsLowercase_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Alphanumeric.ValidateAlphanumeric("AB12cd".AsSpan(), "value");
        });
        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when any character is a non-alphanumeric symbol.
    /// </summary>
    [TestMethod]
    public void ValidateAlphanumeric_WhenInputContainsSymbol_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Alphanumeric.ValidateAlphanumeric("AB-12".AsSpan(), "value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateAlphanumeric" /> accepts an empty span without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateAlphanumeric_WhenInputIsEmpty_ShouldNotThrow()
    {
        Alphanumeric.ValidateAlphanumeric(ReadOnlySpan<char>.Empty, "value");
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip" /> accepts ASCII alphanumeric input plus the three
    /// historical sentinels without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateCusip_WhenAllCharsValid_ShouldNotThrow()
    {
        Alphanumeric.ValidateCusip("01234567*".AsSpan(), "value");
        Alphanumeric.ValidateCusip("ABCDE@".AsSpan(), "value");
        Alphanumeric.ValidateCusip("XYZ123#".AsSpan(), "value");
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with the supplied <c>paramName</c> for any character outside
    /// the CUSIP alphabet.
    /// </summary>
    [TestMethod]
    public void ValidateCusip_WhenInputContainsInvalidSymbol_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Alphanumeric.ValidateCusip("AB-12".AsSpan(), "value");
        });
        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip" /> rejects lowercase letters.
    /// </summary>
    [TestMethod]
    public void ValidateCusip_WhenInputContainsLowercase_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Alphanumeric.ValidateCusip("AB12cd".AsSpan(), "value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.ValidateCusip" /> accepts an empty span without throwing.
    /// </summary>
    [TestMethod]
    public void ValidateCusip_WhenInputIsEmpty_ShouldNotThrow()
    {
        Alphanumeric.ValidateCusip(ReadOnlySpan<char>.Empty, "value");
    }

    /// <summary>
    /// Verifies that <see cref="Alphanumeric.IsVowel" /> returns <see langword="true" /> for the five
    /// uppercase Latin vowels and <see langword="false" /> otherwise.
    /// </summary>
    /// <param name="ch">The character under test.</param>
    /// <param name="expected">The expected boolean result.</param>
    [TestMethod]
    [DataRow('A', true)]
    [DataRow('E', true)]
    [DataRow('I', true)]
    [DataRow('O', true)]
    [DataRow('U', true)]
    [DataRow('B', false)]
    [DataRow('Z', false)]
    [DataRow('0', false)]
    [DataRow('a', false)]
    public void IsVowel_ShouldRecogniseUppercaseLatinVowelsOnly(char ch, bool expected)
    {
        Assert.AreEqual(expected, Alphanumeric.IsVowel(ch));
    }
}
