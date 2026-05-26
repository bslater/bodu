// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher16ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;
using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Drives <see cref="NonCryptographicHashAlgorithmContractTests{TAlgorithm}" /> against
/// <see cref="Fletcher16" /> with a representative set of one-shot and streaming-parity vectors. Bespoke
/// Fletcher-specific tests (block-boundary handling, oversized inputs, the full RevEng catalogue) live in
/// the existing <c>Bodu.IO.Hashing/test</c> partials.
/// </summary>
[TestClass]
public sealed class Fletcher16ContractTests
    : NonCryptographicHashAlgorithmContractTests<Fletcher16>
{
    /// <inheritdoc />
    protected override Fletcher16 Create() => new();

    /// <inheritdoc />
    protected override IReadOnlyList<HashKat> KnownAnswers { get; } = new HashKat[]
    {
        new("abcde",        System.Text.Encoding.ASCII.GetBytes("abcde"),      "C8F0", 16),
        new("abcdef",       System.Text.Encoding.ASCII.GetBytes("abcdef"),     "2057", 16),
        new("abcdefgh",     System.Text.Encoding.ASCII.GetBytes("abcdefgh"),   "0627", 16),
    };

    /// <inheritdoc />
    protected override IReadOnlyList<HashStreamingKat> StreamingCases { get; } = new HashStreamingKat[]
    {
        new(
            "abcdefgh as 1+3+4",
            System.Text.Encoding.ASCII.GetBytes("abcdefgh"),
            [1, 3, 4],
            "0627"),
        new(
            "abcdefgh as 8 single bytes",
            System.Text.Encoding.ASCII.GetBytes("abcdefgh"),
            [1, 1, 1, 1, 1, 1, 1, 1],
            "0627"),
    };
}
