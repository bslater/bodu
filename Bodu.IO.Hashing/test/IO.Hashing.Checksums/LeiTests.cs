// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LeiTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

    /// <summary>
    /// Verifies that <see cref="Lei.IsValid(ReadOnlySpan{char})" /> rejects the empty span. A full-sequence
    /// validator requires at least the check characters, so an empty input cannot satisfy the algorithm's invariant.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse() => Assert.IsFalse(Lei.IsValid([]));

    /// <inheritdoc />
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Lei.Compute(body);
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
    protected override bool IsValidStatic(ReadOnlySpan<char> value) =>
        Lei.IsValid(value);

}
