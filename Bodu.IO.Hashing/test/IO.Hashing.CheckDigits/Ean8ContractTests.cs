// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ean8ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Ean8" /> with the
/// Wikipedia EAN-8 reference vector.
/// </summary>
[TestClass]
public sealed class Ean8ContractTests : CheckDigitContractTests<Ean8>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Ean8.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Ean8.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("Wikipedia EAN-8",  Payload: "7351353", CheckDigit: "7", FullValue: "73513537"),
        new("all zeros",        Payload: "0000000", CheckDigit: "0", FullValue: "00000000"),
    ];
}
