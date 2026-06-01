// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CusipContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.Checksums.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Cusip" /> with the
/// Apple Inc. and Microsoft Corp. published CUSIP body vectors.
/// </summary>
[TestClass]
public sealed class CusipContractTests : CheckDigitContractTests<Cusip>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Cusip.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Cusip.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("Apple Inc.",        Payload: "03783310", CheckDigit: "0", FullValue: "037833100"),
        new("Microsoft Corp.",   Payload: "59491810", CheckDigit: "4", FullValue: "594918104"),
    ];
}
