// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="AsconXof128" /> at multiple
/// output lengths. Validates that the extendable-output function reproduces the documented NIST
/// SP 800-232 reference digests for the canonical sequential-byte inputs and that the requested
/// output length is honoured exactly.
/// </summary>
[TestClass]
public sealed class AsconXof128ContractTests : CryptoHashContractTests<AsconXof128>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization) =>
        AsconXof128.HashData(input, outputLengthBytes);

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } = new CryptoHashKat[]
    {
        new(
            "Ascon-XOF128 empty → 32 bytes",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString(
                "D2AE52E6FD7D4925B8A85DD1E3BAC87A5338708D13CE92F851868ED5782EF084"),
            OutputLengthBytes: 32,
            Key: null),

        new(
            "Ascon-XOF128 empty → 64 bytes",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString(
                "D2AE52E6FD7D4925B8A85DD1E3BAC87A5338708D13CE92F851868ED5782EF084" +
                "045B596B30C1AA517E5BE0695A7E2DCE52ED774F493A09DB7890DDC06E61DC2F"),
            OutputLengthBytes: 64,
            Key: null),

        new(
            "Ascon-XOF128 1-byte 0x00 → 32 bytes",
            Input: [0x00],
            ExpectedDigest: Convert.FromHexString(
                "ECF9BA491725E622581E6431AC0BAF832589273CE1E22010B96427BD574F5AAF"),
            OutputLengthBytes: 32,
            Key: null),

        new(
            "Ascon-XOF128 8-byte sequential → 32 bytes",
            Input: [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07],
            ExpectedDigest: Convert.FromHexString(
                "DE779BAC8B73F590374884BF81AD7850A84678736CEB66D18B0D235998D0D972"),
            OutputLengthBytes: 32,
            Key: null),
    };
}
