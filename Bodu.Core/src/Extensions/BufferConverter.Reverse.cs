// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.Reverse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class BufferConverter
{
#if NETSTANDARD2_0

    /// <summary>
    /// Reverses the elements in a byte array in place.
    /// </summary>
    /// <param name="array">The byte array to reverse.</param>
    /// <remarks>This method reverses the order of bytes within the array without creating a copy.</remarks>
    public static void Reverse(this byte[] array)
    {
        if (array == null || array.Length <= 1)
            return;

        int i = 0;
        int j = array.Length - 1;
        while (i < j)
        {
            byte tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
            i++;
            j--;
        }
    }

    /// <summary>
    /// Reverses the elements in an array of unmanaged elements in place.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the array elements.</typeparam>
    /// <param name="array">The array to reverse.</param>
    /// <remarks>This method reverses the order of elements in the array in place. It does not allocate memory.</remarks>
    public static void Reverse<T>(this T[] array)
        where T : unmanaged
    {
        if (array == null || array.Length <= 1)
            return;

        int i = 0;
        int j = array.Length - 1;
        while (i < j)
        {
            T tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
            i++;
            j--;
        }
    }

#else

    /// <summary>
    /// Reverses the elements in a span of bytes in place.
    /// </summary>
    /// <param name="span">The span of bytes to reverse.</param>
    /// <remarks>
    /// This method reverses the order of bytes within the span without creating a copy. No exception is thrown for empty or
    /// single-element spans; they are treated as no-op.
    /// </remarks>
    public static void Reverse(this Span<byte> span)
    {
        if (span.Length <= 1)
            return;

        int i = 0;
        int j = span.Length - 1;
        while (i < j)
        {
            (span[i], span[j]) = (span[j], span[i]);
            i++;
            j--;
        }
    }

    /// <summary>
    /// Reverses the elements of type <typeparamref name="T" /> in a span in place.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of elements to reverse.</typeparam>
    /// <param name="span">The span containing elements to reverse.</param>
    /// <remarks>
    /// This method reverses the order of elements within the span without allocating additional memory. It operates directly on the
    /// provided memory and assumes platform-native layout for <typeparamref name="T" />. No exception is thrown for empty or
    /// single-element spans; they are treated as no-op.
    /// </remarks>
    public static void Reverse<T>(this Span<T> span)
        where T : unmanaged
    {
        if (span.Length <= 1)
            return;

        int i = 0;
        int j = span.Length - 1;
        while (i < j)
        {
            (span[i], span[j]) = (span[j], span[i]);
            i++;
            j--;
        }
    }

    /// <summary>
    /// Reverses the elements in a memory block of bytes in place.
    /// </summary>
    /// <param name="memory">The memory block of bytes to reverse.</param>
    /// <remarks>
    /// This method reverses the order of bytes within the memory block without creating a copy. The operation is performed directly on
    /// the underlying span.
    /// </remarks>
    public static void Reverse(this Memory<byte> memory) => memory.Span.Reverse();

    /// <summary>
    /// Reverses the elements in a memory block of unmanaged type <typeparamref name="T" /> in place.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to reverse.</typeparam>
    /// <param name="memory">The memory block to reverse.</param>
    /// <remarks>
    /// This method reverses the order of elements within the memory block without creating a copy. The operation is performed directly
    /// on the underlying span.
    /// </remarks>
    public static void Reverse<T>(this Memory<T> memory)
        where T : unmanaged => memory.Span.Reverse();

#endif
}