// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyHelper.ConstantTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

internal static partial class CryptographyHelper
{
    /// <summary>
    /// Computes a byte-wise difference accumulator over two equal-length spans: the result is 0 when the spans are
    /// identical and non-zero otherwise, with no data-dependent branches or early exit.
    /// </summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span. Must have the same length as <paramref name="left" />.</param>
    /// <returns>0 when the spans are equal; a non-zero value otherwise.</returns>
    /// <exception cref="ArgumentException">The spans differ in length.</exception>
    /// <remarks>
    /// The raw accumulator is returned instead of a <see cref="bool" /> so callers can derive a selection mask
    /// arithmetically (see <see cref="ConstantTimeSelect" />); converting through a <see cref="bool" /> would invite
    /// the JIT to materialize a branch between the comparison and the dependent selection.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConstantTimeDifference(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(right, left.Length);

        int accumulator = 0;
        for (int i = 0; i < left.Length; i++)
            accumulator |= left[i] ^ right[i];

        return accumulator;
    }

    /// <summary>
    /// Copies <paramref name="whenZero" /> into <paramref name="destination" /> when <paramref name="difference" /> is
    /// 0, and <paramref name="whenNonZero" /> otherwise, using a mask derived arithmetically from the accumulator so
    /// the selection has no data-dependent branch.
    /// </summary>
    /// <param name="difference">
    /// The difference accumulator, typically from <see cref="ConstantTimeDifference" />.
    /// </param>
    /// <param name="whenZero">The bytes selected when the accumulator is 0.</param>
    /// <param name="whenNonZero">The bytes selected when the accumulator is non-zero.</param>
    /// <param name="destination">The span receiving the selected bytes.</param>
    /// <exception cref="ArgumentException">The spans differ in length.</exception>
    public static void ConstantTimeSelect(
        int difference,
        ReadOnlySpan<byte> whenZero,
        ReadOnlySpan<byte> whenNonZero,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(whenNonZero, whenZero.Length);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination, whenZero.Length);

        // (x | -x) has its sign bit set exactly when x != 0; the arithmetic shift smears it into 0 or -1.
        byte mask = (byte)((difference | -difference) >> 31);

        for (int i = 0; i < destination.Length; i++)
            destination[i] = (byte)((whenNonZero[i] & mask) | (whenZero[i] & (byte)~mask));
    }
}
