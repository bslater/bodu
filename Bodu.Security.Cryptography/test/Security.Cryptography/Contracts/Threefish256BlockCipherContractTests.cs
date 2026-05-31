// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256BlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="Threefish256Cipher" /> using
/// the Skein 1.3 / NIST SHA-3 submission reference vectors for the zero-key/zero-tweak baseline and the
/// in-tree incremental-byte (key 0x10..0x2F, tweak 0x00..0x0F, descending plaintext FF..E0) vector.
/// The contract base verifies encrypt and decrypt directions independently for both rows. Bespoke
/// Threefish coverage (variants, mode/padding, AVX-512 paths) remains in
/// <c>ThreefishBlockCipherTests.*</c> and <c>Threefish256CipherTests.*</c>.
/// </summary>
[TestClass]
public sealed class Threefish256BlockCipherContractTests
    : BlockCipherContractTests<Threefish256Cipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using Threefish256Cipher cipher = new(key, tweak!);
        var output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using Threefish256Cipher cipher = new(key, tweak!);
        var output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Threefish-256 zero key, zero tweak, zero plaintext",
            Algorithm: "Threefish-256",
            Key: new byte[32],
            Plaintext: new byte[32],
            Ciphertext: Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8"),
            BlockSizeBits: 256,
            Tweak: new byte[16]),

        new(
            Name: "Threefish-256 incremental key 0x10..0x2F, incremental tweak 0x00..0x0F, descending plaintext FF..E0",
            Algorithm: "Threefish-256",
            Key: IncrementalBytes(0x10, 32),
            Plaintext: Convert.FromHexString("FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0"),
            Ciphertext: Convert.FromHexString("E0D091FF0EEA8FDFC98192E62ED80AD59D865D08588DF476657056B5955E97DF"),
            BlockSizeBits: 256,
            Tweak: IncrementalBytes(0x00, 16)),
    ];

    private static byte[] IncrementalBytes(byte start, int length)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
            result[i] = (byte)(start + i);
        return result;
    }
}
