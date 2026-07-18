// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.SwapEndian.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Extensions;

public static partial class BufferConverter
{
    /// <summary>
    /// Swaps the byte order (endianness) of each element of type <typeparamref name="T" /> in the specified span.
    /// </summary>
    /// <typeparam name="T">The unmanaged type whose byte order will be swapped.</typeparam>
    /// <param name="span">The span containing elements to swap in place.</param>
    /// <remarks>
    /// This method reverses the byte order of each element in the span individually, performing the operation in place
    /// without allocating memory. No operation is performed for types with a size of one byte or less. The swap assumes
    /// platform-native layout of <typeparamref name="T" />. The operation is safe for empty spans and spans containing
    /// only one element.
    /// </remarks>
    public static void SwapEndian<T>(this Span<T> span)
        where T : unmanaged
    {
        int size = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        if (size <= 1)
            return; // No need to swap single-byte types

        Span<byte> bytes = MemoryMarshal.AsBytes(span);
        for (int i = 0; i < bytes.Length; i += size)
        {
            int left = 0;
            int right = size - 1;
            while (left < right)
            {
                (bytes[i + left], bytes[i + right]) = (bytes[i + right], bytes[i + left]);
                left++;
                right--;
            }
        }
    }

    /// <summary>
    /// Copies data from a source span to a destination span, swapping the byte order (endianness) of each element of
    /// size <paramref name="elementSize" /> bytes.
    /// </summary>
    /// <param name="source">The source span of bytes.</param>
    /// <param name="destination">The destination span of bytes to receive the endian-swapped elements.</param>
    /// <param name="elementSize">The size, in bytes, of each element whose byte order should be swapped.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="elementSize" /> is less than or equal to 1.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the source or destination spans do not have a length that is a multiple of
    /// <paramref name="elementSize" />, or if the destination is too small to receive the swapped data.
    /// </exception>
    /// <remarks>
    /// This method reverses the byte order of each element individually while copying from <paramref name="source" />
    /// to <paramref name="destination" />. It assumes platform-native layout and performs the swap without allocating
    /// additional memory.
    /// </remarks>
    public static void SwapEndian(ReadOnlySpan<byte> source, Span<byte> destination, int elementSize)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(elementSize, 1);
        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(source, elementSize);
        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(destination, elementSize);

        if (destination.Length < source.Length)
            ThrowHelper.ThrowIfCountExceedsAvailable(source.Length, destination.Length);

        for (int i = 0; i < source.Length; i += elementSize)
        {
            for (int left = 0, right = elementSize - 1; left <= right; left++, right--)
            {
                // Read both source values before writing so that the swap is correct even when
                // <paramref name="source" /> and <paramref name="destination" /> refer to the same memory.
                (destination[i + left], destination[i + right]) = (source[i + right], source[i + left]);
            }
        }
    }
}
