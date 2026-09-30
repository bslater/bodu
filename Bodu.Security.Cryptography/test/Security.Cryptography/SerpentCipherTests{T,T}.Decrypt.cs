// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests{T,T}.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SerpentCipherTests<TTest, TCipher>
{
    /// <summary>
    /// Verifies that decrypting seeded blocks under seeded keys and tweaks gives the plaintext of the replaced 1.1.0
    /// rounds, which <see cref="SerpentWideReference" /> keeps.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenKeysTweaksAndBlocksAreSeeded_ShouldMatchTheReplacedRounds()
    {
        foreach ((string name, byte[] key, byte[] tweak, byte[][] blocks) in SeededCases(0x5E7E_0D01))
        {
            using TCipher cipher = CreateCipher(key, tweak);
            var reference = new SerpentWideReference(key, tweak);
            byte[] actual = new byte[blocks[0].Length];

            for (int b = 0; b < blocks.Length; b++)
            {
                cipher.Decrypt(blocks[b], actual);
                CollectionAssert.AreEqual(reference.Decrypt(blocks[b]), actual, $"{name}, block {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that decrypting a block in place, into the memory that holds it, gives the plaintext of the replaced
    /// 1.1.0 rounds.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenOutputIsTheInput_ShouldMatchTheReplacedRounds()
    {
        foreach ((string name, byte[] key, byte[] tweak, byte[][] blocks) in SeededCases(0x5E7E_0D02))
        {
            using TCipher cipher = CreateCipher(key, tweak);
            var reference = new SerpentWideReference(key, tweak);

            for (int b = 0; b < blocks.Length; b++)
            {
                byte[] buffer = (byte[])blocks[b].Clone();
                cipher.Decrypt(buffer, buffer);
                CollectionAssert.AreEqual(reference.Decrypt(blocks[b]), buffer, $"{name}, block {b}");
            }
        }
    }
}
