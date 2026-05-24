// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IsinTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Isin" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class IsinTests
    : AlphanumericCheckDigitAlgorithmTests<IsinTests, Isin>
{

    /// <summary>
    /// Verifies that <see cref="Isin.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the body contains a character outside the alphanumeric
    /// uppercase alphabet.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Isin.Compute("US037833-00".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Isin.IsValid(ReadOnlySpan{char})" /> rejects a full ISIN whose body contains a
    /// non-alphanumeric character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Isin.IsValid("US0378331-05".AsSpan()));
        Assert.IsFalse(Isin.IsValid("US 378331005".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Isin.IsValid(ReadOnlySpan{char})" /> rejects a full ISIN whose check character
    /// is a letter rather than a digit.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckCharacterIsLetter_ShouldReturnFalse()
    {
        Assert.IsFalse(Isin.IsValid("US037833100A".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Isin.IsValid(ReadOnlySpan{char})" /> rejects the empty span. A full-sequence
    /// validator requires at least a check digit, so an empty input cannot satisfy the algorithm's invariant.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(Isin.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Isin.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Isin.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Isin.IsValid("US03783310050".AsSpan()));
        Assert.IsFalse(Isin.IsValid("US037833100".AsSpan()));
    }

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Isin.Compute(body);
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISIN",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigits,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "AppleUS",       Body = "US037833100", ExpectedCheck = '5' },
            new() { Name = "BritishGB",     Body = "GB000263494", ExpectedCheck = '6' },
            new() { Name = "NumericOnly",   Body = "00000000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Isin.IsValid(valueIncludingCheck);

}
