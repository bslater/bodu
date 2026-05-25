// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv1a64ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fnv1a64" />. Documents the FNV-1a-64 offset basis as the empty-input digest
/// (<c>0xCBF29CE484222325</c>), the in-tree 'ABC' value, and a streaming-parity vector over the
/// quick-brown-fox pangram.
/// </summary>
[TestClass]
public sealed class Fnv1a64ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fnv1a64>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Fnv1a64 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "CBF29CE484222325";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } = new HashKat[]
    {
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "FA2FE219A07442EB", 64),
        new("quick brown fox",      s_quickBrownFox,                                "F3F9B7F5E7E47110", 64),
    };

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } = new HashStreamingKat[]
    {
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "F3F9B7F5E7E47110"),
    };
}
