// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20StreamCipherTests.XorKeystreamBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20StreamCipherTests
{
    /// <summary>
    /// Verifies that combining any number of whole blocks at once, from none to a hundred, matches drawing the same
    /// blocks one at a time, and leaves the engine where those draws leave it.
    /// </summary>
    [TestMethod]
    public void XorKeystreamBlocks_ShouldMatchSuccessiveKeystreamBlocks()
    {
        var random = new Random(0x0C4A_1001);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([63, 64, 65, 100]))
        {
            byte[] key = NextBytes(random, ChaCha20StreamCipher.KeySizeBytes);
            byte[] nonce = NextBytes(random, ChaCha20StreamCipher.NonceSizeBytes);
            uint counter = (uint)random.Next();
            byte[] input = NextBytes(random, blocks * ChaCha20StreamCipher.BlockSizeBytes);
            byte[] actual = new byte[input.Length];
            using var bulk = new ChaCha20StreamCipher(key, nonce, counter);
            using var single = new ChaCha20StreamCipher(key, nonce, counter);

            bulk.XorKeystreamBlocks(input, actual);

            CollectionAssert.AreEqual(XorBlockByBlock(single, input), actual, $"{blocks} blocks");

            byte[] nextBulk = new byte[ChaCha20StreamCipher.BlockSizeBytes];
            byte[] nextSingle = new byte[ChaCha20StreamCipher.BlockSizeBytes];
            bulk.NextKeystreamBlock(nextBulk);
            single.NextKeystreamBlock(nextSingle);
            CollectionAssert.AreEqual(nextSingle, nextBulk, $"the block after {blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that the counter wraps from its largest value to zero within one call, as successive single blocks do.
    /// </summary>
    [TestMethod]
    public void XorKeystreamBlocks_WhenCounterWraps_ShouldMatchSuccessiveKeystreamBlocks()
    {
        var random = new Random(0x0C4A_1002);
        byte[] key = NextBytes(random, ChaCha20StreamCipher.KeySizeBytes);
        byte[] nonce = NextBytes(random, ChaCha20StreamCipher.NonceSizeBytes);
        byte[] input = NextBytes(random, 40 * ChaCha20StreamCipher.BlockSizeBytes);
        byte[] actual = new byte[input.Length];
        using var bulk = new ChaCha20StreamCipher(key, nonce, uint.MaxValue - 9);
        using var single = new ChaCha20StreamCipher(key, nonce, uint.MaxValue - 9);

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
        using var engine = new ChaCha20StreamCipher(new byte[32], new byte[12], 0);
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
        using var engine = new ChaCha20StreamCipher(new byte[32], new byte[12], 0);
        byte[] input = new byte[2 * ChaCha20StreamCipher.BlockSizeBytes];
        byte[] output = new byte[input.Length - 1];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            engine.XorKeystreamBlocks(input, output);
        });

        Assert.AreEqual("output", ex.ParamName);
    }
}
