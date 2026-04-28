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
public sealed partial class Isbn10Tests : AlphanumericCheckDigitAlgorithmTests<Isbn10Tests, Isbn10>
{
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
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Isbn10.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Isbn10.IsValid(valueIncludingCheck);

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
}
