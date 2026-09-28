// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyHelper.Xor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class CryptographyHelper
{
    /// <summary>
    /// Writes the byte-wise exclusive OR of <paramref name="left" /> and <paramref name="right" /> into
    /// <paramref name="destination" />, 32 or 16 bytes at a time where the processor accelerates it.
    /// </summary>
    /// <param name="left">The first operand; its length sets how many bytes are combined.</param>
    /// <param name="right">The second operand.</param>
    /// <param name="destination">The span that receives the result.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="right" /> or <paramref name="destination" /> is shorter than <paramref name="left" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="destination" /> may be the same memory as either operand, which is how keystream is applied in
    /// place; any other overlap produces unspecified results. Every byte is read before the byte at the same offset is
    /// written, and each vector is loaded before it is stored.
    /// </para>
    /// <para>
    /// The vector width is chosen by <see cref="Vector256.IsHardwareAccelerated" /> rather than the library's SIMD
    /// switch: exclusive OR moves data without arithmetic, so the widths produce identical results, and none of them
    /// branches on or indexes by the data.
    /// </para>
    /// </remarks>
    internal static void Xor(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(right, left.Length);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, left.Length);

        int length = left.Length;
        ref byte x = ref MemoryMarshal.GetReference(left);
        ref byte y = ref MemoryMarshal.GetReference(right);
        ref byte z = ref MemoryMarshal.GetReference(destination);
        int offset = 0;

        if (Vector256.IsHardwareAccelerated)
        {
            for (; offset <= length - Vector256<byte>.Count; offset += Vector256<byte>.Count)
                (Vector256.LoadUnsafe(ref x, (nuint)offset) ^ Vector256.LoadUnsafe(ref y, (nuint)offset)).StoreUnsafe(ref z, (nuint)offset);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            for (; offset <= length - Vector128<byte>.Count; offset += Vector128<byte>.Count)
                (Vector128.LoadUnsafe(ref x, (nuint)offset) ^ Vector128.LoadUnsafe(ref y, (nuint)offset)).StoreUnsafe(ref z, (nuint)offset);
        }

        for (; offset <= length - sizeof(ulong); offset += sizeof(ulong))
        {
            ulong value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref x, offset)) ^ Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref y, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref z, offset), value);
        }

        for (; offset < length; offset++)
            Unsafe.Add(ref z, offset) = (byte)(Unsafe.Add(ref x, offset) ^ Unsafe.Add(ref y, offset));
    }
}
