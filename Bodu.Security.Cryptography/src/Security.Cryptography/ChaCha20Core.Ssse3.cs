// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Ssse3.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Implements the 128-bit kernel steps with SSSE3: rotations by whole bytes as byte shuffles, other rotations as a
    /// pair of shifts, and the transpose as SSE2 unpacks.
    /// </summary>
    internal readonly struct Ssse3Isa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits: by whole bytes a byte shuffle, otherwise a pair
        /// of shifts.
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
            16 => Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13)).AsUInt32(),
            8 => Ssse3.Shuffle(value.AsByte(), Vector128.Create((byte)3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14)).AsUInt32(),
            _ => Vector128.ShiftLeft(value, count) | Vector128.ShiftRightLogical(value, 32 - count),
        };

        /// <summary>
        /// Transposes four rows of four 32-bit words in place, so that row <c>i</c> becomes column <c>i</c>, with SSE2
        /// unpacks.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3) =>
            Blake2sCore.Ssse3Isa.Transpose(ref row0, ref row1, ref row2, ref row3);
    }
}
