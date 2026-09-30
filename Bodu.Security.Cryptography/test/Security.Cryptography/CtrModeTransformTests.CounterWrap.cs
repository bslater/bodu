// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.CounterWrap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CtrModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CtrModeTransform.Transform" /> latches and throws
    /// <see cref="CryptographicException" /> when the counter would wrap back to its initial value, preventing
    /// keystream reuse under a single (key, IV) pair.
    /// </summary>
    /// <remarks>
    /// The CTR engine uses big-endian increment of the cipher-block-sized counter and latches a
    /// <c>_counterWrapped</c> flag when the increment carries out past the most significant byte (a full 2^n
    /// rollover to zero). Driving a full 2^64 / 2^128 cycle is infeasible, so this test uses reflection to position
    /// the counter one increment short of rollover (all-<c>0xFF</c>) and asserts both the wrap-and-latch transition
    /// and the rejection on the next call.
    /// </remarks>
    [TestMethod]
    public void Transform_WhenCounterWouldWrapToInitial_ShouldThrowCryptographicException()
    {
        using var cipher = new SkipjackBlockCipher(new byte[10]);
        int blockSize = cipher.BlockSize / 8;

        byte[] initialCounter = new byte[blockSize];
        using var transform = new CtrModeTransform(cipher, initialCounter);

        // Drive the internal counter to all-0xFF. The next increment carries through every byte and lands back
        // on the initial all-zero state, latching _counterWrapped.
        FieldInfo counterField = typeof(CtrModeTransform)
            .GetField("_counter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        byte[] atMax = new byte[blockSize];
        Array.Fill(atMax, (byte)0xFF);
        counterField.SetValue(transform, atMax);

        byte[] input = new byte[blockSize];
        byte[] output = new byte[blockSize];

        // First call succeeds: it emits one keystream block under the 0xFF... counter, then increment wraps to
        // all-zero (== initialCounter) and latches.
        _ = transform.Transform(input, output, encrypt: true);

        // Second call must throw before producing any duplicate keystream.
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = transform.Transform(input, output, encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that the counter-wrap latch trips at the true 2^n counter rollover even when the initial counter is
    /// non-zero — the counter reaching all-zero (a full-space rollover) is what latches, not a match against the
    /// initial value.
    /// </summary>
    /// <remarks>
    /// With a non-zero initial counter, the counter passes through all-zero (the 2^n rollover point) well before it
    /// would return to its initial value. Detecting the rollover on the increment's carry-out latches at that point,
    /// which is at or before return-to-initial and therefore strictly conservative against keystream reuse.
    /// </remarks>
    [TestMethod]
    public void Transform_WhenCounterRollsOverWithNonZeroInitialCounter_ShouldThrowCryptographicException()
    {
        using var cipher = new SkipjackBlockCipher(new byte[10]);
        int blockSize = cipher.BlockSize / 8;

        // Non-zero initial counter: the all-zero rollover state never equals it, so match-against-initial detection
        // would miss the wrap. Carry-out detection latches at the rollover regardless.
        byte[] initialCounter = new byte[blockSize];
        initialCounter[blockSize - 1] = 0x01;
        using var transform = new CtrModeTransform(cipher, initialCounter);

        FieldInfo counterField = typeof(CtrModeTransform)
            .GetField("_counter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        byte[] atMax = new byte[blockSize];
        Array.Fill(atMax, (byte)0xFF);
        counterField.SetValue(transform, atMax);

        byte[] input = new byte[blockSize];
        byte[] output = new byte[blockSize];

        // First call emits the 0xFF... keystream block; the increment then carries out past the top byte, rolling
        // the counter to all-zero and latching the wrap.
        _ = transform.Transform(input, output, encrypt: true);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = transform.Transform(input, output, encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that once the counter-wrap latch is set, every subsequent <see cref="CtrModeTransform.Transform" />
    /// call continues to throw rather than silently producing keystream.
    /// </summary>
    [TestMethod]
    public void Transform_WhenCounterWrapLatched_ShouldThrowOnEverySubsequentCall()
    {
        using var cipher = new SkipjackBlockCipher(new byte[10]);
        int blockSize = cipher.BlockSize / 8;

        using var transform = new CtrModeTransform(cipher, new byte[blockSize]);

        FieldInfo latchField = typeof(CtrModeTransform)
            .GetField("_counterWrapped", BindingFlags.NonPublic | BindingFlags.Instance)!;
        latchField.SetValue(transform, true);

        byte[] input = new byte[blockSize];
        byte[] output = new byte[blockSize];

        for (int i = 0; i < 3; i++)
        {
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _ = transform.Transform(input, output, encrypt: true);
            });
        }
    }

    /// <summary>
    /// Verifies that when the counter wraps partway through a run of counters, the blocks before the wrap are written
    /// exactly as block-at-a-time processing writes them, the rest of the output is left untouched, and the call throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Transform_WhenCounterWrapsInsideARun_ShouldWriteTheBlocksBeforeTheWrapThenThrow()
    {
        using var cipher = new AesBlockCipher(new byte[16]);
        using var transform = new CtrModeTransform(cipher, new byte[16]);
        byte[] nearMax = new byte[16];
        Array.Fill(nearMax, (byte)0xFF);
        nearMax[^1] = 0xFD;
        typeof(CtrModeTransform).GetField("_counter", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(transform, nearMax.Clone());

        byte[] input = new byte[16 * 5];
        new Random(5).NextBytes(input);
        byte[] output = new byte[input.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            transform.Transform(input, output, encrypt: true);
        });

        byte[] expected = ReferenceKeystreamXor(cipher, nearMax, input.AsSpan(0, 16 * 3).ToArray(), [16 * 3]);
        CollectionAssert.AreEqual(expected, output[..(16 * 3)], "the three blocks before the wrap");
        CollectionAssert.AreEqual(new byte[16 * 2], output[(16 * 3)..], "the blocks after the wrap");
    }

    /// <summary>
    /// Verifies that when five counter values remain before the wrap and the input needs more, the five blocks are
    /// written exactly as block-at-a-time processing writes them, the rest of the output is left untouched, and the call
    /// throws <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    [TestMethod]
    [DynamicData(nameof(BatchingCiphers))]
    public void Transform_WhenCounterWrapsPartwayThroughAGroup_ShouldWriteTheBlocksBeforeTheWrapThenThrow(string name, Func<IBlockCipher> create)
    {
        using IBlockCipher cipher = create();
        int blockSize = cipher.BlockSize / 8;
        using var transform = new CtrModeTransform(cipher, new byte[blockSize]);
        byte[] nearMax = NearMaximumCounter(blockSize, remaining: 5);
        typeof(CtrModeTransform).GetField("_counter", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(transform, nearMax.Clone());

        byte[] input = new byte[(blockSize * 12) + 3];
        new Random(0x5E19).NextBytes(input);
        byte[] output = new byte[input.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            transform.Transform(input, output, encrypt: true);
        });

        byte[] expected = ReferenceKeystreamXor(cipher, nearMax, input.AsSpan(0, blockSize * 5).ToArray(), [blockSize * 5]);
        CollectionAssert.AreEqual(expected, output[..(blockSize * 5)], $"{name}: the five blocks before the wrap");
        CollectionAssert.AreEqual(new byte[output.Length - (blockSize * 5)], output[(blockSize * 5)..], $"{name}: the output after the wrap");
    }

    /// <summary>
    /// Verifies that when the input's last block, a partial one, takes the counter's last value, every block is written
    /// as block-at-a-time processing writes it without an exception, and the next call throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="name">The cipher's name, for the test's display.</param>
    /// <param name="create">Creates a fresh keyed cipher.</param>
    [TestMethod]
    [DynamicData(nameof(BatchingCiphers))]
    public void Transform_WhenLastPartialBlockTakesTheCounterLastValue_ShouldWriteEveryBlockThenThrowOnTheNextCall(string name, Func<IBlockCipher> create)
    {
        using IBlockCipher cipher = create();
        int blockSize = cipher.BlockSize / 8;
        using var transform = new CtrModeTransform(cipher, new byte[blockSize]);
        byte[] nearMax = NearMaximumCounter(blockSize, remaining: 5);
        typeof(CtrModeTransform).GetField("_counter", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(transform, nearMax.Clone());

        byte[] input = new byte[(blockSize * 4) + 3];
        new Random(0x5E1A).NextBytes(input);
        byte[] output = new byte[input.Length];

        transform.Transform(input, output, encrypt: true);

        CollectionAssert.AreEqual(ReferenceKeystreamXor(cipher, nearMax, input, [input.Length]), output, name);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            transform.Transform(new byte[1], new byte[1], encrypt: true);
        });
    }

    /// <summary>
    /// Returns a counter block with the specified number of values left before it wraps: every byte 0xFF but the last.
    /// </summary>
    /// <param name="blockSize">The block size, in bytes.</param>
    /// <param name="remaining">The number of counter values left, 1 to 256.</param>
    /// <returns>The counter block.</returns>
    private static byte[] NearMaximumCounter(int blockSize, int remaining)
    {
        byte[] counter = new byte[blockSize];
        counter.AsSpan().Fill(0xFF);
        counter[^1] = (byte)(256 - remaining);
        return counter;
    }
}
