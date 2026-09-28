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
    /// Supplies the rotations on ARM64 — by 16 bits a <c>REV32</c>, by 8 bits a <c>TBL</c>, by 12 and 7 bits a shift
    /// and a shift-and-insert — and the lane rotations with <c>EXT</c>.
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
    }
}
