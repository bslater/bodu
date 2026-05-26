// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="Skein512" /> at the
/// canonical 512-bit output. Uses the Skein 1.3 / NIST CD <c>skein_golden_kat.txt</c> empty-input
/// vector.
/// </summary>
[TestClass]
public sealed class Skein512ContractTests : CryptoHashContractTests<Skein512>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using Skein512 algorithm = new(outputLengthBytes * 8);
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } =
    [
        new(
            "Skein-512-512 empty",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString(
                "BC5B4C50925519C290CC634277AE3D6257212395CBA733BBAD37A4AF0FA06AF4" +
                "1FCA7903D06564FEA7A2D3730DBDB80C1F85562DFCC070334EA4D1D9E72CBA7A"),
            OutputLengthBytes: 64,
            Key: null),
    ];
}
