// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20CoreTests.Keystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Salsa20CoreTests
{
    /// <summary>
    /// Verifies that a <see cref="Salsa20Core.Keystream" /> reports the kernel dispatch selects, which its runs of
    /// blocks take, so that the Poly1305 AEADs plan their draws for it.
    /// </summary>
    [TestMethod]
    public void KeystreamKernel_WhenQueried_ShouldBeTheKernelDispatchSelects()
    {
        Salsa20Core.Keystream keystream = default;

        Assert.AreEqual(ChaCha20Core.SelectKernel(), keystream.Kernel);
    }

    /// <summary>
    /// Verifies that the keystream drawn a block at a time from a <see cref="Salsa20Core.Keystream" /> is the core
    /// function at successive counters from the initial one, carrying from the counter's low word into its high word,
    /// under both key sizes.
    /// </summary>
    /// <param name="keyBytes">The key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    public void KeystreamNextBlock_WhenDrawnRepeatedly_ShouldMatchBlockAtSuccessiveCounters(int keyBytes)
    {
        const ulong InitialCounter = 0x0000_0001_FFFF_FFFE;
        var random = new Random(0x5A15_2001 + keyBytes);
        byte[] key = NextBytes(random, keyBytes);
        byte[] nonce = NextBytes(random, Salsa20Core.NonceBytes);
        uint[] state = new uint[Salsa20Core.StateWords];
        Salsa20Core.Initialize(state, key, nonce);
        Salsa20Core.Keystream keystream = default;
        keystream.Initialize(key, nonce, InitialCounter);
        byte[] expected = new byte[Salsa20Core.BlockBytes];
        byte[] actual = new byte[Salsa20Core.BlockBytes];

        for (ulong block = 0; block < 5; block++)
        {
            Salsa20Core.Block(state, InitialCounter + block, expected);
            keystream.NextBlock(actual);

            CollectionAssert.AreEqual(expected, actual, $"block {block}");
        }
    }

    /// <summary>
    /// Verifies that combining runs of whole blocks with a <see cref="Salsa20Core.Keystream" />, from none to twenty,
    /// matches the core function one block at a time and leaves the keystream at the block after the run.
    /// </summary>
    [TestMethod]
    public void KeystreamXorBlocks_WhenCombiningARun_ShouldMatchBlockByBlockAndAdvancePastIt()
    {
        var random = new Random(0x5A15_2002);

        for (int blocks = 0; blocks <= 20; blocks++)
        {
            byte[] key = NextBytes(random, Salsa20Core.Key256Bytes);
            byte[] nonce = NextBytes(random, Salsa20Core.NonceBytes);
            ulong counter = (ulong)random.NextInt64();
            byte[] input = NextBytes(random, blocks * Salsa20Core.BlockBytes);
            uint[] state = new uint[Salsa20Core.StateWords];
            Salsa20Core.Initialize(state, key, nonce);
            Salsa20Core.Keystream keystream = default;
            keystream.Initialize(key, nonce, counter);
            byte[] actual = new byte[input.Length];
            byte[] expectedNext = new byte[Salsa20Core.BlockBytes];
            byte[] actualNext = new byte[Salsa20Core.BlockBytes];

            keystream.XorBlocks(input, actual);
            keystream.NextBlock(actualNext);
            Salsa20Core.Block(state, counter + (ulong)blocks, expectedNext);

            CollectionAssert.AreEqual(XorBlockByBlock(state, counter, input), actual, $"{blocks} blocks");
            CollectionAssert.AreEqual(expectedNext, actualNext, $"the block after {blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that the counter of a <see cref="Salsa20Core.Keystream" /> carries into its high word, and wraps from
    /// its largest value to zero, through a run of blocks and the single block after it, as the core function at each
    /// counter does.
    /// </summary>
    /// <param name="initialCounter">The first block's counter.</param>
    [TestMethod]
    [DataRow(0x0000_0000_FFFF_FFFBUL)]
    [DataRow(0xFFFF_FFFF_FFFF_FFFBUL)]
    public void KeystreamXorBlocks_WhenCounterCarriesOrWraps_ShouldMatchBlockByBlock(ulong initialCounter)
    {
        var random = new Random(0x5A15_2003);
        byte[] key = NextBytes(random, Salsa20Core.Key256Bytes);
        byte[] nonce = NextBytes(random, Salsa20Core.NonceBytes);
        byte[] input = NextBytes(random, 9 * Salsa20Core.BlockBytes);
        uint[] state = new uint[Salsa20Core.StateWords];
        Salsa20Core.Initialize(state, key, nonce);
        Salsa20Core.Keystream keystream = default;
        keystream.Initialize(key, nonce, initialCounter);
        byte[] actual = new byte[input.Length];
        byte[] expectedNext = new byte[Salsa20Core.BlockBytes];
        byte[] actualNext = new byte[Salsa20Core.BlockBytes];

        keystream.XorBlocks(input, actual);
        keystream.NextBlock(actualNext);
        Salsa20Core.Block(state, unchecked(initialCounter + 9), expectedNext);

        CollectionAssert.AreEqual(XorBlockByBlock(state, initialCounter, input), actual);
        CollectionAssert.AreEqual(expectedNext, actualNext);
    }

    /// <summary>
    /// Verifies that clearing a <see cref="Salsa20Core.Keystream" /> zeroes its state and counter: it then produces the
    /// block of an all-zero state at counter zero.
    /// </summary>
    [TestMethod]
    public void KeystreamClear_WhenCalled_ShouldLeaveTheAllZeroStateAtCounterZero()
    {
        var random = new Random(0x5A15_2004);
        Salsa20Core.Keystream keystream = default;
        keystream.Initialize(NextBytes(random, Salsa20Core.Key256Bytes), NextBytes(random, Salsa20Core.NonceBytes), counter: 7);
        byte[] expected = new byte[Salsa20Core.BlockBytes];
        byte[] actual = new byte[Salsa20Core.BlockBytes];
        Salsa20Core.Block(new uint[Salsa20Core.StateWords], 0, expected);

        keystream.Clear();
        keystream.NextBlock(actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that seeding a <see cref="Salsa20Core.Keystream" /> with a key that is neither 16 nor 32 bytes long
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the key.
    /// </summary>
    /// <param name="length">The rejected key length.</param>
    [TestMethod]
    [DataRow(15)]
    [DataRow(24)]
    [DataRow(33)]
    public void KeystreamInitialize_WhenKeyIsNeither16Nor32Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Keystream keystream = default;
            keystream.Initialize(new byte[length], new byte[Salsa20Core.NonceBytes], counter: 0);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that seeding a <see cref="Salsa20Core.Keystream" /> with a nonce that is not 8 bytes long throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the nonce.
    /// </summary>
    /// <param name="length">The rejected nonce length.</param>
    [TestMethod]
    [DataRow(7)]
    [DataRow(12)]
    [DataRow(24)]
    public void KeystreamInitialize_WhenNonceIsNot8Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Keystream keystream = default;
            keystream.Initialize(new byte[Salsa20Core.Key256Bytes], new byte[length], counter: 0);
        });

        Assert.AreEqual("nonce", ex.ParamName);
    }
}
