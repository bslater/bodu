// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Pearson" /> in its canonical 8-bit form using the original Pearson permutation table.
/// Documents the empty-input zero baseline, the in-tree 'ABC' value, and the quick-brown-fox vector.
/// Multi-byte hash sizes (16/32/64-bit) and alternate permutation tables stay covered by
/// <c>PearsonTests.*</c>.
/// </summary>
[TestClass]
public sealed class PearsonContractTests
    : NonCryptographicHashAlgorithmContractTests<Pearson>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Pearson Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "00";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } = new HashKat[]
    {
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "51", 8),
        new("quick brown fox",      s_quickBrownFox,                                "A5", 8),
    };

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } = new HashStreamingKat[]
    {
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "A5"),
    };
}
