// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Code39Mod43ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Code39Mod43" /> with the canonical
/// <c>"CODE39"</c> reference vector and a set of bodies that exercise the symbol portion of the alphabet. Bespoke
/// streaming and guard coverage stays in <see cref="Code39Mod43Tests" />.
/// </summary>
[TestClass]
public sealed class Code39Mod43ContractTests
    : CheckDigitContractTests<Code39Mod43>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Code39Mod43.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Code39Mod43.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("Canonical",   Payload: "CODE39", CheckDigit: "W", FullValue: "CODE39W"),
        new("LettersOnly", Payload: "HELLO",  CheckDigit: "B", FullValue: "HELLOB"),
        new("WithSpace",   Payload: "A B",    CheckDigit: "G", FullValue: "A BG"),
        new("SymbolBody",  Payload: "%",      CheckDigit: "%", FullValue: "%%"),
    ];
}
