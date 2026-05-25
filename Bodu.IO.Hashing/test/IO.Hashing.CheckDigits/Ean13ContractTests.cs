// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ean13ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Ean13" /> with the
/// Wikipedia EAN-13 reference vector and an ISBN-shaped 13-digit payload.
/// </summary>
[TestClass]
public sealed class Ean13ContractTests : CheckDigitContractTests<Ean13>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Ean13.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Ean13.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } = new CheckDigitKat[]
    {
        new("Wikipedia EAN-13",   Payload: "501234567890", CheckDigit: "0", FullValue: "5012345678900"),
        new("book ISBN shape",    Payload: "978030640615", CheckDigit: "7", FullValue: "9780306406157"),
    };
}
