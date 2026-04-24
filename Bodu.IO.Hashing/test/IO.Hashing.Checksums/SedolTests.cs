// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SedolTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Sedol" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class SedolTests : AlphanumericCheckDigitAlgorithmTests<SedolTests, Sedol>
{
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "SEDOL",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigits,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",            Body = "",       ExpectedCheck = '0' },
            new() { Name = "NumericBody",      Body = "026349", ExpectedCheck = '4' },
            new() { Name = "AlphanumericBody", Body = "B0WNLY", ExpectedCheck = '7' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Sedol.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Sedol.IsValid(valueIncludingCheck);

    /// <summary>
    /// Verifies that <see cref="Sedol.Compute(ReadOnlySpan{char})" /> rejects a body that contains a vowel.
    /// </summary>
    /// <param name="vowel">The vowel character to exercise.</param>
    [DataRow('A')]
    [DataRow('E')]
    [DataRow('I')]
    [DataRow('O')]
    [DataRow('U')]
    [DataTestMethod]
    public void Compute_WhenBodyContainsVowel_ShouldThrowArgumentOutOfRangeException(char vowel)
    {
        string body = new string(new[] { '1', vowel, '2', '3', '4', '5' });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Sedol.Compute(body.AsSpan());
        });
    }

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
}
