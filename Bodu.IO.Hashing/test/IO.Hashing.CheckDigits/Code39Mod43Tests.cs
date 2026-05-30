// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Code39Mod43Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Code39Mod43" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Code39Mod43Tests
    : AlphanumericCheckDigitAlgorithmTests<Code39Mod43Tests, Code39Mod43>
{
    /// <summary>
    /// Verifies that <see cref="Code39Mod43.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the body contains a character outside the Code 39 alphabet.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Code39Mod43.Compute("CODE#39".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Code39Mod43.Compute(ReadOnlySpan{char})" /> rejects lowercase letters, since the
    /// Code 39 alphabet is uppercase only.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsLowercaseLetter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Code39Mod43.Compute("code39".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Code39Mod43.IsValid(ReadOnlySpan{char})" /> rejects the empty span.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(Code39Mod43.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Code39Mod43.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body contains
    /// a character outside the Code 39 alphabet.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Code39Mod43.IsValid("CODE#39W".AsSpan()));
    }

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Code39Mod43.Compute(body);

    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Code 39 Mod 43",
        InputAlphabet = CheckDigitInputAlphabet.Code39,
        OutputAlphabet = CheckDigitOutputAlphabet.Code39,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Canonical",    Body = "CODE39", ExpectedCheck = 'W' },
            new() { Name = "DigitsOnly",   Body = "0",      ExpectedCheck = '0' },
            new() { Name = "LettersOnly",  Body = "HELLO",  ExpectedCheck = 'B' },
            new() { Name = "WithSpace",    Body = "A B",    ExpectedCheck = 'G' },
            new() { Name = "SymbolBody",   Body = "%",      ExpectedCheck = '%' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Code39Mod43.IsValid(valueIncludingCheck);
}
