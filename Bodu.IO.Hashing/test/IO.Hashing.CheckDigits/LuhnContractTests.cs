// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LuhnContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.CheckDigits.Contracts;

/// <summary>
/// Drives <see cref="CheckDigitContractTests{TAlgorithm}" /> against <see cref="Luhn" /> with the
/// Wikipedia reference vector, the Visa test-card body, and small single-digit boundary cases. Bespoke
/// streaming/append/error-detection coverage stays in the existing <c>LuhnTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class LuhnContractTests : CheckDigitContractTests<Luhn>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        Luhn.Compute(payload.AsSpan());

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        Luhn.IsValid(fullValue.AsSpan());

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        new("single zero",            Payload: "0",                CheckDigit: "0", FullValue: "00"),
        new("single one",             Payload: "1",                CheckDigit: "8", FullValue: "18"),
        new("Wikipedia example",      Payload: "7992739871",       CheckDigit: "3", FullValue: "79927398713"),
        new("Visa test card body",    Payload: "411111111111111",  CheckDigit: "1", FullValue: "4111111111111111"),
    ];
}
