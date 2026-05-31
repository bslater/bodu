// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GummTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Gumm" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class GummTests
    : CheckDigitAlgorithmTests<GummTests, Gumm>
{
    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Gumm.Compute(digits);

    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Gumm",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",        Body = string.Empty, ExpectedCheck = '0' },
            new() { Name = "SingleZero",   Body = "0",          ExpectedCheck = '2' },
            new() { Name = "SingleOne",    Body = "1",          ExpectedCheck = '3' },
            new() { Name = "ThreeDigits",  Body = "236",        ExpectedCheck = '9' },
            new() { Name = "AscendingRun", Body = "12345",      ExpectedCheck = '7' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Gumm.IsValid(digitsIncludingCheck);
}
