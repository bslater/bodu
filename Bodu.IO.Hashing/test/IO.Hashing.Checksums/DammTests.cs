// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DammTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Damm" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class DammTests : CheckDigitAlgorithmTests<DammTests, Damm>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Damm",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",            Body = "",      ExpectedCheck = '0' },
            new() { Name = "SingleZero",       Body = "0",     ExpectedCheck = '0' },
            new() { Name = "SingleOne",        Body = "1",     ExpectedCheck = '3' },
            new() { Name = "WikipediaExample", Body = "572",   ExpectedCheck = '4' },
            new() { Name = "AscendingRun",     Body = "12345", ExpectedCheck = '9' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Damm.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Damm.IsValid(digitsIncludingCheck);
}
