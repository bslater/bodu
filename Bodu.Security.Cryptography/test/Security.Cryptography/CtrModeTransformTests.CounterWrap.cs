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
}
