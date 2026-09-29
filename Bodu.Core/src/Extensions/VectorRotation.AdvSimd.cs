// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorRotation.AdvSimd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Arm = System.Runtime.Intrinsics.Arm;

namespace Bodu.Extensions;

internal static partial class VectorRotation
{
    /// <summary>
    /// Rotates the lanes of 128-bit vectors with ARM64 AdvSimd: by 16 bits as a halfword reversal, by 8 bits as a table
    /// lookup, and by any other count as a shift and a shift-and-insert.
    /// </summary>
    internal readonly struct AdvSimd
        : IVector128Rotation
    {
        /// <summary>
        /// Rotates every 32-bit lane of a vector left by a number of bits: by 16 bits with a halfword reversal, by 8
        /// bits with a table lookup, otherwise with a shift and a shift-and-insert.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <para>
        /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated. Because it is a constant,
        /// the choice between the forms folds away when the call is inlined.
        /// </para>
        /// <para>
        /// The right shift is the portable operator, as in <see cref="Ssse3.RotateLeft" />: .NET 8 does not fold a
        /// computed count such as <c>(byte)(32 - count)</c> into an intrinsic's immediate.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) => count switch
        {
            16 => Arm.AdvSimd.ReverseElement16(value.AsInt32()).AsUInt32(),
            8 => Arm.AdvSimd.Arm64.VectorTableLookup(value.AsByte(), Vector128.Create((byte)3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14)).AsUInt32(),
            _ => Arm.AdvSimd.ShiftLeftAndInsert(Vector128.ShiftRightLogical(value, 32 - count), value, count),
        };
    }
}
