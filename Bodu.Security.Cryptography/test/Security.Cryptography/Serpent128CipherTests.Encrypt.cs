// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128CipherTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class Serpent128CipherTests
{
    /// <summary>
    /// Verifies that the cipher - key schedule and rounds, both now over the S-box circuits - encrypts as the
    /// table-driven reference does, for seeded keys of every Serpent key size and seeded blocks.
    /// </summary>
    /// <param name="keyBytes">The key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void Encrypt_WhenKeysAndBlocksAreSeededRandom_ShouldMatchTheTableDrivenReference(int keyBytes)
    {
        var random = new Random(0x5E4F_3001 + keyBytes);

        for (int i = 0; i < 32; i++)
        {
            byte[] key = new byte[keyBytes];
            byte[] block = new byte[16];
            random.NextBytes(key);
            random.NextBytes(block);
            byte[] actual = new byte[16];
            using var cipher = new Serpent128Cipher(key);

            cipher.Encrypt(block, actual);

            CollectionAssert.AreEqual(SerpentReference.Encrypt(SerpentReference.ExpandKey(key), block), actual, $"case {i}");
        }
    }
}
