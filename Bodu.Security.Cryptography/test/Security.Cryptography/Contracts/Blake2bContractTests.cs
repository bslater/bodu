// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="Blake2b" /> at its canonical
/// 512-bit output. Verifies the documented RFC 7693 / reference-implementation empty-input digest plus
/// the BLAKE2 reference 'abc' vector. Multi-size variants (128/160/192/224/256/384) and keyed-MAC mode
/// stay covered by <c>Blake2bTests.*</c>.
/// </summary>
[TestClass]
public sealed class Blake2bContractTests : CryptoHashContractTests<Blake2b>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using Blake2b algorithm = new(outputLengthBytes * 8);
        if (key is not null)
            algorithm.Key = key;
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } =
    [
        new(
            "BLAKE2b-512 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString(
                "786A02F742015903C6C6FD852552D272912F4740E15847618A86E217F71F5419" +
                "D25E1031AFEE585313896444934EB04B903A685B1448B755D56F701AFE9BE2CE"),
            OutputLengthBytes: 64,
            Key: null),

        new(
            "BLAKE2b-256 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString("0E5751C026E543B2E8AB2EB06099DAA1D1E5DF47778F7787FAAB45CDF12FE3A8"),
            OutputLengthBytes: 32,
            Key: null),

        new(
            "BLAKE2b-160 empty",
            Input: [],
            ExpectedDigest: Convert.FromHexString("3345524ABF6BBE1809449224B5972C41790B6CF2"),
            OutputLengthBytes: 20,
            Key: null),
    ];
}
