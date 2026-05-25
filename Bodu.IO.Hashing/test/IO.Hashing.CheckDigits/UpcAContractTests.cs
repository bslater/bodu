// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UpcAContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="UpcA" /> with the
/// Wikipedia UPC-A reference vector.
/// </summary>
[TestClass]
public sealed class UpcAContractTests : CheckDigitContractTests<UpcA>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        UpcA.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        UpcA.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } = new CheckDigitKat[]
    {
        new("Wikipedia UPC-A",  Payload: "03600029145", CheckDigit: "2", FullValue: "036000291452"),
        new("all zeros",        Payload: "00000000000", CheckDigit: "0", FullValue: "000000000000"),
    };
}
