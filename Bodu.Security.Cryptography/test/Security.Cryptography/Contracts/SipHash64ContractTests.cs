// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash64ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="SipHash64" /> with two
/// canonical Aumasson / Bernstein 2-4 test vectors using the standard
/// <c>00 01 02 ... 0F</c> key. The contract base verifies digest equality and that the requested output
/// length is honoured. Bespoke SipHash tests (key length validation, streaming, custom round counts)
/// live in the existing <c>SipHashTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class SipHash64ContractTests : CryptoHashContractTests<SipHash64>
{
    private static readonly byte[] ReferenceKey =
        [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];

    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using SipHash64 algorithm = new();
        if (key is not null)
            algorithm.Key = key;
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } =
    [
        // Aumasson / Bernstein SipHash 2-4 reference vector 0: empty input.
        new(
            "empty",
            Input: [],
            ExpectedDigest: [0x31, 0x0E, 0x0E, 0xDD, 0x47, 0xDB, 0x6F, 0x72],
            OutputLengthBytes: 8,
            Key: ReferenceKey),

        // Aumasson / Bernstein SipHash 2-4 reference vector 1: single-byte input 0x00.
        new(
            "single-byte 0x00",
            Input: [0x00],
            ExpectedDigest: [0xFD, 0x67, 0xDC, 0x93, 0xC5, 0x39, 0xF8, 0x74],
            OutputLengthBytes: 8,
            Key: ReferenceKey),
    ];
}
