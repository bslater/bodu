// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Gtin14ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Gtin14" /> with the GS1
/// example body and an all-zero baseline.
/// </summary>
[TestClass]
public sealed class Gtin14ContractTests
    : CheckDigitContractTests<Gtin14>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Gtin14.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Gtin14.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("GS1 example",   Payload: "1061414100041",   CheckDigit: "5", FullValue: "10614141000415"),
        new("all zeros",     Payload: "0000000000000",   CheckDigit: "0", FullValue: "00000000000000"),
    ];
}
