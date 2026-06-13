// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod11_2ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;
using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.Checksums.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Iso7064Mod11_2" /> with
/// the ISO 7064 standard example, small single-digit boundary cases, and a longer numeric body. The
/// algorithm's output alphabet includes the multiplicative <c>'X'</c> sentinel.
/// </summary>
[TestClass]
public sealed class Iso7064Mod11_2ContractTests
    : CheckDigitContractTests<Iso7064Mod11_2>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Iso7064Mod11_2.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Iso7064Mod11_2.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("ISO 7064 standard example", Payload: "0794",       CheckDigit: "0", FullValue: "07940"),
        new("single zero",               Payload: "0",          CheckDigit: "1", FullValue: "01"),
        new("longer numeric",            Payload: "1234567890", CheckDigit: "8", FullValue: "12345678908"),
    ];
}
