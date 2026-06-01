// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod11_2Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Iso7064Mod11_2" /> check-character algorithm.
/// </summary>
[TestClass]
public sealed class Iso7064Mod11_2Tests
    : AlphanumericCheckDigitAlgorithmTests<Iso7064Mod11_2Tests, Iso7064Mod11_2>
{

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when invoked with a non-digit input.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Iso7064Mod11_2.Compute("07X4".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body
    /// contains a non-digit character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsLetter_ShouldReturnFalse() => Assert.IsFalse(Iso7064Mod11_2.IsValid("07A40".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose check
    /// character is neither a decimal digit nor the <c>'X'</c> sentinel.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckCharacterIsInvalid_ShouldReturnFalse()
    {
        Assert.IsFalse(Iso7064Mod11_2.IsValid("0794Y".AsSpan()));
        Assert.IsFalse(Iso7064Mod11_2.IsValid("0794-".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.IsValid(ReadOnlySpan{char})" /> accepts the sentinel <c>'X'</c>
    /// in the trailing check position and rejects it elsewhere.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckPositionIsX_ShouldBeInterpretedAsTen()
    {
        // '1' computes to 'X'; concatenated sequence is therefore valid.
        Assert.IsTrue(Iso7064Mod11_2.IsValid("1X".AsSpan()));
        Assert.IsFalse(Iso7064Mod11_2.IsValid("X1".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.IsValid(ReadOnlySpan{char})" /> rejects the empty span. A
    /// full-sequence validator requires at least a check character, so an empty input cannot satisfy the
    /// algorithm's invariant.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse() => Assert.IsFalse(Iso7064Mod11_2.IsValid([]));

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Iso7064Mod11_2.Compute(body);
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISO 7064 MOD 11-2",
        InputAlphabet = CheckDigitInputAlphabet.DecimalDigits,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigitsOrX,
        EmptyCheckDigit = '1',
        KnownAnswers =
        [
            new() { Name = "StandardExample",     Body = "0794",      ExpectedCheck = '0' },
            new() { Name = "SingleZero",          Body = "0",         ExpectedCheck = '1' },
            new() { Name = "SingleOne",           Body = "1",         ExpectedCheck = 'X' },
            new() { Name = "LongerNumeric",       Body = "1234567890", ExpectedCheck = '8' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Iso7064Mod11_2.IsValid(valueIncludingCheck);

}
