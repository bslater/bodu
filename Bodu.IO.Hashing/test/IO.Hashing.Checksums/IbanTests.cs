// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IbanTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Iban" /> check-code algorithm.
/// </summary>
[TestClass]
public sealed class IbanTests
    : MultiCharCheckDigitAlgorithmTests<IbanTests, Iban>
{

    /// <summary>
    /// Verifies that <see cref="Iban.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the body contains a non-alphanumeric character.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Iban.Compute("GBWEST123-5698765432".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose BBAN contains a
    /// non-alphanumeric character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenBbanContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Iban.IsValid("GB82WEST123456-8765432".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> rejects a sequence whose country-code prefix
    /// contains a non-alphanumeric character. Exercises the rearrangement loop's second pass when the prefix
    /// positions fail <c>TryFold</c>.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCountryCodePrefixContainsInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Iban.IsValid("@B82WEST12345698765432".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> returns <see langword="true" /> for an empty
    /// span — the documented short-circuit branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Iban.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> rejects sequences shorter than four
    /// characters — the minimum CC+DD prefix length.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsShorterThanMinimum_ShouldReturnFalse()
    {
        Assert.IsFalse(Iban.IsValid("GB".AsSpan()));
        Assert.IsFalse(Iban.IsValid("GB8".AsSpan()));
    }

    /// <inheritdoc />
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Iban.Compute(body);

    /// <inheritdoc />
    /// <remarks>
    /// IBAN's canonical text form interleaves the check digits between the country code and BBAN
    /// (<c>CC + DD + BBAN</c>), so this override supplies positive vectors in that layout — and a handful of
    /// negative vectors covering whitespace, tampered check digits, and non-numeric check positions.
    /// </remarks>
    protected override IEnumerable<MultiCharCheckDigitIsValidKnownAnswer> GetIsValidKnownAnswers()
    {
        yield return new() { Name = "WikipediaGB", Value = "GB82WEST12345698765432", ExpectedIsValid = true };
        yield return new() { Name = "WikipediaDE", Value = "DE89370400440532013000", ExpectedIsValid = true };
        yield return new() { Name = "WikipediaFR", Value = "FR1420041010050500013M02606", ExpectedIsValid = true };

        yield return new() { Name = "GroupedDisplayWithSpaces", Value = "GB82 WEST 1234 5698 7654 32", ExpectedIsValid = false };
        yield return new() { Name = "CheckDigitsTampered", Value = "GB83WEST12345698765432", ExpectedIsValid = false };
        yield return new() { Name = "CheckPositionsAreLetters", Value = "GBXXWEST12345698765432", ExpectedIsValid = false };
    }
    /// <inheritdoc />
    protected override MultiCharCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "IBAN",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        CheckLength = 2,
        KnownAnswers =
        [
            new() { Name = "WikipediaGB",  Body = "GBWEST12345698765432",        ExpectedCheck = "82" },
            new() { Name = "WikipediaDE",  Body = "DE370400440532013000",        ExpectedCheck = "89" },
            new() { Name = "WikipediaFR",  Body = "FR20041010050500013M02606",  ExpectedCheck = "14" },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> value) =>
        Iban.IsValid(value);

}
