// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IbanTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Iban" /> check-code algorithm.
/// </summary>
[TestClass]
public sealed class IbanTests : MultiCharCheckDigitAlgorithmTests<IbanTests, Iban>
{
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
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Iban.Compute(body);

    /// <inheritdoc />
    /// <remarks>
    /// The base harness supplies <c>body + check</c> concatenated. For IBAN, the check digits live in positions 2
    /// and 3 of the canonical form (not at the end); this override rearranges <c>CC + BBAN + DD</c> into the
    /// canonical <c>CC + DD + BBAN</c> before calling <see cref="Iban.IsValid(ReadOnlySpan{char})" />.
    /// </remarks>
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck)
    {
        if (valueIncludingCheck.Length < 4) return Iban.IsValid(valueIncludingCheck);

        int bodyLen = valueIncludingCheck.Length - 2;
        char[] canonical = new char[valueIncludingCheck.Length];
        valueIncludingCheck.Slice(0, 2).CopyTo(canonical);                        // CC
        valueIncludingCheck.Slice(bodyLen, 2).CopyTo(canonical.AsSpan(2));        // DD
        valueIncludingCheck.Slice(2, bodyLen - 2).CopyTo(canonical.AsSpan(4));    // BBAN
        return Iban.IsValid(canonical.AsSpan());
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> accepts the canonical IBAN text form.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCanonicalIbanIsValid_ShouldReturnTrue()
    {
        Assert.IsTrue(Iban.IsValid("GB82WEST12345698765432".AsSpan()));
        Assert.IsTrue(Iban.IsValid("DE89370400440532013000".AsSpan()));
        Assert.IsTrue(Iban.IsValid("FR1420041010050500013M02606".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> rejects an IBAN with whitespace in the
    /// conventional grouped display form.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenIbanContainsWhitespace_ShouldReturnFalse()
    {
        Assert.IsFalse(Iban.IsValid("GB82 WEST 1234 5698 7654 32".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> rejects an IBAN whose check digits are
    /// tampered.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckDigitsTampered_ShouldReturnFalse()
    {
        Assert.IsFalse(Iban.IsValid("GB83WEST12345698765432".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Iban.IsValid(ReadOnlySpan{char})" /> rejects an IBAN where the check positions
    /// are not decimal digits.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckPositionsAreLetters_ShouldReturnFalse()
    {
        Assert.IsFalse(Iban.IsValid("GBXXWEST12345698765432".AsSpan()));
    }
}
