// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish1024BlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="Threefish1024Cipher" />
/// using the Skein 1.3 / NIST SHA-3 submission reference vectors for the zero-key/zero-tweak baseline
/// and the in-tree incremental-byte (key 0x10..0x8F, tweak 0x00..0x0F, descending plaintext FF..80)
/// vector at the widest 1024-bit block size.
/// </summary>
[TestClass]
public sealed class Threefish1024BlockCipherContractTests
    : BlockCipherContractTests<Threefish1024Cipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using Threefish1024Cipher cipher = new(key, tweak!);
        byte[] output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using Threefish1024Cipher cipher = new(key, tweak!);
        byte[] output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Threefish-1024 zero key, zero tweak, zero plaintext",
            Algorithm: "Threefish-1024",
            Key: new byte[128],
            Plaintext: new byte[128],
            Ciphertext: Convert.FromHexString(
                "F05C3D0A3D05B304F785DDC7D1E036015C8AA76E2F217B06C6E1544C0BC1A90D" +
                "F0ACCB9473C24E0FD54FEA68057F43329CB454761D6DF5CF7B2E9B3614FBD5A2" +
                "0B2E4760B40603540D82EABC5482C171C832AFBE68406BC39500367A592943FA" +
                "9A5B4A43286CA3C4CF46104B443143D560A4B230488311DF4FEEF7E1DFE8391E"),
            BlockSizeBits: 1024,
            Tweak: new byte[16]),

        new(
            Name: "Threefish-1024 incremental key 0x10..0x8F, incremental tweak 0x00..0x0F, descending plaintext FF..80",
            Algorithm: "Threefish-1024",
            Key: IncrementalBytes(0x10, 128),
            Plaintext: Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
                "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" +
                "BFBEBDBCBBBAB9B8B7B6B5B4B3B2B1B0AFAEADACABAAA9A8A7A6A5A4A3A2A1A0" +
                "9F9E9D9C9B9A999897969594939291908F8E8D8C8B8A89888786858483828180"),
            Ciphertext: Convert.FromHexString(
                "A6654DDBD73CC3B05DD777105AA849BCE49372EAAFFC5568D254771BAB85531C" +
                "94F780E7FFAAE430D5D8AF8C70EEBBE1760F3B42B737A89CB363490D670314BD" +
                "8AA41EE63C2E1F45FBD477922F8360B388D6125EA6C7AF0AD7056D01796E90C8" +
                "3313F4150A5716B30ED5F569288AE974CE2B4347926FCE57DE44512177DD7CDE"),
            BlockSizeBits: 1024,
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
