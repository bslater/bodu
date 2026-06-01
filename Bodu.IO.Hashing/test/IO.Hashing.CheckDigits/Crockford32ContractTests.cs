// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Crockford32ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Crockford32" /> with the worked
/// <c>"16J"</c> reference vector and bodies that resolve to the five check-only symbols. Bespoke decoding, case,
/// and guard coverage stays in <see cref="Crockford32Tests" />.
/// </summary>
[TestClass]
public sealed class Crockford32ContractTests : CheckDigitContractTests<Crockford32>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Crockford32.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Crockford32.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("Canonical",    Payload: "16J", CheckDigit: "D", FullValue: "16JD"),
        new("MaxDigit",     Payload: "Z",   CheckDigit: "Z", FullValue: "ZZ"),
        new("CheckStar",    Payload: "10",  CheckDigit: "*", FullValue: "10*"),
        new("CheckLetterU", Payload: "14",  CheckDigit: "U", FullValue: "14U"),
    ];
}
