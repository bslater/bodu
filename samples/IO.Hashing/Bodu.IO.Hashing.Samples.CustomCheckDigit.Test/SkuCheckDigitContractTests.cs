// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkuCheckDigitContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.Samples.CustomCheckDigit;

/// <summary>
/// Drives the library's shared <see cref="CheckDigitContractTests{TAlgorithm}" /> against the
/// sample's <see cref="SkuCheckDigit" />. This is the pattern for any custom
/// <c>CheckDigitAlgorithm</c>: implement the two adapter members, supply known-answer vectors,
/// and inherit the compute/validate/corruption contract the built-in schemes are held to.
/// </summary>
[TestClass]
public sealed class SkuCheckDigitContractTests
    : CheckDigitContractTests<SkuCheckDigit>
{
    /// <inheritdoc />
    protected override char Compute(string payload) =>
        SkuCheckDigit.Compute(payload);

    /// <inheritdoc />
    protected override bool IsValid(string fullValue) =>
        SkuCheckDigit.IsValid(fullValue);

    /// <inheritdoc />
    protected override IReadOnlyList<CheckDigitKat> KnownAnswers { get; } =
    [
        // Row shape: display name, payload, expected check digit, full value (payload + check digit)
        // that IsValid must accept. The base class derives the corruption cases from these rows.
        new("nine-digit payload", "123456789", "3", "1234567893"),
        new("leading zeros preserved", "000451", "6", "0004516"),
        new("repeated high digits", "998877", "8", "9988778"),
        new("two-digit payload", "42", "6", "426"),
        new("pi digits", "31415926", "9", "314159269"),
        new("single digit", "7", "1", "71"),
    ];
}
