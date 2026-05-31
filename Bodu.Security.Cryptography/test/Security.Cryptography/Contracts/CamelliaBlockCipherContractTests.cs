// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="CamelliaBlockCipher" />
/// using the RFC 3713 canonical 128-bit-key test vector and the all-zero / single-bit avalanche pairs
/// from the Camellia reference distribution. Bespoke Camellia coverage (192/256-bit keys, mode/padding,
/// RFC 5528 test-vector set) lives in the existing <c>CamelliaBlockCipherTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class CamelliaBlockCipherContractTests
    : BlockCipherContractTests<CamelliaBlockCipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using CamelliaBlockCipher cipher = new(key);
        var output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using CamelliaBlockCipher cipher = new(key);
        var output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Camellia-128 RFC 3713 reference",
            Algorithm: "Camellia-128",
            Key: Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210"),
            Plaintext: Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210"),
            Ciphertext: Convert.FromHexString("67673138549669730857065648EABE43"),
            BlockSizeBits: 128),

        new(
            Name: "Camellia-128 zero-key, high-bit plaintext (avalanche)",
            Algorithm: "Camellia-128",
            Key: new byte[16],
            Plaintext: Convert.FromHexString("80000000000000000000000000000000"),
            Ciphertext: Convert.FromHexString("07923A39EB0A817D1C4D87BDB82D1F1C"),
            BlockSizeBits: 128),

        new(
            Name: "Camellia-128 high-bit key, zero plaintext (avalanche)",
            Algorithm: "Camellia-128",
            Key: Convert.FromHexString("80000000000000000000000000000000"),
            Plaintext: new byte[16],
            Ciphertext: Convert.FromHexString("6C227F749319A3AA7DA235A9BBA05A2C"),
            BlockSizeBits: 128),
    ];
}
