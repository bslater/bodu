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
    /// Supplies the rotations with SSSE3 - by 16 and 8 bits a <c>PSHUFB</c>, by 12 and 7 bits a pair of shifts - the
    /// lane rotations with <c>PSHUFD</c>, and the transpose with SSE2's unpacks.
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

        /// <summary>
        /// Transposes four rows of four words, interleaving words and then word pairs with <c>PUNPCK</c>.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3)
        {
            Vector128<ulong> low01 = Sse2.UnpackLow(row0, row1).AsUInt64();
            Vector128<ulong> high01 = Sse2.UnpackHigh(row0, row1).AsUInt64();
            Vector128<ulong> low23 = Sse2.UnpackLow(row2, row3).AsUInt64();
            Vector128<ulong> high23 = Sse2.UnpackHigh(row2, row3).AsUInt64();

            row0 = Sse2.UnpackLow(low01, low23).AsUInt32();
            row1 = Sse2.UnpackHigh(low01, low23).AsUInt32();
            row2 = Sse2.UnpackLow(high01, high23).AsUInt32();
            row3 = Sse2.UnpackHigh(high01, high23).AsUInt32();
        }
    }
}
