// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.SwapEndian.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Extensions;

public static partial class BufferConverter
{
#if NETSTANDARD2_0

    /// <summary>
    /// Swaps the byte order (endianness) of each element of type <typeparamref name="T" /> in the specified array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type whose byte order will be swapped.</typeparam>
    /// <param name="array">The array containing elements to swap in place.</param>
    /// <remarks>
    /// Each element in the array is interpreted as a block of bytes and reversed individually. No operation is performed if the type
    /// has a size of one byte or less.
    /// </remarks>
    public static void SwapEndian<T>(this T[] array)
        where T : unmanaged
    {
        if (array == null || array.Length == 0)
            return;

        int size = Marshal.SizeOf<T>();
        if (size <= 1)
            return;

        var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        try
        {
            IntPtr basePtr = handle.AddrOfPinnedObject();
            byte[] temp = new byte[size];

            for (int index = 0; index < array.Length; index++)
            {
                IntPtr elementPtr = basePtr + (index * size);
                Marshal.Copy(elementPtr, temp, 0, size);
                Array.Reverse(temp);
                Marshal.Copy(temp, 0, elementPtr, size);
            }
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Swaps the byte order (endianness) of each element of size <paramref name="elementSize" /> bytes while copying from a source byte
    /// array to a destination byte array.
    /// </summary>
    /// <param name="source">The source byte array.</param>
    /// <param name="destination">The destination byte array.</param>
    /// <param name="elementSize">The size of each element in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="elementSize" /> is &lt;= 1.</exception>
    /// <exception cref="ArgumentException">Thrown if array lengths are not multiples of <paramref name="elementSize" />.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <see langword="null" />.</exception>
    public static void SwapEndian(byte[] source, byte[] destination, int elementSize)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfLessThanOrEqual(elementSize, 1);
        ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(source, elementSize);
        ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(destination, elementSize);
        if (destination.Length < source.Length)
            ThrowHelper.ThrowIfCountExceedsAvailable(source.Length, destination.Length);

        for (int i = 0; i < source.Length; i += elementSize)
        {
            for (int j = 0; j < elementSize; j++)
            {
                destination[i + j] = source[i + elementSize - 1 - j];
            }
        }
    }
#else

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
#if NET5_0_OR_GREATER
        var size = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
#else
        int size = Marshal.SizeOf<T>();
#endif
        if (size <= 1)
            return; // No need to swap single-byte types

        Span<byte> bytes = MemoryMarshal.AsBytes(span);
        for (var i = 0; i < bytes.Length; i += size)
        {
            var left = 0;
            var right = size - 1;
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

        for (var i = 0; i < source.Length; i += elementSize)
        {
            for (int left = 0, right = elementSize - 1; left <= right; left++, right--)
            {
                // Read both source values before writing so that the swap is correct even when
                // <paramref name="source" /> and <paramref name="destination" /> refer to the same memory.
                (destination[i + left], destination[i + right]) = (source[i + right], source[i + left]);
            }
        }
    }

#endif
}
