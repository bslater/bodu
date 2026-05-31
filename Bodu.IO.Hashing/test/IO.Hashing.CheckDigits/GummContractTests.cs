// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GummContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Gumm" /> with a small set of
/// self-consistent vectors. Bespoke streaming coverage stays in <see cref="GummTests" />, and the exhaustive
/// single-error / transposition detection proof stays in <c>GummTests.ErrorDetection</c>.
/// </summary>
[TestClass]
public sealed class GummContractTests : CheckDigitContractTests<Gumm>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Gumm.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Gumm.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("single zero",   Payload: "0",     CheckDigit: "2", FullValue: "02"),
        new("three digits",  Payload: "236",   CheckDigit: "9", FullValue: "2369"),
        new("ascending 1..5", Payload: "12345", CheckDigit: "7", FullValue: "123457"),
    ];
}
