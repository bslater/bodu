// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class BufferConverter
{
    /// <summary>
    /// Converts a specified number of elements of type <typeparamref name="T" /> from a byte array into a new array of
    /// type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy to.</typeparam>
    /// <param name="sourceArray">The source byte array.</param>
    /// <param name="sourceIndex">The starting index in the <paramref name="sourceArray" />.</param>
    /// <param name="count">The number of elements of type <typeparamref name="T" /> to copy.</param>
    /// <returns>A new array of <typeparamref name="T" /> elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceArray" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sourceIndex" /> or <paramref name="count" /> is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">The specified range exceeds the bounds of the source array.</exception>
    /// <remarks>
    /// The method assumes that the byte array represents elements of type <typeparamref name="T" /> using
    /// platform-native endianness.
    /// </remarks>
    public static T[] ToArray<T>(this byte[] sourceArray, int sourceIndex, int count)
        where T : unmanaged
    {
        ThrowHelper.ThrowIfNull(sourceArray);
#if NET5_0_OR_GREATER
        int elementSize = Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(sourceArray, sourceIndex, count * elementSize);

        var result = new T[count];
        sourceArray.CopyTo(sourceIndex, result, 0, count);
        return result;
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Converts a specified number of elements of type <typeparamref name="T" /> from a span of bytes into a new array
    /// of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy to.</typeparam>
    /// <param name="sourceSpan">The source span of bytes.</param>
    /// <param name="count">The number of elements to copy.</param>
    /// <returns>A new array of <typeparamref name="T" /> elements.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count" /> is negative or exceeds the available bytes.
    /// </exception>
    /// <remarks>
    /// The method assumes that the span represents elements of type <typeparamref name="T" /> using platform-native
    /// endianness.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when a span or array argument does not meet a length or offset precondition.
    /// </exception>
    public static T[] ToArray<T>(this ReadOnlySpan<byte> sourceSpan, int count)
        where T : unmanaged
    {
#if NET5_0_OR_GREATER
        int elementSize = Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(sourceSpan, 0, count * elementSize);

        var result = new T[count];
        sourceSpan.CopyTo(result.AsSpan(), count);
        return result;
    }

#endif
}
