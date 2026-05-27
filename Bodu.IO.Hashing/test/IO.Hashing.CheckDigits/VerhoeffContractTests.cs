// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VerhoeffContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Verhoeff" /> with the
/// Wikipedia reference vector and a small ascending-digit run. Bespoke streaming/append/error-
/// detection coverage stays in the existing <c>VerhoeffTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class VerhoeffContractTests : CheckDigitContractTests<Verhoeff>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Verhoeff.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Verhoeff.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("single zero",       Payload: "0",     CheckDigit: "4", FullValue: "04"),
        new("Wikipedia example", Payload: "236",   CheckDigit: "3", FullValue: "2363"),
        new("ascending 1..5",    Payload: "12345", CheckDigit: "1", FullValue: "123451"),
    ];
}
