// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LeiContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.Checksums.Contracts;

/// <summary>
/// Drives <see cref="MultiCharCheckDigitContractTests{TAlgorithm}" /> against <see cref="Lei" /> with
/// the GLEIF example LEI body and a fully-numeric baseline. LEI uses the ISO 7064 MOD 97-10 algorithm
/// over the 18-character payload and emits a 2-character decimal check sequence appended to the
/// payload.
/// </summary>
[TestClass]
public sealed class LeiContractTests : MultiCharCheckDigitContractTests<Lei>
{
    /// <inheritdoc />
    protected override string Compute(string payload) =>
        Lei.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Lei.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("GLEIF example",   Payload: "54930084UKLVMY22DS", CheckDigit: "16", FullValue: "54930084UKLVMY22DS16"),
        new("numeric body",    Payload: "123456789012345678", CheckDigit: "88", FullValue: "12345678901234567888"),
    ];
}
