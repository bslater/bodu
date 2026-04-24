// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IsinTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Isin" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class IsinTests : AlphanumericCheckDigitAlgorithmTests<IsinTests, Isin>
{
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISIN",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigits,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",         Body = "",            ExpectedCheck = '0' },
            new() { Name = "AppleUS",       Body = "US037833100", ExpectedCheck = '5' },
            new() { Name = "BritishGB",     Body = "GB000263494", ExpectedCheck = '6' },
            new() { Name = "NumericOnly",   Body = "00000000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Isin.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Isin.IsValid(valueIncludingCheck);

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

    /// <summary>
    /// Verifies that <see cref="Isin.IsValid(ReadOnlySpan{char})" /> rejects a full ISIN whose check character
    /// is a letter rather than a digit.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckCharacterIsLetter_ShouldReturnFalse()
    {
        Assert.IsFalse(Isin.IsValid("US037833100A".AsSpan()));
    }
}
