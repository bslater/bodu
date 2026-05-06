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
    /// <remarks>
    /// IBAN's canonical text form interleaves the check digits between the country code and BBAN
    /// (<c>CC + DD + BBAN</c>), so this override supplies positive vectors in that layout — and a handful of
    /// negative vectors covering whitespace, tampered check digits, and non-numeric check positions.
    /// </remarks>
    protected override IEnumerable<MultiCharCheckDigitIsValidKnownAnswer> GetIsValidKnownAnswers()
    {
        yield return new() { Name = "WikipediaGB",                  Value = "GB82WEST12345698765432",       ExpectedIsValid = true };
        yield return new() { Name = "WikipediaDE",                  Value = "DE89370400440532013000",       ExpectedIsValid = true };
        yield return new() { Name = "WikipediaFR",                  Value = "FR1420041010050500013M02606",  ExpectedIsValid = true };

        yield return new() { Name = "GroupedDisplayWithSpaces",     Value = "GB82 WEST 1234 5698 7654 32",  ExpectedIsValid = false };
        yield return new() { Name = "CheckDigitsTampered",          Value = "GB83WEST12345698765432",       ExpectedIsValid = false };
        yield return new() { Name = "CheckPositionsAreLetters",     Value = "GBXXWEST12345698765432",       ExpectedIsValid = false };
    }

    /// <inheritdoc />
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Iban.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> value) =>
        Iban.IsValid(value);
}
