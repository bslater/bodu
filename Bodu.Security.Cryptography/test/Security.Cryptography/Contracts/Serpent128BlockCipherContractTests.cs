// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128BlockCipherContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="BlockCipherContractTests{TCipher}" /> against <see cref="Serpent128Cipher" /> with
/// the NESSIE Serpent-128 single-bit avalanche reference vectors (zero-key/zero-plaintext baseline and
/// high-bit key with zero plaintext). Wider Serpent variants (256/512/1024 key sizes), the larger
/// NESSIE corpus, and bespoke mode/padding coverage stay in the existing
/// <c>SerpentBlockCipherTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class Serpent128BlockCipherContractTests
    : BlockCipherContractTests<Serpent128Cipher>
{
    /// <inheritdoc />
    protected override byte[] EncryptBlock(byte[] key, byte[] plaintext, byte[]? tweak)
    {
        using Serpent128Cipher cipher = new(key);
        byte[] output = new byte[plaintext.Length];
        cipher.Encrypt(plaintext, output);
        return output;
    }

    /// <inheritdoc />
    protected override byte[] DecryptBlock(byte[] key, byte[] ciphertext, byte[]? tweak)
    {
        using Serpent128Cipher cipher = new(key);
        byte[] output = new byte[ciphertext.Length];
        cipher.Decrypt(ciphertext, output);
        return output;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKat> KnownAnswers { get; } =
    [
        new(
            Name: "Serpent-128 zero-key zero-plaintext",
            Algorithm: "Serpent-128",
            Key: new byte[16],
            Plaintext: new byte[16],
            Ciphertext: Convert.FromHexString("3620B17AE6A993D09618B8768266BAE9"),
            BlockSizeBits: 128),

        new(
            Name: "Serpent-128 high-bit key, zero plaintext (NESSIE test 1)",
            Algorithm: "Serpent-128",
            Key: Convert.FromHexString("80000000000000000000000000000000"),
            Plaintext: new byte[16],
            Ciphertext: Convert.FromHexString("264E5481EFF42A4606ABDA06C0BFDA3D"),
            BlockSizeBits: 128),
    ];
}
