// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CusipTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Cusip" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class CusipTests : AlphanumericCheckDigitAlgorithmTests<CusipTests, Cusip>
{
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "CUSIP",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigits,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",             Body = "",         ExpectedCheck = '0' },
            new() { Name = "AppleInc",          Body = "03783310", ExpectedCheck = '0' },
            new() { Name = "MicrosoftCorp",     Body = "59491810", ExpectedCheck = '4' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Cusip.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Cusip.IsValid(valueIncludingCheck);

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Cusip.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Cusip.IsValid("0378331000".AsSpan()));
        Assert.IsFalse(Cusip.IsValid("03783310".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.Compute(ReadOnlySpan{char})" /> accepts the historical CUSIP sentinels
    /// <c>'*'</c>, <c>'@'</c>, and <c>'#'</c> without throwing.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsCusipSentinels_ShouldNotThrow()
    {
        _ = Cusip.Compute("1234567*".AsSpan());
        _ = Cusip.Compute("1234567@".AsSpan());
        _ = Cusip.Compute("1234567#".AsSpan());
    }
}
