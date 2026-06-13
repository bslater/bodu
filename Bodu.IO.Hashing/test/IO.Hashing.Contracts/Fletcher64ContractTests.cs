// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher64ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fletcher64" /> with the documented "abc" and quick-brown-fox known-answer values plus
/// streaming-parity vectors. Bespoke Fletcher-specific tests (block-boundary handling, incremental
/// 31-byte sequence, full RevEng catalogue) remain in <c>FletcherTests.64.cs</c>.
/// </summary>
[TestClass]
public sealed class Fletcher64ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fletcher64>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Fletcher64 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "0000000000000000";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "0000018A000000C6", 64),
        new("quick brown fox",      s_quickBrownFox,                                "00015BA200000FD9", 64),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 12+12+12+7",
            s_quickBrownFox,
            [12, 12, 12, 7],
            "00015BA200000FD9"),
        new(
            "quick brown fox as byte-by-byte",
            s_quickBrownFox,
            Enumerable.Repeat(1, s_quickBrownFox.Length).ToArray(),
            "00015BA200000FD9"),
    ];
}
