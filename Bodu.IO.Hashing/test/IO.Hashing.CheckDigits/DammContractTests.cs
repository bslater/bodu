// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DammContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Damm" /> with the
/// Wikipedia reference vector and a small ascending-digit run. Bespoke streaming/append/error-
/// detection coverage stays in the existing <c>DammTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class DammContractTests : CheckDigitContractTests<Damm>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Damm.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Damm.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("single zero",        Payload: "0",      CheckDigit: "0", FullValue: "00"),
        new("single one",         Payload: "1",      CheckDigit: "3", FullValue: "13"),
        new("Wikipedia example",  Payload: "572",    CheckDigit: "4", FullValue: "5724"),
        new("ascending 1..5",     Payload: "12345",  CheckDigit: "9", FullValue: "123459"),
    ];
}
