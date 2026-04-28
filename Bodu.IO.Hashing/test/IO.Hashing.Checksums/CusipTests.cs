// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CusipTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

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

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> returns <see langword="true" /> for an
    /// empty input, matching the documented contract.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Cusip.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> resolves the historical CUSIP sentinels
    /// <c>'*'</c>, <c>'@'</c>, and <c>'#'</c> within the body — confirming that the corresponding decode
    /// branches are exercised.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsCusipSentinels_ShouldEvaluateAgainstComputedCheckDigit()
    {
        // Pair each sentinel-bearing body with its computed check digit so the verification is consistent.
        ReadOnlySpan<char> bodyStar = "1234567*".AsSpan();
        char checkStar = Cusip.Compute(bodyStar);
        Assert.IsTrue(Cusip.IsValid(($"1234567*{checkStar}").AsSpan()));

        ReadOnlySpan<char> bodyAt = "1234567@".AsSpan();
        char checkAt = Cusip.Compute(bodyAt);
        Assert.IsTrue(Cusip.IsValid(($"1234567@{checkAt}").AsSpan()));

        ReadOnlySpan<char> bodyHash = "1234567#".AsSpan();
        char checkHash = Cusip.Compute(bodyHash);
        Assert.IsTrue(Cusip.IsValid(($"1234567#{checkHash}").AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> returns <see langword="false" /> when the
    /// body contains a character outside the CUSIP alphabet.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsInvalidCharacter_ShouldReturnFalse()
    {
        // The lowercase 'a' is not part of the CUSIP alphabet; any otherwise-valid suffix must still be rejected.
        Assert.IsFalse(Cusip.IsValid("0378331a0".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> returns <see langword="false" /> when
    /// the trailing check character is not a decimal digit.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckCharacterIsNotADigit_ShouldReturnFalse()
    {
        Assert.IsFalse(Cusip.IsValid("03783310A".AsSpan()));
    }
}
