// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="BlowfishBlockCipher" />
/// using Eric Young's published 16-round Blowfish ECB vectors (transcribed from <c>vectors-2.txt</c>
/// hosted on Schneier's site). Bespoke Blowfish coverage (key-schedule edge cases, padding modes,
/// stream-mode transforms) remains in the existing <c>BlowfishBlockCipherTests.*</c> and
/// <c>BlowfishTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class BlowfishBlockCipherContractTests
    : BlockCipherContractTests<BlowfishBlockCipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using BlowfishBlockCipher cipher = new(key);
        byte[] output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using BlowfishBlockCipher cipher = new(key);
        byte[] output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Key=0000000000000000, PT=0000000000000000",
            Algorithm: "Blowfish",
            Key: Convert.FromHexString("0000000000000000"),
            Plaintext: Convert.FromHexString("0000000000000000"),
            Ciphertext: Convert.FromHexString("4EF997456198DD78"),
            BlockSizeBits: 64),

        new(
            Name: "Key=FFFFFFFFFFFFFFFF, PT=FFFFFFFFFFFFFFFF",
            Algorithm: "Blowfish",
            Key: Convert.FromHexString("FFFFFFFFFFFFFFFF"),
            Plaintext: Convert.FromHexString("FFFFFFFFFFFFFFFF"),
            Ciphertext: Convert.FromHexString("51866FD5B85ECB8A"),
            BlockSizeBits: 64),

        new(
            Name: "Key=0123456789ABCDEF, PT=1111111111111111",
            Algorithm: "Blowfish",
            Key: Convert.FromHexString("0123456789ABCDEF"),
            Plaintext: Convert.FromHexString("1111111111111111"),
            Ciphertext: Convert.FromHexString("61F9C3802281B096"),
            BlockSizeBits: 64),

        new(
            Name: "Key=FEDCBA9876543210, PT=0123456789ABCDEF",
            Algorithm: "Blowfish",
            Key: Convert.FromHexString("FEDCBA9876543210"),
            Plaintext: Convert.FromHexString("0123456789ABCDEF"),
            Ciphertext: Convert.FromHexString("0ACEAB0FC6A0A28D"),
            BlockSizeBits: 64),

        new(
            Name: "Key=7CA110454A1A6E57, PT=01A1D6D039776742",
            Algorithm: "Blowfish",
            Key: Convert.FromHexString("7CA110454A1A6E57"),
            Plaintext: Convert.FromHexString("01A1D6D039776742"),
            Ciphertext: Convert.FromHexString("59C68245EB05282B"),
            BlockSizeBits: 64),
    ];
}
