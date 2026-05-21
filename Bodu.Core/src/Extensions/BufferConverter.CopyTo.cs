// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Extensions;

public static partial class BufferConverter
{
    /// <summary>
    /// Copies a specified number of elements of type <typeparamref name="T" /> from a byte array into a target array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy to.</typeparam>
    /// <param name="sourceArray">The source byte array.</param>
    /// <param name="sourceIndex">The starting index in the <paramref name="sourceArray" />.</param>
    /// <param name="targetArray">The target array to receive the elements.</param>
    /// <param name="targetIndex">The starting index in the <paramref name="targetArray" />.</param>
    /// <param name="count">The number of elements of type <typeparamref name="T" /> to copy.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="sourceArray" /> or <paramref name="targetArray" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sourceIndex" />, <paramref name="targetIndex" />, or <paramref name="count" /> is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The specified range exceeds the bounds of the source or target arrays.
    /// </exception>
    /// <remarks>
    /// The method assumes that the byte array represents elements of type <typeparamref name="T" /> using
    /// platform-native endianness.
    /// </remarks>
    public static void CopyTo<T>(this byte[] sourceArray, int sourceIndex, T[] targetArray, int targetIndex, int count)
        where T : unmanaged
    {
        ThrowHelper.ThrowIfNull(sourceArray);
        ThrowHelper.ThrowIfNull(targetArray);

#if NET5_0_OR_GREATER
        var elementSize = Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        var byteCount = count * elementSize;

        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(sourceArray, sourceIndex, byteCount);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(targetArray, targetIndex, byteCount);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        var sourceSpan = new ReadOnlySpan<byte>(sourceArray, sourceIndex, byteCount);
        var targetSpan = new Span<T>(targetArray, targetIndex, count);
        MemoryMarshal.Cast<byte, T>(sourceSpan).CopyTo(targetSpan);
#else
        var handle = GCHandle.Alloc(targetArray, GCHandleType.Pinned);
        try
        {
            IntPtr destPtr = handle.AddrOfPinnedObject() + (targetIndex * elementSize);
            Marshal.Copy(sourceArray, sourceIndex, destPtr, byteCount);
        }
        finally
        {
            handle.Free();
        }
#endif
    }

    /// <summary>
    /// Copies a specified number of elements of type <typeparamref name="T" /> from a byte array into a target array,
    /// starting at index zero in the target array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy to.</typeparam>
    /// <param name="sourceArray">The source byte array.</param>
    /// <param name="sourceIndex">The starting index in the <paramref name="sourceArray" />.</param>
    /// <param name="targetArray">The target array to receive the elements.</param>
    /// <param name="count">The number of elements of type <typeparamref name="T" /> to copy.</param>
    public static void CopyTo<T>(this byte[] sourceArray, int sourceIndex, T[] targetArray, int count)
            where T : unmanaged => CopyTo(sourceArray, sourceIndex, targetArray, 0, count);

#if !NETSTANDARD2_0

    /// <summary>
    /// Copies a specified number of elements of type <typeparamref name="T" /> from a source span into a target span.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy to.</typeparam>
    /// <param name="sourceSpan">The source span of bytes.</param>
    /// <param name="targetSpan">The target span of <typeparamref name="T" /> elements.</param>
    /// <param name="count">The number of elements to copy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The specified <paramref name="count" /> exceeds the available elements in the source or target span.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a span or array argument does not meet a length or offset precondition.
    /// </exception>
    public static void CopyTo<T>(this ReadOnlySpan<byte> sourceSpan, Span<T> targetSpan, int count)
        where T : unmanaged
    {
#if NET5_0_OR_GREATER
        var elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        var byteCount = count * elementSize;

        // ThrowHelper will handle range and size checks
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(sourceSpan, 0, byteCount);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(targetSpan, 0, count);

        MemoryMarshal.Cast<byte, T>(sourceSpan[..byteCount]).CopyTo(targetSpan[..count]);
    }

    /// <summary>
    /// Copies a specified number of elements of type <typeparamref name="T" /> from a memory region of bytes into a
    /// memory region of <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy to.</typeparam>
    /// <param name="sourceMemory">The source memory of bytes.</param>
    /// <param name="targetMemory">The target memory of <typeparamref name="T" /> elements.</param>
    /// <param name="count">The number of elements to copy.</param>
    public static void CopyTo<T>(this Memory<byte> sourceMemory, Memory<T> targetMemory, int count)
        where T : unmanaged => CopyTo(sourceMemory.Span, targetMemory.Span, count);

#endif

    /// <summary>
    /// Copies a single value of type <typeparamref name="T" /> into a byte array at the specified index.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to copy.</typeparam>
    /// <param name="value">The value to copy.</param>
    /// <param name="targetArray">The byte array to receive the value.</param>
    /// <param name="index">The starting index in the <paramref name="targetArray" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="targetArray" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or exceeds the available space in <paramref name="targetArray" />.
    /// </exception>
    public static void CopyTo<T>(this T value, byte[] targetArray, int index)
        where T : unmanaged
    {
        ThrowHelper.ThrowIfNull(targetArray);

#if NET5_0_OR_GREATER
        var elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(targetArray, index, elementSize);

#if NETSTANDARD2_0

        // Use GCHandle to write without Span
        var handle = GCHandle.Alloc(targetArray, GCHandleType.Pinned);
        try
        {
            IntPtr destPtr = handle.AddrOfPinnedObject() + index;
            Marshal.StructureToPtr(value, destPtr, false);
        }
        finally
        {
            handle.Free();
        }
#else
        Span<byte> target = targetArray.AsSpan(index, elementSize);
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(target, in value); // preferred overload in .NET 8+
#else
        MemoryMarshal.Write(target, ref value); // for .NET 5–7
#endif
#endif
    }
}
