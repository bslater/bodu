// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UpcATests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="UpcA" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class UpcATests : CheckDigitAlgorithmTests<UpcATests, UpcA>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "UPC-A",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "WikipediaUPCA",  Body = "03600029145", ExpectedCheck = '2' },
            new() { Name = "AllZeros",       Body = "00000000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        UpcA.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        UpcA.IsValid(digitsIncludingCheck);

    /// <summary>
    /// Verifies that <see cref="UpcA.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="UpcA.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(UpcA.IsValid("0360002914520".AsSpan()));
        Assert.IsFalse(UpcA.IsValid("03600029145".AsSpan()));
    }
}
