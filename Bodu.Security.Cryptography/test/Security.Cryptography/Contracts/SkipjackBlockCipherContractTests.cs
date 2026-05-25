// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="SkipjackBlockCipher" /> with
/// the canonical FIPS PUB 185 Section 8 worked example plus the NSA Skipjack reference single-bit-key
/// avalanche vectors. The contract base verifies encrypt and decrypt directions independently; bespoke
/// Skipjack tests (round-key inspection, padding modes, transform lifecycle) live in the existing
/// <c>SkipjackBlockCipherTests.*</c> and <c>SkipjackAlgorithmTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class SkipjackBlockCipherContractTests
    : BlockCipherContractTests<SkipjackBlockCipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using SkipjackBlockCipher cipher = new(key);
        byte[] output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using SkipjackBlockCipher cipher = new(key);
        byte[] output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } = new BlockCipherKat[]
    {
        new(
            Name: "FIPS 185 §8 canonical vector",
            Algorithm: "Skipjack",
            Key: Convert.FromHexString("00998877665544332211"),
            Plaintext: Convert.FromHexString("33221100DDCCBBAA"),
            Ciphertext: Convert.FromHexString("2587CAE27A12D300"),
            BlockSizeBits: 64),

        new(
            Name: "NSA reference: all-zero plaintext, key bit 0 set",
            Algorithm: "Skipjack",
            Key: Convert.FromHexString("80000000000000000000"),
            Plaintext: Convert.FromHexString("0000000000000000"),
            Ciphertext: Convert.FromHexString("E378FE4157A66452"),
            BlockSizeBits: 64),

        new(
            Name: "NSA reference: all-zero plaintext, key bit 1 set",
            Algorithm: "Skipjack",
            Key: Convert.FromHexString("40000000000000000000"),
            Plaintext: Convert.FromHexString("0000000000000000"),
            Ciphertext: Convert.FromHexString("61CE4785762E8980"),
            BlockSizeBits: 64),

        new(
            Name: "NSA reference: all-zero plaintext, key byte 1 high bit set",
            Algorithm: "Skipjack",
            Key: Convert.FromHexString("00800000000000000000"),
            Plaintext: Convert.FromHexString("0000000000000000"),
            Ciphertext: Convert.FromHexString("F76307829359FC11"),
            BlockSizeBits: 64),
    };
}
