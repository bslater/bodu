// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128CipherTests.XorCounterKeystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

internal sealed partial class Serpent128CipherTests
{
    /// <summary>
    /// Verifies that the cipher's counter-mode entry point combines the input with the keystream of successive counter
    /// blocks as encrypting each counter block with <see cref="Serpent128Cipher.Encrypt" /> does, for seeded keys of
    /// every Serpent key size, seeded counters and lengths with and without a partial last block, and advances the
    /// counter one block for every whole or partial block.
    /// </summary>
    /// <param name="keyBytes">The key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void XorCounterKeystream_WhenKeysAndCountersAreSeededRandom_ShouldMatchEncryptOfEachCounterBlock(int keyBytes)
    {
        var random = new Random(0x5E4F_C101 + keyBytes);

        foreach (int length in new[] { 0, 1, 16, 63, 64, 65, 127, 128, 129, 200, 1000 })
        {
            byte[] key = new byte[keyBytes];
            byte[] input = new byte[length];
            random.NextBytes(key);
            random.NextBytes(input);
            var counter = new UInt128((ulong)random.NextInt64(long.MinValue, long.MaxValue), (ulong)random.NextInt64(long.MinValue, long.MaxValue));

            using var cipher = new Serpent128Cipher(key);
            byte[] expected = new byte[length];
            byte[] block = new byte[16];
            byte[] keystream = new byte[16];
            for (int offset = 0; offset < length; offset += 16)
            {
                BinaryPrimitives.WriteUInt128BigEndian(block, counter + (UInt128)(offset / 16));
                cipher.Encrypt(block, keystream);
                for (int i = 0; i < Math.Min(16, length - offset); i++)
                    expected[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
            }

            ulong high = (ulong)(counter >> 64);
            ulong low = (ulong)counter;
            byte[] actual = new byte[length];
            ((ICounterModeBlockCipher)cipher).XorCounterKeystream(ref high, ref low, input, actual);

            CollectionAssert.AreEqual(expected, actual, $"{length} bytes");
            Assert.AreEqual(counter + (UInt128)((length + 15) / 16), new UInt128(high, low), $"{length} bytes: the counter");
        }
    }

    /// <summary>
    /// Verifies that the counter-mode entry point of a disposed cipher throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void XorCounterKeystream_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new Serpent128Cipher(new byte[16]);
        cipher.Dispose();
        ulong high = 0;
        ulong low = 0;

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            ((ICounterModeBlockCipher)cipher).XorCounterKeystream(ref high, ref low, new byte[16], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that an output shorter than the input is rejected with <see cref="ArgumentException" /> naming the
    /// output, before the counter moves.
    /// </summary>
    [TestMethod]
    public void XorCounterKeystream_WhenOutputIsShorterThanInput_ShouldThrowArgumentException()
    {
        using var cipher = new Serpent128Cipher(new byte[16]);
        ulong high = 7;
        ulong low = 9;

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICounterModeBlockCipher)cipher).XorCounterKeystream(ref high, ref low, new byte[20], new byte[19]);
        });

        Assert.AreEqual("output", ex.ParamName);
        Assert.AreEqual((7UL, 9UL), (high, low));
    }
}
