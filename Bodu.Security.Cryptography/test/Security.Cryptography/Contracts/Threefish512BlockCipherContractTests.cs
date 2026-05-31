// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish512BlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="Threefish512Cipher" /> using
/// the Skein 1.3 / NIST SHA-3 submission reference vectors for the zero-key/zero-tweak baseline and the
/// in-tree incremental-byte (key 0x10..0x4F, tweak 0x00..0x0F, descending plaintext FF..C0) vector at
/// the wider 512-bit block size.
/// </summary>
[TestClass]
public sealed class Threefish512BlockCipherContractTests
    : BlockCipherContractTests<Threefish512Cipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using Threefish512Cipher cipher = new(key, tweak!);
        byte[] output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using Threefish512Cipher cipher = new(key, tweak!);
        byte[] output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Threefish-512 zero key, zero tweak, zero plaintext",
            Algorithm: "Threefish-512",
            Key: new byte[64],
            Plaintext: new byte[64],
            Ciphertext: Convert.FromHexString(
                "B1A2BBC6EF6025BC40EB3822161F36E375D1BB0AEE3186FBD19E47C5D479947B" +
                "7BC2F8586E35F0CFF7E7F03084B0B7B1F1AB3961A580A3E97EB41EA14A6D7BBE"),
            BlockSizeBits: 512,
            Tweak: new byte[16]),

        new(
            Name: "Threefish-512 incremental key 0x10..0x4F, incremental tweak 0x00..0x0F, descending plaintext FF..C0",
            Algorithm: "Threefish-512",
            Key: IncrementalBytes(0x10, 64),
            Plaintext: Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
                "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0"),
            Ciphertext: Convert.FromHexString(
                "E304439626D45A2CB401CAD8D636249A6338330EB06D45DD8B36B90E97254779" +
                "272A0A8D99463504784420EA18C9A725AF11DFFEA10162348927673D5C1CAF3D"),
            BlockSizeBits: 512,
            Tweak: IncrementalBytes(0x00, 16)),
    ];

    private static byte[] IncrementalBytes(byte start, int length)
    {
        byte[] result = new byte[length];
        for (int i = 0; i < length; i++)
            result[i] = (byte)(start + i);
        return result;
    }
}
