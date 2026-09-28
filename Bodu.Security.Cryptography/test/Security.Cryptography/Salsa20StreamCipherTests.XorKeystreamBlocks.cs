// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20StreamCipherTests.XorKeystreamBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Salsa20StreamCipherTests
{
    /// <summary>
    /// Verifies that combining any number of whole blocks at once, from none to a hundred, matches drawing the same
    /// blocks one at a time, and leaves the engine where those draws leave it, under both key sizes.
    /// </summary>
    /// <param name="keyBytes">The key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    public void XorKeystreamBlocks_WhenBlockCountVaries_ShouldMatchSuccessiveKeystreamBlocks(int keyBytes)
    {
        var random = new Random(0x5A15_1001 + keyBytes);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([63, 64, 65, 100]))
        {
            byte[] key = NextBytes(random, keyBytes);
            byte[] nonce = NextBytes(random, Salsa20StreamCipher.NonceSizeBytes);
            ulong counter = (ulong)random.NextInt64();
            byte[] input = NextBytes(random, blocks * Salsa20StreamCipher.BlockSizeBytes);
            byte[] actual = new byte[input.Length];
            using var bulk = new Salsa20StreamCipher(key, nonce, counter);
            using var single = new Salsa20StreamCipher(key, nonce, counter);

            bulk.XorKeystreamBlocks(input, actual);

            CollectionAssert.AreEqual(XorBlockByBlock(single, input), actual, $"{blocks} blocks");

            byte[] nextBulk = new byte[Salsa20StreamCipher.BlockSizeBytes];
            byte[] nextSingle = new byte[Salsa20StreamCipher.BlockSizeBytes];
            bulk.NextKeystreamBlock(nextBulk);
            single.NextKeystreamBlock(nextSingle);
            CollectionAssert.AreEqual(nextSingle, nextBulk, $"the block after {blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that the counter carries from its low word into its high word, and wraps from its largest value to
    /// zero, within one call, as successive single blocks do.
    /// </summary>
    /// <param name="initialCounter">The first block's counter.</param>
    [TestMethod]
    [DataRow(0x0000_0000_FFFF_FFF7UL)]
    [DataRow(0xFFFF_FFFF_FFFF_FFF7UL)]
    public void XorKeystreamBlocks_WhenCounterCarriesOrWraps_ShouldMatchSuccessiveKeystreamBlocks(ulong initialCounter)
    {
        var random = new Random(0x5A15_1002);
        byte[] key = NextBytes(random, Salsa20StreamCipher.KeySize256Bytes);
        byte[] nonce = NextBytes(random, Salsa20StreamCipher.NonceSizeBytes);
        byte[] input = NextBytes(random, 40 * Salsa20StreamCipher.BlockSizeBytes);
        byte[] actual = new byte[input.Length];
        using var bulk = new Salsa20StreamCipher(key, nonce, initialCounter);
        using var single = new Salsa20StreamCipher(key, nonce, initialCounter);

        bulk.XorKeystreamBlocks(input, actual);

        CollectionAssert.AreEqual(XorBlockByBlock(single, input), actual);
    }

    /// <summary>
    /// Verifies that input that is not a whole number of blocks is rejected with <see cref="ArgumentException" />
    /// naming the parameter.
    /// </summary>
    /// <param name="length">The rejected input length.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(63)]
    [DataRow(65)]
    public void XorKeystreamBlocks_WhenInputIsNotWholeBlocks_ShouldThrowArgumentException(int length)
    {
        using var engine = new Salsa20StreamCipher(new byte[32], new byte[8], 0);
        byte[] input = new byte[length];
        byte[] output = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            engine.XorKeystreamBlocks(input, output);
        });

        Assert.AreEqual("input", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an output shorter than the input is rejected with <see cref="ArgumentException" /> naming the
    /// parameter.
    /// </summary>
    [TestMethod]
    public void XorKeystreamBlocks_WhenOutputIsShorterThanInput_ShouldThrowArgumentException()
    {
        using var engine = new Salsa20StreamCipher(new byte[32], new byte[8], 0);
        byte[] input = new byte[2 * Salsa20StreamCipher.BlockSizeBytes];
        byte[] output = new byte[input.Length - 1];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            engine.XorKeystreamBlocks(input, output);
        });

        Assert.AreEqual("output", ex.ParamName);
    }
}
