// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Avx512.cs" company="Bodu Pty. Ltd.">
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
    /// Implements the 128-bit and 256-bit kernel steps with AVX-512VL, whose rotate instruction turns every rotation
    /// into one instruction; the 128-bit transpose is SSE2's.
    /// </summary>
    internal readonly struct Avx512Isa
        : IVector128Isa, IVector256Isa
    {
        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits: one AVX-512VL rotate instruction.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, a constant from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) =>
            Avx512F.VL.RotateLeft(value, count);

        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits: one AVX-512VL rotate instruction.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, a constant from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) =>
            Avx512F.VL.RotateLeft(value, count);

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
