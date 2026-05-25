// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="Skein256" /> at the
/// canonical 256-bit output. Uses the Skein 1.3 / NIST CD <c>skein_golden_kat.txt</c> empty-input
/// vector. Multi-size variants (160/224), Skein-MAC mode, and incremental message coverage stay in
/// the existing <c>Skein256Tests.*</c> partials.
/// </summary>
[TestClass]
public sealed class Skein256ContractTests : CryptoHashContractTests<Skein256>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using Skein256 algorithm = new(outputLengthBytes * 8);
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } = new CryptoHashKat[]
    {
        new(
            "Skein-256-256 empty",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString("C8877087DA56E072870DAA843F176E9453115929094C3A40C463A196C29BF7BA"),
            OutputLengthBytes: 32,
            Key: null),
    };
}
