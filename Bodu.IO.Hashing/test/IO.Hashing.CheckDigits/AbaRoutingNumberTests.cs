// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AbaRoutingNumberTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="AbaRoutingNumber" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed partial class AbaRoutingNumberTests
    : CheckDigitAlgorithmTests<AbaRoutingNumberTests, AbaRoutingNumber>
{

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        AbaRoutingNumber.Compute(digits);
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ABA",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "FrbBostonRoutingBody",   Body = "01100001", ExpectedCheck = '5' },
            new() { Name = "FrbNewYorkRoutingBody",  Body = "02100008", ExpectedCheck = '9' },
            new() { Name = "AllZeros",               Body = "00000000", ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        AbaRoutingNumber.IsValid(digitsIncludingCheck);

}
