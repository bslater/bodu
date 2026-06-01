// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv164ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fnv164" /> (FNV-1, not FNV-1a) at 64-bit width. Documents the offset basis
/// 0xCBF29CE484222325 empty-input digest plus the 'ABC' and quick-brown-fox vectors.
/// </summary>
[TestClass]
public sealed class Fnv164ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fnv164>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Fnv164 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "CBF29CE484222325";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "D86FEA186B53126B", 64),
        new("quick brown fox",      s_quickBrownFox,                                "A8B2F3117DE37ACE", 64),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "A8B2F3117DE37ACE"),
    ];
}
