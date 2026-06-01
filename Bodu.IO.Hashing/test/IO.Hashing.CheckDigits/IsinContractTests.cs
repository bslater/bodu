// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IsinContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Isin" /> with the
/// canonical Apple US and British-issued reference vectors plus an all-zero baseline. ISIN combines
/// alphanumeric body characters with the Luhn check digit on the converted digit stream.
/// </summary>
[TestClass]
public sealed class IsinContractTests : CheckDigitContractTests<Isin>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Isin.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Isin.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("Apple US ISIN",      Payload: "US037833100", CheckDigit: "5", FullValue: "US0378331005"),
        new("British GB ISIN",    Payload: "GB000263494", CheckDigit: "6", FullValue: "GB0002634946"),
        new("all-zero numeric",   Payload: "00000000000", CheckDigit: "0", FullValue: "000000000000"),
    ];
}
