// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isbn10Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Isbn10" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Isbn10Tests
    : AlphanumericCheckDigitAlgorithmTests<Isbn10Tests, Isbn10>
{

    /// <summary>
    /// Verifies that <see cref="Isbn10.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the body contains a non-digit character.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Isbn10.Compute("0306A0615".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Isbn10.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body contains a
    /// character outside the decimal-digit alphabet.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsLetter_ShouldReturnFalse()
    {
        Assert.IsFalse(Isbn10.IsValid("0306A06152".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Isbn10.IsValid(ReadOnlySpan{char})" /> returns <see langword="true" /> for an
    /// empty span — the documented short-circuit branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Isbn10.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Isbn10.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Isbn10.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Isbn10.IsValid("03064061520".AsSpan()));
        Assert.IsFalse(Isbn10.IsValid("030640615".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Isbn10.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose <c>'X'</c>
    /// sentinel appears somewhere other than the final check position.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenXAppearsInBody_ShouldReturnFalse()
    {
        Assert.IsFalse(Isbn10.IsValid("X306406152".AsSpan()));
    }

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Isbn10.Compute(body);
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISBN-10",
        InputAlphabet = CheckDigitInputAlphabet.DecimalDigits,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigitsOrX,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "HarryPotterPhilosophers", Body = "043942089", ExpectedCheck = 'X' },
            new() { Name = "WikipediaIsbn10",         Body = "030640615", ExpectedCheck = '2' },
            new() { Name = "AllZeros",                Body = "000000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Isbn10.IsValid(valueIncludingCheck);

}
