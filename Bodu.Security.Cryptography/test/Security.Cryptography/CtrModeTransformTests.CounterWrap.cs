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
    /// <c>_counterWrapped</c> flag when the counter matches its initial value after increment. Driving a full
    /// 2^64 / 2^128 cycle is infeasible, so this test uses reflection to position the counter one increment short
    /// of wrap (all-<c>0xFF</c> with an initial counter of all zeros) and asserts both the wrap-and-latch
    /// transition and the rejection on the next call.
    /// </remarks>
    [TestMethod]
    public void Transform_WhenCounterWouldWrapToInitial_ShouldThrowCryptographicException()
    {
        using var cipher = new SkipjackBlockCipher(new byte[10]);
        var blockSize = cipher.BlockSize / 8;

        var initialCounter = new byte[blockSize];
        using var transform = new CtrModeTransform(cipher, initialCounter);

        // Drive the internal counter to all-0xFF. The next increment carries through every byte and lands back
        // on the initial all-zero state, latching _counterWrapped.
        FieldInfo counterField = typeof(CtrModeTransform)
            .GetField("_counter", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var atMax = new byte[blockSize];
        Array.Fill(atMax, (byte)0xFF);
        counterField.SetValue(transform, atMax);

        var input = new byte[blockSize];
        var output = new byte[blockSize];

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
    /// Verifies that once the counter-wrap latch is set, every subsequent <see cref="CtrModeTransform.Transform" />
    /// call continues to throw rather than silently producing keystream.
    /// </summary>
    [TestMethod]
    public void Transform_WhenCounterWrapLatched_ShouldThrowOnEverySubsequentCall()
    {
        using var cipher = new SkipjackBlockCipher(new byte[10]);
        var blockSize = cipher.BlockSize / 8;

        using var transform = new CtrModeTransform(cipher, new byte[blockSize]);

        FieldInfo latchField = typeof(CtrModeTransform)
            .GetField("_counterWrapped", BindingFlags.NonPublic | BindingFlags.Instance)!;
        latchField.SetValue(transform, true);

        var input = new byte[blockSize];
        var output = new byte[blockSize];

        for (var i = 0; i < 3; i++)
        {
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                _ = transform.Transform(input, output, encrypt: true);
            });
        }
    }
}
