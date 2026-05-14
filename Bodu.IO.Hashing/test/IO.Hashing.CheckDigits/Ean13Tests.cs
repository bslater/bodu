// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ean13Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Ean13" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Ean13Tests
    : CheckDigitAlgorithmTests<Ean13Tests, Ean13>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "EAN-13",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "WikipediaEAN13", Body = "501234567890",  ExpectedCheck = '0' },
            new() { Name = "BookIsbnShape",  Body = "978030640615",  ExpectedCheck = '7' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Ean13.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Ean13.IsValid(digitsIncludingCheck);

    /// <summary>
    /// Verifies that <see cref="Ean13.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Ean13.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Ean13.IsValid("5012345678900X".AsSpan()));
        Assert.IsFalse(Ean13.IsValid("501234567890".AsSpan()));
    }
}
