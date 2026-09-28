// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Avx2.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2bCore
{
    /// <summary>
    /// Supplies the rotations with AVX2, as Argon2's AVX2 kernel does: by 32 bits a <c>VPSHUFD</c>, by 24 and 16 bits a
    /// <c>VPSHUFB</c>, and by 63 bits an addition and a shift.
    /// </summary>
    internal readonly struct Avx2Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 32 bits, swapping its halves.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsUInt32(), 0b10_11_00_01).AsUInt64();

        /// <summary>
        /// Rotates each word right by 24 bits, moving its bytes down three places.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10, 3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64();

        /// <summary>
        /// Rotates each word right by 16 bits, moving its bytes down two places.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9, 2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64();

        /// <summary>
        /// Rotates each word right by 63 bits: its double, with its top bit carried round to the bottom.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx2.Add(value, value) ^ Avx2.ShiftRightLogical(value, 63);
    }
}
