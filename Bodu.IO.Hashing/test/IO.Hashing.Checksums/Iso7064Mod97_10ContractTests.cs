// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod97_10ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.Checksums.Contracts;

/// <summary>
/// Drives <see cref="MultiCharCheckDigitContractTests{TAlgorithm}" /> against
/// <see cref="Iso7064Mod97_10" /> with the ISO 7064 standard example, an IBAN-rearranged body, an LEI
/// payload, and an all-letters baseline. The algorithm emits a 2-character decimal check sequence.
/// </summary>
[TestClass]
public sealed class Iso7064Mod97_10ContractTests : MultiCharCheckDigitContractTests<Iso7064Mod97_10>
{
    /// <inheritdoc />
    protected override string Compute(string payload) =>
        Iso7064Mod97_10.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Iso7064Mod97_10.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("ISO 7064 standard example", Payload: "794",                  CheckDigit: "44", FullValue: "79444"),
        new("IBAN rearranged",           Payload: "WEST12345698765432GB", CheckDigit: "82", FullValue: "WEST12345698765432GB82"),
        new("LEI body",                  Payload: "54930084UKLVMY22DS",   CheckDigit: "16", FullValue: "54930084UKLVMY22DS16"),
        new("all letters",               Payload: "ABCDEFGHIJ",           CheckDigit: "46", FullValue: "ABCDEFGHIJ46"),
    ];
}
