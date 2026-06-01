// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="Blake2s" /> at multiple
/// canonical output sizes (128, 160, 224, 256 bits). Documents the RFC 7693 / reference-implementation
/// empty-input digests. Keyed-MAC mode and per-byte streaming stays covered by <c>Blake2sTests.*</c>.
/// </summary>
[TestClass]
public sealed class Blake2sContractTests : CryptoHashContractTests<Blake2s>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using Blake2s algorithm = new(outputLengthBytes * 8);
        if (key is not null)
            algorithm.Key = key;
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } =
    [
        new(
            "BLAKE2s-256 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString("69217A3079908094E11121D042354A7C1F55B6482CA1A51E1B250DFD1ED0EEF9"),
            OutputLengthBytes: 32,
            Key: null),

        new(
            "BLAKE2s-224 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString("1FA1291E65248B37B3433475B2A0DD63D54A11ECC4E3E034E7BC1EF4"),
            OutputLengthBytes: 28,
            Key: null),

        new(
            "BLAKE2s-160 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString("354C9C33F735962418BDACB9479873429C34916F"),
            OutputLengthBytes: 20,
            Key: null),

        new(
            "BLAKE2s-128 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString("64550D6FFE2C0A01A14ABA1EADE0200C"),
            OutputLengthBytes: 16,
            Key: null),
    ];
}
