// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.AdvSimd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// Supplies the rotations on ARM64 - by 16 bits a <c>REV32</c>, by 8 bits a <c>TBL</c>, by 12 and 7 bits a shift
    /// and a shift-and-insert - the lane rotations with <c>EXT</c>, and the transpose with <c>ZIP1</c> and <c>ZIP2</c>.
    /// </summary>
    internal readonly struct AdvSimdIsa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits, exchanging its halves.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight16(Vector128<uint> value) =>
            AdvSimd.ReverseElement16(value.AsInt32()).AsUInt32();

        /// <summary>
        /// Rotates each word right by 12 bits, as a shift and a shift-and-insert.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight12(Vector128<uint> value) =>
            AdvSimd.ShiftLeftAndInsert(AdvSimd.ShiftRightLogical(value, 12), value, 20);

        /// <summary>
        /// Rotates each word right by 8 bits, moving its bytes down one place.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight8(Vector128<uint> value) =>
            AdvSimd.Arm64.VectorTableLookup(value.AsByte(), Vector128.Create((byte)1, 2, 3, 0, 5, 6, 7, 4, 9, 10, 11, 8, 13, 14, 15, 12)).AsUInt32();

        /// <summary>
        /// Rotates each word right by 7 bits, as a shift and a shift-and-insert.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight7(Vector128<uint> value) =>
            AdvSimd.ShiftLeftAndInsert(AdvSimd.ShiftRightLogical(value, 7), value, 25);

        /// <summary>
        /// Rotates the four lanes one place toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes1(Vector128<uint> value) =>
            AdvSimd.ExtractVector128(value, value, 1);

        /// <summary>
        /// Rotates the four lanes two places.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes2(Vector128<uint> value) =>
            AdvSimd.ExtractVector128(value, value, 2);

        /// <summary>
        /// Rotates the four lanes three places toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes3(Vector128<uint> value) =>
            AdvSimd.ExtractVector128(value, value, 3);

        /// <summary>
        /// Transposes four rows of four words, interleaving words and then word pairs with <c>ZIP1</c> and <c>ZIP2</c>.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3)
        {
            Vector128<ulong> low01 = AdvSimd.Arm64.ZipLow(row0, row1).AsUInt64();
            Vector128<ulong> high01 = AdvSimd.Arm64.ZipHigh(row0, row1).AsUInt64();
            Vector128<ulong> low23 = AdvSimd.Arm64.ZipLow(row2, row3).AsUInt64();
            Vector128<ulong> high23 = AdvSimd.Arm64.ZipHigh(row2, row3).AsUInt64();

            row0 = AdvSimd.Arm64.ZipLow(low01, low23).AsUInt32();
            row1 = AdvSimd.Arm64.ZipHigh(low01, low23).AsUInt32();
            row2 = AdvSimd.Arm64.ZipLow(high01, high23).AsUInt32();
            row3 = AdvSimd.Arm64.ZipHigh(high01, high23).AsUInt32();
        }
    }
}
