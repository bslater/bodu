// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.RotateBitsLeft.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Rotates the bits of the specified <see cref="byte" /> value to the left by the specified number of positions.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 8]. A count of 0 or 8 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="byte" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than 0 or greater than 8.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte RotateBitsLeft(this byte value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 8);
        return (byte)((value << count) | (value >> (8 - count)));
    }

    /// <summary>
    /// Rotates the bits of the specified <see cref="ushort" /> value to the left by the specified number of positions.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 16]. A count of 0 or 16 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="ushort" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than 0 or greater than 16.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort RotateBitsLeft(this ushort value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 16);
        return (ushort)((value << count) | (value >> (16 - count)));
    }

    /// <summary>
    /// Rotates the bits of the specified <see cref="uint" /> value to the left by the specified number of positions.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 32]. A count of 0 or 32 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="uint" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than 0 or greater than 32.
    /// </exception>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateLeft(uint, int)" /> after validation, allowing the JIT to lower the
    /// rotation to a single CPU instruction on supported platforms.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateBitsLeft(this uint value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 32);
        return BitOperations.RotateLeft(value, count);
    }

    /// <summary>
    /// Rotates the bits of the specified <see cref="ulong" /> value to the left by the specified number of positions.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 64]. A count of 0 or 64 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="ulong" /> with the bits of <paramref name="value" /> rotated left by <paramref name="count" />
    /// positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is less than 0 or greater than 64.
    /// </exception>
    /// <remarks>
    /// Delegates to <see cref="BitOperations.RotateLeft(ulong, int)" /> after validation, allowing the JIT to lower the
    /// rotation to a single CPU instruction on supported platforms.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateBitsLeft(this ulong value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 64);
        return BitOperations.RotateLeft(value, count);
    }
}
