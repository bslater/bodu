// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod97_10Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Iso7064Mod97_10" /> check-code algorithm.
/// </summary>
[TestClass]
public sealed class Iso7064Mod97_10Tests
    : MultiCharCheckDigitAlgorithmTests<Iso7064Mod97_10Tests, Iso7064Mod97_10>
{

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod97_10.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when invoked with a body that contains a non-alphanumeric
    /// character.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Iso7064Mod97_10.Compute("79-44".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod97_10.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose body
    /// contains a non-alphanumeric character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBodyContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Iso7064Mod97_10.IsValid("WEST 12345698765432GB82".AsSpan()));
        Assert.IsFalse(Iso7064Mod97_10.IsValid("WEST-12345698765432GB82".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod97_10.IsValid(ReadOnlySpan{char})" /> returns <see langword="true" />
    /// for an empty span — the documented short-circuit branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Iso7064Mod97_10.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod97_10.IsValid(ReadOnlySpan{char})" /> accepts a sequence whose body is
    /// purely numeric.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceUsesNumericBody_ShouldReturnTrue()
    {
        // 79444 — published worked example for MOD 97-10.
        Assert.IsTrue(Iso7064Mod97_10.IsValid("79444".AsSpan()));
    }

    /// <inheritdoc />
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Iso7064Mod97_10.Compute(body);
    /// <inheritdoc />
    protected override MultiCharCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISO 7064 MOD 97-10",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        CheckLength = 2,
        EmptyCheckDigits = "01",
        KnownAnswers =
        [
            new() { Name = "StandardExampleNumeric", Body = "794",                ExpectedCheck = "44" },
            new() { Name = "IbanRearranged",         Body = "WEST12345698765432GB", ExpectedCheck = "82" },
            new() { Name = "LeiBody",                Body = "54930084UKLVMY22DS",   ExpectedCheck = "16" },
            new() { Name = "AllLetters",             Body = "ABCDEFGHIJ",           ExpectedCheck = "46" },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> value) =>
        Iso7064Mod97_10.IsValid(value);

}
