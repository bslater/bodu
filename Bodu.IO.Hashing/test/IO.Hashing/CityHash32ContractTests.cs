// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash32ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="CityHash32" /> with documented empty / 'ABC' / quick-brown-fox values.
/// </summary>
[TestClass]
public sealed class CityHash32ContractTests
    : NonCryptographicHashAlgorithmContractTests<CityHash32>
{
    private static readonly byte[] s_quickBrownFox =
        System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <inheritdoc />
    protected override CityHash32 Create() => new();

    /// <inheritdoc />
    protected override string? EmptyInputExpectedHex => "7AD156DC";

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } =
    [
        new("ABC",                  System.Text.Encoding.ASCII.GetBytes("ABC"),     "1325B816", 32),
        new("quick brown fox",      s_quickBrownFox,                                "10C839A3", 32),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } =
    [
        new(
            "quick brown fox as 10+10+10+13",
            s_quickBrownFox,
            [10, 10, 10, 13],
            "10C839A3"),
    ];
}
