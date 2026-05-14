// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LeiTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Lei" /> check-code algorithm.
/// </summary>
[TestClass]
public sealed class LeiTests
    : MultiCharCheckDigitAlgorithmTests<LeiTests, Lei>
{
    /// <inheritdoc />
    protected override MultiCharCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "LEI",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        CheckLength = 2,
        EmptyCheckDigits = "01",
        KnownAnswers =
        [
            new() { Name = "GleifExample",   Body = "54930084UKLVMY22DS", ExpectedCheck = "16" },
            new() { Name = "NumericBody",    Body = "123456789012345678", ExpectedCheck = "88" },
        ],
    };

    /// <inheritdoc />
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Lei.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> value) =>
        Lei.IsValid(value);
}
