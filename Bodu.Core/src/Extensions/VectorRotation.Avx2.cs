// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorRotation.Avx2.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using X86 = System.Runtime.Intrinsics.X86;

namespace Bodu.Extensions;

internal static partial class VectorRotation
{
    /// <summary>
    /// Rotates the lanes of 256-bit vectors with AVX2: 32-bit lanes by 8 or 16 bits, and 64-bit lanes by any multiple
    /// of 8 bits, as one in-lane shuffle, and by any other count as a pair of shifts.
    /// </summary>
    internal readonly struct Avx2
        : IVector256Rotation
    {
        /// <summary>
        /// Rotates every 32-bit lane of a vector left by a number of bits: by 8 or 16 bits with an in-lane byte
        /// shuffle, otherwise with a pair of shifts.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <para>
        /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated. Because it is a constant,
        /// the choice between the forms folds away when the call is inlined. The byte shuffle works within each 128-bit
        /// half, so its pattern repeats.
        /// </para>
        /// <para>
        /// The shifts are the portable operators, not <c>Avx2.ShiftLeftLogical</c> and <c>Avx2.ShiftRightLogical</c>.
        /// .NET 10 compiles either form to immediate shifts. .NET 8 does not fold <c>(byte)(32 - count)</c> into the
        /// intrinsic's immediate: it passes the count from memory, and the ChaCha20 and Salsa20 kernels built on that
        /// form spill more and run slower than on the portable operators, which move the count into a register.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) => count switch
        {
            16 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13, 2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13)).AsUInt32(),
            8 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14, 3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14)).AsUInt32(),
            _ => Vector256.ShiftLeft(value, count) | Vector256.ShiftRightLogical(value, 32 - count),
        };

        /// <summary>
        /// Rotates every 64-bit lane of a vector left by a number of bits: by 32 bits with a doubleword shuffle, by any
        /// other multiple of 8 bits with an in-lane byte shuffle, otherwise with a pair of shifts.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <para>
        /// <paramref name="count" /> must be a constant from 1 to 63; it is not validated. Because it is a constant,
        /// the choice between the forms folds away when the call is inlined. The shuffles work within each 128-bit
        /// half, so their patterns repeat.
        /// </para>
        /// <para>
        /// The shifts are the portable operators, as for 32-bit lanes. .NET 10 compiles them to immediate shifts, and
        /// the instruction set's intrinsics measured no faster on .NET 8.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateLeft(Vector256<ulong> value, [ConstantExpected(Min = 1, Max = 63)] byte count) => count switch
        {
            8 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)7, 0, 1, 2, 3, 4, 5, 6, 15, 8, 9, 10, 11, 12, 13, 14, 7, 0, 1, 2, 3, 4, 5, 6, 15, 8, 9, 10, 11, 12, 13, 14)).AsUInt64(),
            16 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)6, 7, 0, 1, 2, 3, 4, 5, 14, 15, 8, 9, 10, 11, 12, 13, 6, 7, 0, 1, 2, 3, 4, 5, 14, 15, 8, 9, 10, 11, 12, 13)).AsUInt64(),
            24 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)5, 6, 7, 0, 1, 2, 3, 4, 13, 14, 15, 8, 9, 10, 11, 12, 5, 6, 7, 0, 1, 2, 3, 4, 13, 14, 15, 8, 9, 10, 11, 12)).AsUInt64(),
            32 => X86.Avx2.Shuffle(value.AsUInt32(), 0b10_11_00_01).AsUInt64(),
            40 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10, 3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64(),
            48 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9, 2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64(),
            56 => X86.Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)1, 2, 3, 4, 5, 6, 7, 0, 9, 10, 11, 12, 13, 14, 15, 8, 1, 2, 3, 4, 5, 6, 7, 0, 9, 10, 11, 12, 13, 14, 15, 8)).AsUInt64(),
            _ => Vector256.ShiftLeft(value, count) | Vector256.ShiftRightLogical(value, 64 - count),
        };
    }
}
