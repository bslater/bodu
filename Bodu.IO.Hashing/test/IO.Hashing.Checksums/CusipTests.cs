// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CusipTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Cusip" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class CusipTests
    : AlphanumericCheckDigitAlgorithmTests<CusipTests, Cusip>
{

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
    /// Verifies that <see cref="Cusip.Compute(ReadOnlySpan{char})" /> rejects a body character that is not part of
    /// the CUSIP alphabet by throwing <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Cusip.Compute("0378331-".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> accepts the historical CUSIP punctuation
    /// sentinels (<c>'*'</c>, <c>'@'</c>, <c>'#'</c>) within the body when the resulting sequence is consistent.
    /// </summary>
    /// <param name="sentinel">The CUSIP sentinel under test.</param>
    [DataRow('*')]
    [DataRow('@')]
    [DataRow('#')]
    [TestMethod]
    public void IsValid_WhenBodyContainsCusipSentinel_ShouldAcceptValidSequence(char sentinel)
    {
        var body = "1234567" + sentinel;
        var check = Cusip.Compute(body.AsSpan());
        Assert.IsTrue(Cusip.IsValid((body + check).AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body contains a
    /// character outside <c>'0'</c>–<c>'9'</c>, <c>'A'</c>–<c>'Z'</c>, and the punctuation sentinels.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Cusip.IsValid("0378331-0".AsSpan()));
        Assert.IsFalse(Cusip.IsValid("037833 10".AsSpan()));
        Assert.IsFalse(Cusip.IsValid("037833a10".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose check character is
    /// not a decimal digit.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckCharacterIsNotDigit_ShouldReturnFalse()
    {
        Assert.IsFalse(Cusip.IsValid("03783310A".AsSpan()));
        Assert.IsFalse(Cusip.IsValid("03783310*".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Cusip.IsValid(ReadOnlySpan{char})" /> rejects the empty span. A full-sequence
    /// validator requires at least a check digit, so an empty input cannot satisfy the algorithm's invariant.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse() => Assert.IsFalse(Cusip.IsValid(ReadOnlySpan<char>.Empty));

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

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Cusip.Compute(body);
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
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Cusip.IsValid(valueIncludingCheck);

}
