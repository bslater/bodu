// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AbaRoutingNumberTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="AbaRoutingNumber" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class AbaRoutingNumberTests : CheckDigitAlgorithmTests<AbaRoutingNumberTests, AbaRoutingNumber>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ABA",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",                  Body = "",         ExpectedCheck = '0' },
            new() { Name = "FrbBostonRoutingBody",   Body = "01100001", ExpectedCheck = '5' },
            new() { Name = "FrbNewYorkRoutingBody",  Body = "02100008", ExpectedCheck = '9' },
            new() { Name = "AllZeros",               Body = "00000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        AbaRoutingNumber.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        AbaRoutingNumber.IsValid(digitsIncludingCheck);

    /// <summary>
    /// Verifies that <see cref="AbaRoutingNumber.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length
    /// is not exactly <see cref="AbaRoutingNumber.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(AbaRoutingNumber.IsValid("0110000150".AsSpan()));
        Assert.IsFalse(AbaRoutingNumber.IsValid("01100001".AsSpan()));
    }

    /// <summary>
    /// Verifies that a real-world Federal Reserve Bank routing number round-trips through
    /// <see cref="AbaRoutingNumber.IsValid(ReadOnlySpan{char})" />.
    /// </summary>
    [TestMethod]
    public void IsValid_FrbBostonRoutingNumber_ShouldReturnTrue()
    {
        Assert.IsTrue(AbaRoutingNumber.IsValid("011000015".AsSpan()));
    }
}
