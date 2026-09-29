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
    /// Rotates the lanes of 256-bit vectors with AVX2: by 8 or 16 bits as one in-lane byte shuffle, and by any other
    /// count as a pair of shifts.
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
        /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated. Because it is a constant,
        /// the choice between the forms folds away when the call is inlined. The byte shuffle works within each 128-bit
        /// half, so its pattern repeats.
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
    }
}
