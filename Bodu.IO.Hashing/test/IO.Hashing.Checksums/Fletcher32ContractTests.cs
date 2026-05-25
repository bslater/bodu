// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher32ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;
using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fletcher32" /> with the documented "abc" and quick-brown-fox known-answer values plus
/// streaming-parity vectors. Bespoke Fletcher-specific tests (block-boundary handling, incremental
/// 15-byte sequence, full RevEng catalogue) remain in <c>FletcherTests.32.cs</c>.
/// </summary>
[TestClass]
public sealed class Fletcher32ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fletcher32>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Fletcher32 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "00000000";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } = new HashKat[]
    {
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "018A00C6", 32),
        new("quick brown fox",      s_quickBrownFox,                                "5BA30FD9", 32),
    };

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } = new HashStreamingKat[]
    {
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "5BA30FD9"),
        new(
            "quick brown fox as byte-by-byte",
            s_quickBrownFox,
            Enumerable.Repeat(1, s_quickBrownFox.Length).ToArray(),
            "5BA30FD9"),
    };
}
