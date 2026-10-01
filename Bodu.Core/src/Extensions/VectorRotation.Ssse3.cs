// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorRotation.Ssse3.cs" company="Bodu Pty. Ltd.">
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
    /// Rotates the lanes of 128-bit vectors with SSSE3: by 8 or 16 bits as one byte shuffle, and by any other count as
    /// a pair of shifts.
    /// </summary>
    internal readonly struct Ssse3
        : IVector128Rotation
    {
        /// <summary>
        /// Rotates every 32-bit lane of a vector left by a number of bits: by 8 or 16 bits with a byte shuffle,
        /// otherwise with a pair of shifts.
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
        /// The shifts are the portable operators, not <c>Sse2.ShiftLeftLogical</c> and <c>Sse2.ShiftRightLogical</c>.
        /// .NET 10 compiles either form to immediate shifts. .NET 8 does not fold <c>(byte)(32 - count)</c> into the
        /// intrinsic's immediate: it passes the count from memory, and the ChaCha20 and Salsa20 kernels built on that
        /// form spill more and run slower than on the portable operators, which move the count into a register.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) => count switch
        {
            16 => X86.Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13)).AsUInt32(),
            8 => X86.Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14)).AsUInt32(),
            _ => Vector128.ShiftLeft(value, count) | Vector128.ShiftRightLogical(value, 32 - count),
        };
    }
}
