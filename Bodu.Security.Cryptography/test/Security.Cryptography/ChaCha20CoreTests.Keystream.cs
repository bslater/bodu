// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.Keystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that a <see cref="ChaCha20Core.Keystream" /> reports the kernel dispatch selects, which its runs of
    /// blocks take, so that the Poly1305 AEADs plan their draws for it.
    /// </summary>
    [TestMethod]
    public void KeystreamKernel_WhenQueried_ShouldBeTheKernelDispatchSelects()
    {
        ChaCha20Core.Keystream keystream = default;

        Assert.AreEqual(ChaCha20Core.SelectKernel(), keystream.Kernel);
    }

    /// <summary>
    /// Verifies that the keystream drawn a block at a time from a <see cref="ChaCha20Core.Keystream" /> is the block
    /// function at successive counters from the initial one.
    /// </summary>
    [TestMethod]
    public void KeystreamNextBlock_WhenDrawnRepeatedly_ShouldMatchBlockAtSuccessiveCounters()
    {
        var random = new Random(0x0C4A_2001);
        byte[] key = NextBytes(random, ChaCha20Core.KeyBytes);
        byte[] nonce = NextBytes(random, ChaCha20Core.NonceBytes);
        uint[] state = new uint[ChaCha20Core.StateWords];
        ChaCha20Core.Initialize(state, key, nonce);
        ChaCha20Core.Keystream keystream = default;
        keystream.Initialize(key, nonce, counter: 41);
        byte[] expected = new byte[ChaCha20Core.BlockBytes];
        byte[] actual = new byte[ChaCha20Core.BlockBytes];

        for (uint block = 0; block < 5; block++)
        {
            ChaCha20Core.Block(state, 41 + block, expected);
            keystream.NextBlock(actual);

            CollectionAssert.AreEqual(expected, actual, $"block {block}");
        }
    }

    /// <summary>
    /// Verifies that combining runs of whole blocks with a <see cref="ChaCha20Core.Keystream" />, from none to twenty,
    /// matches the block function one block at a time and leaves the keystream at the block after the run.
    /// </summary>
    [TestMethod]
    public void KeystreamXorBlocks_WhenCombiningARun_ShouldMatchBlockByBlockAndAdvancePastIt()
    {
        var random = new Random(0x0C4A_2002);

        for (int blocks = 0; blocks <= 20; blocks++)
        {
            byte[] key = NextBytes(random, ChaCha20Core.KeyBytes);
            byte[] nonce = NextBytes(random, ChaCha20Core.NonceBytes);
            uint counter = (uint)random.NextInt64(0, 1L << 32);
            byte[] input = NextBytes(random, blocks * ChaCha20Core.BlockBytes);
            uint[] state = new uint[ChaCha20Core.StateWords];
            ChaCha20Core.Initialize(state, key, nonce);
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(key, nonce, counter);
            byte[] actual = new byte[input.Length];
            byte[] expectedNext = new byte[ChaCha20Core.BlockBytes];
            byte[] actualNext = new byte[ChaCha20Core.BlockBytes];

            keystream.XorBlocks(input, actual);
            keystream.NextBlock(actualNext);
            ChaCha20Core.Block(state, counter + (uint)blocks, expectedNext);

            CollectionAssert.AreEqual(XorBlockByBlock(state, counter, input), actual, $"{blocks} blocks");
            CollectionAssert.AreEqual(expectedNext, actualNext, $"the block after {blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that the counter of a <see cref="ChaCha20Core.Keystream" /> counts up modulo 2^32 through a run of
    /// blocks and the single blocks after it, as the block function at each counter does.
    /// </summary>
    [TestMethod]
    public void KeystreamXorBlocks_WhenCounterWraps_ShouldMatchBlockByBlock()
    {
        var random = new Random(0x0C4A_2003);
        byte[] key = NextBytes(random, ChaCha20Core.KeyBytes);
        byte[] nonce = NextBytes(random, ChaCha20Core.NonceBytes);
        byte[] input = NextBytes(random, 9 * ChaCha20Core.BlockBytes);
        uint[] state = new uint[ChaCha20Core.StateWords];
        ChaCha20Core.Initialize(state, key, nonce);
        ChaCha20Core.Keystream keystream = default;
        keystream.Initialize(key, nonce, uint.MaxValue - 3);
        byte[] actual = new byte[input.Length];
        byte[] expectedNext = new byte[ChaCha20Core.BlockBytes];
        byte[] actualNext = new byte[ChaCha20Core.BlockBytes];

        keystream.XorBlocks(input, actual);
        keystream.NextBlock(actualNext);
        ChaCha20Core.Block(state, 5, expectedNext);

        CollectionAssert.AreEqual(XorBlockByBlock(state, uint.MaxValue - 3, input), actual);
        CollectionAssert.AreEqual(expectedNext, actualNext);
    }

    /// <summary>
    /// Verifies that clearing a <see cref="ChaCha20Core.Keystream" /> zeroes its state and counter: it then produces
    /// the block of an all-zero state at counter zero.
    /// </summary>
    [TestMethod]
    public void KeystreamClear_WhenCalled_ShouldLeaveTheAllZeroStateAtCounterZero()
    {
        var random = new Random(0x0C4A_2004);
        ChaCha20Core.Keystream keystream = default;
        keystream.Initialize(NextBytes(random, ChaCha20Core.KeyBytes), NextBytes(random, ChaCha20Core.NonceBytes), counter: 7);
        byte[] expected = new byte[ChaCha20Core.BlockBytes];
        byte[] actual = new byte[ChaCha20Core.BlockBytes];
        ChaCha20Core.Block(new uint[ChaCha20Core.StateWords], 0, expected);

        keystream.Clear();
        keystream.NextBlock(actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that seeding a <see cref="ChaCha20Core.Keystream" /> with a key that is not 32 bytes long throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the key.
    /// </summary>
    /// <param name="length">The rejected key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    public void KeystreamInitialize_WhenKeyIsNot32Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(new byte[length], new byte[ChaCha20Core.NonceBytes], counter: 0);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that seeding a <see cref="ChaCha20Core.Keystream" /> with a nonce that is not 12 bytes long throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the nonce.
    /// </summary>
    /// <param name="length">The rejected nonce length.</param>
    [TestMethod]
    [DataRow(8)]
    [DataRow(11)]
    [DataRow(24)]
    public void KeystreamInitialize_WhenNonceIsNot12Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Keystream keystream = default;
            keystream.Initialize(new byte[ChaCha20Core.KeyBytes], new byte[length], counter: 0);
        });

        Assert.AreEqual("nonce", ex.ParamName);
    }
}
