// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverter.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Extensions;

public static partial class BufferConverter
{
    /// <summary>
    /// Reads a single element of type <typeparamref name="T" /> from a byte array at a specified index.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to read.</typeparam>
    /// <param name="sourceArray">The source byte array.</param>
    /// <param name="index">The starting index in the <paramref name="sourceArray" />.</param>
    /// <returns>The value of type <typeparamref name="T" /> read from the specified location.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceArray" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is out of range or insufficient bytes remain.
    /// </exception>
    /// <remarks>
    /// The method assumes that the byte array represents the value using platform-native endianness.
    /// </remarks>
    public static T Read<T>(this byte[] sourceArray, int index)
        where T : unmanaged
    {
        ThrowHelper.ThrowIfNull(sourceArray);
#if NET5_0_OR_GREATER
        var elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(sourceArray, index, elementSize);

#if NETSTANDARD2_0
        var handle = GCHandle.Alloc(sourceArray, GCHandleType.Pinned);
        try
        {
            IntPtr sourcePtr = handle.AddrOfPinnedObject() + index;
            return Marshal.PtrToStructure<T>(sourcePtr);
        }
        finally
        {
            handle.Free();
        }
#else
        return System.Runtime.InteropServices.MemoryMarshal.Read<T>(sourceArray.AsSpan(index, elementSize));
#endif
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Reads a single element of type <typeparamref name="T" /> from a span of bytes.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to read.</typeparam>
    /// <param name="sourceSpan">The source span of bytes.</param>
    /// <returns>The value of type <typeparamref name="T" /> read from the span.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The span length is insufficient to represent a value of type <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// The method assumes that the span represents the value using platform-native endianness.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when a span or array argument does not meet a length or offset precondition.
    /// </exception>
    public static T Read<T>(this ReadOnlySpan<byte> sourceSpan)
        where T : unmanaged
    {
#if NET5_0_OR_GREATER
        var elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
#else
        int elementSize = Marshal.SizeOf<T>();
#endif
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(sourceSpan, 0, elementSize);

        return MemoryMarshal.Read<T>(sourceSpan[..elementSize]);
    }

#endif
}
