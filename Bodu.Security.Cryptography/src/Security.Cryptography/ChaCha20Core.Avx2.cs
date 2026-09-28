// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Avx2.cs" company="Bodu Pty. Ltd.">
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
    /// Implements the 256-bit kernel step with AVX2: rotations by whole bytes as in-lane byte shuffles, other rotations
    /// as a pair of shifts.
    /// </summary>
    internal readonly struct Avx2Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits: by whole bytes an in-lane byte shuffle,
        /// otherwise a pair of shifts.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, a constant from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// With a constant <paramref name="count" />, the choice between the forms folds away when the call is inlined.
        /// The byte shuffle works within each 128-bit lane, so its pattern repeats.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) => count switch
        {
            16 => Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13, 2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9, 14, 15, 12, 13)).AsUInt32(),
            8 => Avx2.Shuffle(
                value.AsByte(),
                Vector256.Create((byte)3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14, 3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10, 15, 12, 13, 14)).AsUInt32(),
            _ => Vector256.ShiftLeft(value, count) | Vector256.ShiftRightLogical(value, 32 - count),
        };
    }
}
