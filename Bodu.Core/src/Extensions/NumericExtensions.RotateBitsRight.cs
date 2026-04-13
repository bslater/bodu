// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.RotateBitsRight.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Rotates the bits of the specified <see cref="byte"/> value to the right by the specified number of positions.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 8]. A count of 0 or 8 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="byte"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is less than 0 or greater than 8.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte RotateBitsRight(this byte value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 8);
        return (byte)((value >> count) | (value << (8 - count)));
    }

    /// <summary>
    /// Rotates the bits of the specified <see cref="ushort"/> value to the right by the specified number of positions.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 16]. A count of 0 or 16 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="ushort"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is less than 0 or greater than 16.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort RotateBitsRight(this ushort value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 16);
        return (ushort)((value >> count) | (value << (16 - count)));
    }

    /// <summary>
    /// Rotates the bits of the specified <see cref="uint"/> value to the right by the specified number of positions.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 32]. A count of 0 or 32 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="uint"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is less than 0 or greater than 32.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateBitsRight(this uint value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 32);
        return (value >> count) | (value << (32 - count));
    }

    /// <summary>
    /// Rotates the bits of the specified <see cref="ulong"/> value to the right by the specified number of positions.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be rotated.</param>
    /// <param name="count">
    /// The number of positions to rotate. Must be in the range [0, 64]. A count of 0 or 64 is a no-op.
    /// </param>
    /// <returns>
    /// A <see cref="ulong"/> with the bits of <paramref name="value"/> rotated right by <paramref name="count"/> positions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is less than 0 or greater than 64.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateBitsRight(this ulong value, int count)
    {
        ThrowHelper.ThrowIfOutOfRange(count, 0, 64);
        return (value >> count) | (value << (64 - count));
    }
}
