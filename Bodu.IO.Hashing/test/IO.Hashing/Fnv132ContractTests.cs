// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv132ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fnv132" /> (FNV-1, not FNV-1a). Documents the offset basis 0x811C9DC5 empty-input
/// digest, the in-tree 'ABC' value, and the quick-brown-fox vector.
/// </summary>
[TestClass]
public sealed class Fnv132ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fnv132>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Fnv132 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "811C9DC5";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "634CAFEB", 32),
        new("quick brown fox",      s_quickBrownFox,                                "E9C86C6E", 32),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "E9C86C6E"),
    ];
}
