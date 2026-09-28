// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Avx2.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>
    /// Supplies the rotations with AVX2: by 16 and 8 bits a <c>VPSHUFB</c>, by 12 and 7 bits a pair of shifts.
    /// </summary>
    internal readonly struct Avx2Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits, exchanging its halves.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight16(Vector256<uint> value) =>
            Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13, 2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13)).AsUInt32();

        /// <summary>
        /// Rotates each word right by 12 bits, as a pair of shifts.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight12(Vector256<uint> value) =>
            Avx2.ShiftRightLogical(value, 12) | Avx2.ShiftLeftLogical(value, 20);

        /// <summary>
        /// Rotates each word right by 8 bits, moving its bytes down one place.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight8(Vector256<uint> value) =>
            Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)1, 2, 3, 0, 5, 6, 7, 4, 9, 10, 11, 8, 13, 14, 15, 12, 1, 2, 3, 0, 5, 6, 7, 4, 9, 10, 11, 8, 13, 14, 15, 12)).AsUInt32();

        /// <summary>
        /// Rotates each word right by 7 bits, as a pair of shifts.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight7(Vector256<uint> value) =>
            Avx2.ShiftRightLogical(value, 7) | Avx2.ShiftLeftLogical(value, 25);
    }
}
