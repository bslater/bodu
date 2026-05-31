// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LuhnTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Luhn" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class LuhnTests
    : CheckDigitAlgorithmTests<LuhnTests, Luhn>
{

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Luhn.Compute(digits);
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Luhn",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",            Body = string.Empty,                ExpectedCheck = '0' },
            new() { Name = "SingleZero",       Body = "0",               ExpectedCheck = '0' },
            new() { Name = "SingleOne",        Body = "1",               ExpectedCheck = '8' },
            new() { Name = "WikipediaExample", Body = "7992739871",      ExpectedCheck = '3' },
            new() { Name = "VisaTestCardBody", Body = "411111111111111", ExpectedCheck = '1' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Luhn.IsValid(digitsIncludingCheck);

}
