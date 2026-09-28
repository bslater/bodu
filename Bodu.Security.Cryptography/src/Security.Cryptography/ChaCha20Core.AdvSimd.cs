// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.AdvSimd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Implements the 128-bit kernel steps with ARM64 AdvSimd: a rotation by 16 bits as a halfword reversal, by 8 bits
    /// as a table lookup, other rotations as a shift and a shift-and-insert, and the transpose as zips.
    /// </summary>
    internal readonly struct AdvSimdIsa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits: by 16 bits a halfword reversal, by 8 bits a
        /// table lookup, otherwise a shift and a shift-and-insert.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, a constant from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// With a constant <paramref name="count" />, the choice between the forms folds away when the call is inlined.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) => count switch
        {
            16 => AdvSimd.ReverseElement16(value.AsInt32()).AsUInt32(),
            8 => AdvSimd.Arm64.VectorTableLookup(value.AsByte(), Vector128.Create((byte)3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14)).AsUInt32(),
            _ => AdvSimd.ShiftLeftAndInsert(Vector128.ShiftRightLogical(value, 32 - count), value, count),
        };

        /// <summary>
        /// Transposes four rows of four 32-bit words in place, so that row <c>i</c> becomes column <c>i</c>, with zips.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3) =>
            Blake2sCore.AdvSimdIsa.Transpose(ref row0, ref row1, ref row2, ref row3);
    }
}
