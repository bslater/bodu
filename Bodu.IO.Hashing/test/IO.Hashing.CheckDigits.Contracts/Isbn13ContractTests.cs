// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isbn13ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Isbn13" /> with the
/// Wikipedia ISBN-13 reference, Knuth Volume 1 ISBN-13, and an all-zero baseline.
/// </summary>
[TestClass]
public sealed class Isbn13ContractTests
    : CheckDigitContractTests<Isbn13>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Isbn13.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Isbn13.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("Wikipedia ISBN-13",   Payload: "978030640615", CheckDigit: "7", FullValue: "9780306406157"),
        new("Knuth Volume 1",      Payload: "978020137962", CheckDigit: "4", FullValue: "9780201379624"),
        new("all zeros",           Payload: "000000000000", CheckDigit: "0", FullValue: "0000000000000"),
    ];
}
