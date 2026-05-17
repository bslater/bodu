// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.ReverseWords.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Swaps the two bytes of the specified <see cref="ushort" /> value.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose bytes are to be swapped.</param>
    /// <returns>
    /// A <see cref="ushort" /> whose high and low bytes are exchanged relative to <paramref name="value" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Because a <see cref="ushort" /> comprises a single 16-bit word, this operation is equivalent to
    /// <see cref="ReverseBytes(ushort)" />.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReverseWords(this ushort value) =>
        (ushort)((value >> 8) | (value << 8));

    /// <summary>
    /// Swaps the bytes within each 16-bit word of the specified <see cref="uint" /> value.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose per-word bytes are to be swapped.</param>
    /// <returns>
    /// A <see cref="uint" /> in which the two bytes of each 16-bit half have been exchanged. For example,
    /// <c>0xAABBCCDD</c> becomes <c>0xBBAADDCC</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method treats the value as two consecutive 16-bit words and reverses the byte order within each word
    /// independently. It differs from <see cref="ReverseBytes(uint)" />, which reverses the byte order of the entire
    /// 32-bit value.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseWords(this uint value) =>
        ((value & 0x00FF00FFU) << 8) | ((value & 0xFF00FF00U) >> 8);

    /// <summary>
    /// Swaps the bytes within each 16-bit word of the specified <see cref="ulong" /> value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose per-word bytes are to be swapped.</param>
    /// <returns>
    /// A <see cref="ulong" /> in which the two bytes of each 16-bit quarter have been exchanged. For example,
    /// <c>0xAABBCCDD11223344</c> becomes <c>0xBBAADDCC22114433</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method treats the value as four consecutive 16-bit words and reverses the byte order within each word
    /// independently. It differs from <see cref="ReverseBytes(ulong)" />, which reverses the byte order of the entire
    /// 64-bit value.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseWords(this ulong value) =>
        ((value & 0x00FF00FF00FF00FFUL) << 8) | ((value & 0xFF00FF00FF00FF00UL) >> 8);

    /// <summary>
    /// Swaps the bytes within each 16-bit word of the specified byte array and returns a new array.
    /// </summary>
    /// <param name="bytes">The byte array to process. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new byte array in which each pair of adjacent bytes has been exchanged. The original array is not modified.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The array is processed in consecutive pairs. Each even-indexed byte is exchanged with the byte immediately
    /// following it, leaving all other bytes in place.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// The length of <paramref name="bytes" /> is not a positive multiple of 2.
    /// </exception>
    public static byte[] ReverseWords(this byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);
        ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(bytes, 2);

        var result = (byte[])bytes.Clone();

        for (var i = 0; i < result.Length; i += 2)
        {
            var temp = result[i];
            result[i] = result[i + 1];
            result[i + 1] = temp;
        }

        return result;
    }

    /// <summary>
    /// Swaps the bytes within each 16-bit word of the specified <see cref="Span{T}" />, modifying it in place.
    /// </summary>
    /// <param name="bytes">The byte span to process in place.</param>
    /// <remarks>
    /// <para>
    /// The span is processed in consecutive pairs. Each even-indexed byte is exchanged with the byte immediately
    /// following it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The length of <paramref name="bytes" /> is not a positive multiple of 2.
    /// </exception>
    public static void ReverseWords(this Span<byte> bytes)
    {
        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(bytes, 2);

        for (var i = 0; i < bytes.Length; i += 2)
        {
            var temp = bytes[i];
            bytes[i] = bytes[i + 1];
            bytes[i + 1] = temp;
        }
    }
}
