// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Tamper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Provides byte-mutation helpers for tamper-detection tests (modified ciphertext, tag, or associated data).
/// </summary>
/// <remarks>
/// Each helper returns a mutated copy so the original vector remains intact for subsequent assertions. The helpers
/// only mutate; the tamper test itself keeps its <c>Assert.ThrowsExactly</c> (or fixed-time comparison) explicit at
/// the call site.
/// </remarks>
internal static class Tamper
{
    /// <summary>
    /// Returns a copy of the source buffer with a single bit inverted.
    /// </summary>
    /// <param name="source">The buffer to copy and mutate. Must not be <see langword="null" />.</param>
    /// <param name="bitIndex">The zero-based index of the bit to flip, counted least-significant-bit first within
    /// each byte.</param>
    /// <returns>A new array identical to <paramref name="source" /> except for the flipped bit.</returns>
    public static byte[] FlipBit(byte[] source, int bitIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(bitIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bitIndex, source.Length * 8);

        var copy = (byte[])source.Clone();
        copy[bitIndex / 8] ^= (byte)(1 << (bitIndex % 8));
        return copy;
    }

    /// <summary>
    /// Returns a copy of the source buffer with every bit of a single byte inverted.
    /// </summary>
    /// <param name="source">The buffer to copy and mutate. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based index of the byte to invert.</param>
    /// <returns>A new array identical to <paramref name="source" /> except for the inverted byte.</returns>
    public static byte[] FlipByte(byte[] source, int index)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, source.Length);

        var copy = (byte[])source.Clone();
        copy[index] ^= 0xFF;
        return copy;
    }
}
