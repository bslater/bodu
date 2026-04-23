// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VerhoeffTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Verhoeff" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class VerhoeffTests : CheckDigitAlgorithmTests<VerhoeffTests, Verhoeff>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Verhoeff",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",            Body = "",      ExpectedCheck = '0' },
            new() { Name = "SingleZero",       Body = "0",     ExpectedCheck = '4' },
            new() { Name = "WikipediaExample", Body = "236",   ExpectedCheck = '3' },
            new() { Name = "AscendingRun",     Body = "12345", ExpectedCheck = '1' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Verhoeff.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Verhoeff.IsValid(digitsIncludingCheck);
}
