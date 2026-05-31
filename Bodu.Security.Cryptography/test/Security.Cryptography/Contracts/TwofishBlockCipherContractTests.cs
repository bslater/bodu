// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="TwofishBlockCipher" /> with
/// a representative subset of the Twofish AES submission ECB intermediate-values vectors at each
/// supported key length (128, 192, 256 bits). The full I=1..5,10 iteration corpus per key size remains
/// in the existing <c>TwofishBlockCipherTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class TwofishBlockCipherContractTests
    : BlockCipherContractTests<TwofishBlockCipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using TwofishBlockCipher cipher = new(key);
        byte[] output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using TwofishBlockCipher cipher = new(key);
        byte[] output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Twofish-128 I=1: zero key, zero plaintext",
            Algorithm: "Twofish-128",
            Key: new byte[16],
            Plaintext: new byte[16],
            Ciphertext: Convert.FromHexString("9F589F5CF6122C32B6BFEC2F2AE8C35A"),
            BlockSizeBits: 128),

        new(
            Name: "Twofish-128 I=10",
            Algorithm: "Twofish-128",
            Key: Convert.FromHexString("34C8A5FB2D3D08A170D120AC6D26DBFA"),
            Plaintext: Convert.FromHexString("28530B358C1B42EF277DE6D4407FC591"),
            Ciphertext: Convert.FromHexString("8A8AB983310ED78C8C0ECDE030B8DCA4"),
            BlockSizeBits: 128),

        new(
            Name: "Twofish-192 I=1: zero key, zero plaintext",
            Algorithm: "Twofish-192",
            Key: new byte[24],
            Plaintext: new byte[16],
            Ciphertext: Convert.FromHexString("EFA71F788965BD4453F860178FC19101"),
            BlockSizeBits: 128),

        new(
            Name: "Twofish-192 I=10",
            Algorithm: "Twofish-192",
            Key: Convert.FromHexString("AE8109BFDA85C1F2C5038B34ED691BFF3AF6F7CE5BD35EF1"),
            Plaintext: Convert.FromHexString("893FD67B98C550073571BD631263FC78"),
            Ciphertext: Convert.FromHexString("16434FC9C8841A63D58700B5578E8F67"),
            BlockSizeBits: 128),

        new(
            Name: "Twofish-256 I=1: zero key, zero plaintext",
            Algorithm: "Twofish-256",
            Key: new byte[32],
            Plaintext: new byte[16],
            Ciphertext: Convert.FromHexString("57FF739D4DC92C1BD7FC01700CC8216F"),
            BlockSizeBits: 128),

        new(
            Name: "Twofish-256 I=10",
            Algorithm: "Twofish-256",
            Key: Convert.FromHexString("DC096BCD99FC72F79936D4C748E75AF75AB67A5F8539A4A5FD9F0373BA463466"),
            Plaintext: Convert.FromHexString("C5A3E7CEE0F1B7260528A68FB4EA05F2"),
            Ciphertext: Convert.FromHexString("43D5CEC327B24AB90AD34A79D0469151"),
            BlockSizeBits: 128),
    ];
}
