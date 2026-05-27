// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AbaRoutingNumberContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="AbaRoutingNumber" />
/// with the published Federal Reserve Bank of Boston and FRB New York routing-number bodies.
/// </summary>
[TestClass]
public sealed class AbaRoutingNumberContractTests : CheckDigitContractTests<AbaRoutingNumber>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        AbaRoutingNumber.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        AbaRoutingNumber.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("FRB Boston routing body",   Payload: "01100001", CheckDigit: "5", FullValue: "011000015"),
        new("FRB New York routing body", Payload: "02100008", CheckDigit: "9", FullValue: "021000089"),
        new("all zeros",                 Payload: "00000000", CheckDigit: "0", FullValue: "000000000"),
    ];
}
