// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;
using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Adler32" />. Documents the canonical Adler-32 empty input (<c>0x00000001</c>), the
/// in-tree 'ABC' value, and a streaming-parity vector over the quick-brown-fox pangram.
/// </summary>
[TestClass]
public sealed class Adler32ContractTests
    : NonCryptographicHashAlgorithmContractTests<Adler32>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Adler32 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "00000001";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "018D00C7", 32),
        new("quick brown fox",      s_quickBrownFox,                                "5BDC0FDA", 32),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "5BDC0FDA"),
    ];
}
