// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SedolTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Sedol" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class SedolTests
    : AlphanumericCheckDigitAlgorithmTests<SedolTests, Sedol>
{

    /// <summary>
    /// Verifies that <see cref="Sedol.Compute(ReadOnlySpan{char})" /> rejects a body character that is not part of
    /// the SEDOL alphabet by throwing <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Sedol.Compute("B0WNL-".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Sedol.Compute(ReadOnlySpan{char})" /> rejects a body that contains a vowel.
    /// </summary>
    /// <param name="vowel">The vowel character to exercise.</param>
    [DataRow('A')]
    [DataRow('E')]
    [DataRow('I')]
    [DataRow('O')]
    [DataRow('U')]
    [TestMethod]
    public void Compute_WhenBodyContainsVowel_ShouldThrowExactly(char vowel)
    {
        string body = new(['1', vowel, '2', '3', '4', '5']);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Sedol.Compute(body.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Sedol.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body contains a
    /// non-alphanumeric character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Sedol.IsValid("B0WNL-7".AsSpan()));
        Assert.IsFalse(Sedol.IsValid("B0WNL 7".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Sedol.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body contains a
    /// vowel — vowels are not part of the SEDOL alphabet.
    /// </summary>
    /// <param name="vowel">The vowel character placed in the body.</param>
    [DataRow('A')]
    [DataRow('E')]
    [DataRow('I')]
    [DataRow('O')]
    [DataRow('U')]
    [TestMethod]
    public void IsValid_WhenBodyContainsVowel_ShouldReturnFalse(char vowel)
    {
        // Position 1 of an otherwise plausible SEDOL is replaced by a vowel.
        string sequence = new(['B', vowel, 'W', 'N', 'L', 'Y', '7']);
        Assert.IsFalse(Sedol.IsValid(sequence.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Sedol.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose check character is
    /// not a decimal digit.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckCharacterIsNotDigit_ShouldReturnFalse()
    {
        Assert.IsFalse(Sedol.IsValid("B0WNLYZ".AsSpan()));
        Assert.IsFalse(Sedol.IsValid("B0WNLY-".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Sedol.IsValid(ReadOnlySpan{char})" /> rejects the empty span. A full-sequence
    /// validator requires at least a check digit, so an empty input cannot satisfy the algorithm's invariant.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse() => Assert.IsFalse(Sedol.IsValid([]));

    /// <summary>
    /// Verifies that <see cref="Sedol.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose length is not
    /// exactly <see cref="Sedol.SequenceLength" />.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceLengthIsWrong_ShouldReturnFalse()
    {
        Assert.IsFalse(Sedol.IsValid("B0WNLY7X".AsSpan()));
        Assert.IsFalse(Sedol.IsValid("B0WNLY".AsSpan()));
    }

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Sedol.Compute(body);
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "SEDOL",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigits,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "NumericBody",      Body = "026349", ExpectedCheck = '4' },
            new() { Name = "AlphanumericBody", Body = "B0WNLY", ExpectedCheck = '7' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Sedol.IsValid(valueIncludingCheck);

}
