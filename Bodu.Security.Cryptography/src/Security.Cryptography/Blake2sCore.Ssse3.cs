// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.Ssse3.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// Supplies the rotations with SSSE3 — by 16 and 8 bits a <c>PSHUFB</c>, by 12 and 7 bits a pair of shifts — and
    /// the lane rotations with <c>PSHUFD</c>.
    /// </summary>
    internal readonly struct Ssse3Isa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits, exchanging its halves.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight16(Vector128<uint> value) =>
            Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13)).AsUInt32();

        /// <summary>
        /// Rotates each word right by 12 bits, as a pair of shifts.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight12(Vector128<uint> value) =>
            Sse2.ShiftRightLogical(value, 12) | Sse2.ShiftLeftLogical(value, 20);

        /// <summary>
        /// Rotates each word right by 8 bits, moving its bytes down one place.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight8(Vector128<uint> value) =>
            Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)1, 2, 3, 0, 5, 6, 7, 4, 9, 10, 11, 8, 13, 14, 15, 12)).AsUInt32();

        /// <summary>
        /// Rotates each word right by 7 bits, as a pair of shifts.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight7(Vector128<uint> value) =>
            Sse2.ShiftRightLogical(value, 7) | Sse2.ShiftLeftLogical(value, 25);

        /// <summary>
        /// Rotates the four lanes one place toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes1(Vector128<uint> value) =>
            Sse2.Shuffle(value, 0b00_11_10_01);

        /// <summary>
        /// Rotates the four lanes two places.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes2(Vector128<uint> value) =>
            Sse2.Shuffle(value, 0b01_00_11_10);

        /// <summary>
        /// Rotates the four lanes three places toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes3(Vector128<uint> value) =>
            Sse2.Shuffle(value, 0b10_01_00_11);
    }
}
