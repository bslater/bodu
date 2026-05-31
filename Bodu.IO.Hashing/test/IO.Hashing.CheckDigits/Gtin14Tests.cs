// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Gtin14Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Gtin14" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Gtin14Tests
    : CheckDigitAlgorithmTests<Gtin14Tests, Gtin14>
{

    /// <summary>
    /// Verifies that <see cref="Gtin14.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Gtin14.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Gtin14.IsValid("106141410004150".AsSpan()));
        Assert.IsFalse(Gtin14.IsValid("1061414100041".AsSpan()));
    }

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Gtin14.Compute(digits);
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "GTIN-14",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "GS1Example",    Body = "1061414100041",  ExpectedCheck = '5' },
            new() { Name = "AllZeros",      Body = "0000000000000",  ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Gtin14.IsValid(digitsIncludingCheck);

}
