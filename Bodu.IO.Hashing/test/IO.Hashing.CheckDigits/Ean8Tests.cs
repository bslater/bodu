// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ean8Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Ean8" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Ean8Tests
    : CheckDigitAlgorithmTests<Ean8Tests, Ean8>
{

    /// <summary>
    /// Verifies that <see cref="Ean8.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Ean8.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Ean8.IsValid("735135370".AsSpan()));
        Assert.IsFalse(Ean8.IsValid("7351353".AsSpan()));
    }

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Ean8.Compute(digits);
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "EAN-8",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "WikipediaEAN8", Body = "7351353", ExpectedCheck = '7' },
            new() { Name = "AllZeros",      Body = "0000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Ean8.IsValid(digitsIncludingCheck);

}
