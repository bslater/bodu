// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv1a32ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fnv1a32" />. Documents the FNV-1a offset basis as the empty-input digest
/// (<c>0x811C9DC5</c>), the in-tree 'ABC' value, and a streaming-parity vector over the
/// quick-brown-fox pangram.
/// </summary>
[TestClass]
public sealed class Fnv1a32ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fnv1a32>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Fnv1a32 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "811C9DC5";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "5C842F6B", 32),
        new("quick brown fox",      s_quickBrownFox,                                "048FFF90", 32),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "048FFF90"),
        new(
            "quick brown fox byte-by-byte",
            s_quickBrownFox,
            Enumerable.Repeat(1, s_quickBrownFox.Length).ToArray(),
            "048FFF90"),
    ];
}
