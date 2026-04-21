// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.ReverseBits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Reverses the order of all bits in the specified <see cref="byte"/> value.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value whose bits are to be reversed.</param>
    /// <returns>
    /// A <see cref="byte"/> whose bits appear in the reverse order of those in <paramref name="value"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReverseBits(this byte value)
    {
        value = (byte)(((value >> 1) & 0x55) | ((value & 0x55) << 1));
        value = (byte)(((value >> 2) & 0x33) | ((value & 0x33) << 2));
        value = (byte)((value >> 4) | (value << 4));
        return value;
    }

    /// <summary>
    /// Reverses the order of all bits in the specified <see cref="ushort"/> value.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bits are to be reversed.</param>
    /// <returns>
    /// A <see cref="ushort"/> whose bits appear in the reverse order of those in <paramref name="value"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReverseBits(this ushort value)
    {
        value = (ushort)(((value >> 1) & 0x5555) | ((value & 0x5555) << 1));
        value = (ushort)(((value >> 2) & 0x3333) | ((value & 0x3333) << 2));
        value = (ushort)(((value >> 4) & 0x0F0F) | ((value & 0x0F0F) << 4));
        value = (ushort)((value >> 8) | (value << 8));
        return value;
    }

    /// <summary>
    /// Reverses the order of all bits in the specified <see cref="uint"/> value.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose bits are to be reversed.</param>
    /// <returns>
    /// A <see cref="uint"/> whose bits appear in the reverse order of those in <paramref name="value"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseBits(this uint value)
    {
        value = ((value >> 1) & 0x55555555) | ((value & 0x55555555) << 1);
        value = ((value >> 2) & 0x33333333) | ((value & 0x33333333) << 2);
        value = ((value >> 4) & 0x0F0F0F0F) | ((value & 0x0F0F0F0F) << 4);
        value = ((value >> 8) & 0x00FF00FF) | ((value & 0x00FF00FF) << 8);
        value = (value >> 16) | (value << 16);
        return value;
    }

    /// <summary>
    /// Reverses the order of all bits in the specified <see cref="ulong"/> value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose bits are to be reversed.</param>
    /// <returns>
    /// A <see cref="ulong"/> whose bits appear in the reverse order of those in <paramref name="value"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseBits(this ulong value)
    {
        value = ((value >> 1) & 0x5555555555555555UL) | ((value & 0x5555555555555555UL) << 1);
        value = ((value >> 2) & 0x3333333333333333UL) | ((value & 0x3333333333333333UL) << 2);
        value = ((value >> 4) & 0x0F0F0F0F0F0F0F0FUL) | ((value & 0x0F0F0F0F0F0F0F0FUL) << 4);
        value = ((value >> 8) & 0x00FF00FF00FF00FFUL) | ((value & 0x00FF00FF00FF00FFUL) << 8);
        value = ((value >> 16) & 0x0000FFFF0000FFFFUL) | ((value & 0x0000FFFF0000FFFFUL) << 16);
        value = (value >> 32) | (value << 32);
        return value;
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength"/> bits in the specified
    /// <see cref="byte"/> value.
    /// </summary>
    /// <param name="value">The 8-bit unsigned value to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. Must be in the range [0, 8]. Bits above the
    /// reflected window are discarded.
    /// </param>
    /// <returns>
    /// A <see cref="byte"/> whose least significant <paramref name="bitLength"/> bits contain the
    /// bit-reversed window of <paramref name="value"/>, with all higher bits set to zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bitLength"/> is less than 0 or greater than 8.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReverseBits(this byte value, int bitLength)
    {
        ThrowHelper.ThrowIfOutOfRange(bitLength, 0, 8);
        return value.ReverseBitsUnchecked(bitLength);
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength"/> bits in the specified
    /// <see cref="ushort"/> value.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. Must be in the range [0, 16]. Bits above the
    /// reflected window are discarded.
    /// </param>
    /// <returns>
    /// A <see cref="ushort"/> whose least significant <paramref name="bitLength"/> bits contain the
    /// bit-reversed window of <paramref name="value"/>, with all higher bits set to zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bitLength"/> is less than 0 or greater than 16.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReverseBits(this ushort value, int bitLength)
    {
        ThrowHelper.ThrowIfOutOfRange(bitLength, 0, 16);
        return value.ReverseBitsUnchecked(bitLength);
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength"/> bits in the specified
    /// <see cref="uint"/> value.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. Must be in the range [0, 32]. Bits above the
    /// reflected window are discarded.
    /// </param>
    /// <returns>
    /// A <see cref="uint"/> whose least significant <paramref name="bitLength"/> bits contain the
    /// bit-reversed window of <paramref name="value"/>, with all higher bits set to zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bitLength"/> is less than 0 or greater than 32.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseBits(this uint value, int bitLength)
    {
        ThrowHelper.ThrowIfOutOfRange(bitLength, 0, 32);
        return value.ReverseBitsUnchecked(bitLength);
    }

    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength"/> bits in the specified
    /// <see cref="ulong"/> value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">
    /// The number of least significant bits to reverse. Must be in the range [0, 64]. Bits above the
    /// reflected window are discarded.
    /// </param>
    /// <returns>
    /// A <see cref="ulong"/> whose least significant <paramref name="bitLength"/> bits contain the
    /// bit-reversed window of <paramref name="value"/>, with all higher bits set to zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bitLength"/> is less than 0 or greater than 64.
    /// </exception>
    /// <remarks>
    /// Commonly used by CRC and other checksum primitives to implement the <c>reflect-in</c> /
    /// <c>reflect-out</c> transformations where <paramref name="bitLength"/> matches the hash width.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseBits(this ulong value, int bitLength)
    {
        ThrowHelper.ThrowIfOutOfRange(bitLength, 0, 64);
        return value.ReverseBitsUnchecked(bitLength);
    }

    /// <summary>
    /// Reverses the bits within each byte of the specified byte array and returns a new array.
    /// </summary>
    /// <param name="bytes">The byte array to process. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// A new byte array in which the bits of each element have been reversed. The original array
    /// is not modified.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each byte is reversed independently using <see cref="ReverseBits(byte)"/>. The relative
    /// order of bytes within the array is preserved.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="bytes"/> is <see langword="null"/>.
    /// </exception>
    public static byte[] ReverseBits(this byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);

        byte[] result = new byte[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
            result[i] = bytes[i].ReverseBits();

        return result;
    }
}
