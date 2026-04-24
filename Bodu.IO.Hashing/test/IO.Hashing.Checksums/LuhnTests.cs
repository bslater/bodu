// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LuhnTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Luhn" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class LuhnTests : CheckDigitAlgorithmTests<LuhnTests, Luhn>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Luhn",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",            Body = "",                ExpectedCheck = '0' },
            new() { Name = "SingleZero",       Body = "0",               ExpectedCheck = '0' },
            new() { Name = "SingleOne",        Body = "1",               ExpectedCheck = '8' },
            new() { Name = "WikipediaExample", Body = "7992739871",      ExpectedCheck = '3' },
            new() { Name = "VisaTestCardBody", Body = "411111111111111", ExpectedCheck = '1' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Luhn.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Luhn.IsValid(digitsIncludingCheck);
}
