// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash128ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="SipHash128" /> using a small
/// set of canonical Aumasson / Bernstein SipHash 2-4 vectors with the standard <c>00 01 02 ... 0F</c>
/// key. The contract base verifies digest equality and that the requested 16-byte output length is
/// honoured; bespoke SipHash tests (key length validation, streaming, custom round counts, all 64
/// reference rows) live in the existing <c>SipHashTests.128.*</c> partials.
/// </summary>
[TestClass]
public sealed class SipHash128ContractTests : CryptoHashContractTests<SipHash128>
{
    private static readonly byte[] ReferenceKey =
        [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];

    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using SipHash128 algorithm = new();
        if (key is not null)
            algorithm.Key = key;
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } =
    [
        // Aumasson / Bernstein SipHash 2-4 128-bit reference vector 0: empty input.
        new(
            "empty",
            Input: Array.Empty<byte>(),
            ExpectedDigest:
            [
                0xA3, 0x81, 0x7F, 0x04, 0xBA, 0x25, 0xA8, 0xE6,
                0x6D, 0xF6, 0x72, 0x14, 0xC7, 0x55, 0x02, 0x93,
            ],
            OutputLengthBytes: 16,
            Key: ReferenceKey),

        // Aumasson / Bernstein SipHash 2-4 128-bit reference vector 1: single-byte input 0x00.
        new(
            "single-byte 0x00",
            Input: [0x00],
            ExpectedDigest:
            [
                0xDA, 0x87, 0xC1, 0xD8, 0x6B, 0x99, 0xAF, 0x44,
                0x34, 0x76, 0x59, 0x11, 0x9B, 0x22, 0xFC, 0x45,
            ],
            OutputLengthBytes: 16,
            Key: ReferenceKey),
    ];
}
