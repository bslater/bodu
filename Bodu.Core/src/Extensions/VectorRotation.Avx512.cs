// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VectorRotation.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Extensions;

internal static partial class VectorRotation
{
    /// <summary>
    /// Rotates the lanes of 128-bit, 256-bit and 512-bit vectors with AVX-512, whose rotate instruction makes every
    /// rotation one instruction.
    /// </summary>
    /// <remarks>
    /// The 512-bit rotation requires AVX-512F; the 128-bit and 256-bit rotations also require AVX-512VL.
    /// </remarks>
    internal readonly struct Avx512
        : IVector128Rotation, IVector256Rotation, IVector512Rotation
    {
        /// <summary>
        /// Rotates every 32-bit lane of a 128-bit vector left by a number of bits, with one AVX-512VL rotate
        /// instruction.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) =>
            Avx512F.VL.RotateLeft(value, count);

        /// <summary>
        /// Rotates every 32-bit lane of a 256-bit vector left by a number of bits, with one AVX-512VL rotate
        /// instruction.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) =>
            Avx512F.VL.RotateLeft(value, count);

        /// <summary>
        /// Rotates every 64-bit lane of a 256-bit vector left by a number of bits, with one AVX-512VL rotate
        /// instruction.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <paramref name="count" /> must be a constant from 1 to 63; it is not validated.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateLeft(Vector256<ulong> value, [ConstantExpected(Min = 1, Max = 63)] byte count) =>
            Avx512F.VL.RotateLeft(value, count);

        /// <summary>
        /// Rotates every 32-bit lane of a 512-bit vector left by a number of bits, with one AVX-512F rotate
        /// instruction.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits to rotate each lane by.</param>
        /// <returns>The rotated lanes.</returns>
        /// <remarks>
        /// <paramref name="count" /> must be a constant from 1 to 31; it is not validated.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<uint> RotateLeft(Vector512<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count) =>
            Avx512F.RotateLeft(value, count);
    }
}
