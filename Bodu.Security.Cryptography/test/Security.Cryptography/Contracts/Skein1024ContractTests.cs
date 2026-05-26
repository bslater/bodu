// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein1024ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="Skein1024" /> at the
/// canonical 1024-bit output. Uses the Skein 1.3 / NIST CD <c>skein_golden_kat.txt</c> empty-input
/// vector.
/// </summary>
[TestClass]
public sealed class Skein1024ContractTests : CryptoHashContractTests<Skein1024>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using Skein1024 algorithm = new(outputLengthBytes * 8);
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } = new CryptoHashKat[]
    {
        new(
            "Skein-1024-1024 empty",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString(
                "0FFF9563BB3279289227AC77D319B6FFF8D7E9F09DA1247B72A0A265CD6D2A62" +
                "645AD547ED8193DB48CFF847C06494A03F55666D3B47EB4C20456C9373C86297" +
                "D630D5578EBD34CB40991578F9F52B18003EFA35D3DA6553FF35DB91B81AB890" +
                "BEC1B189B7F52CB2A783EBB7D823D725B0B4A71F6824E88F68F982EEFC6D19C6"),
            OutputLengthBytes: 128,
            Key: null),
    };
}
