// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Bernstein" /> (the classic <c>djb2</c> hash). Documents the empty-input digest
/// <c>0x00001505</c> (initial value 5381) plus the in-tree 'ABC' and quick-brown-fox values.
/// </summary>
[TestClass]
public sealed class BernsteinContractTests
    : NonCryptographicHashAlgorithmContractTests<Bernstein>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override Bernstein Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "00001505";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "0B87D02B", 32),
        new("quick brown fox",      s_quickBrownFox,                                "34CC38DE", 32),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "34CC38DE"),
    ];
}
