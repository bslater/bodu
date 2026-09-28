// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Avx512Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2bCore
{
    /// <summary>
    /// Supplies the rotations with AVX-512VL, each a single <c>VPRORQ</c>.
    /// </summary>
    internal readonly struct Avx512Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 32 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 32);

        /// <summary>
        /// Rotates each word right by 24 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight24(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 24);

        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight16(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 16);

        /// <summary>
        /// Rotates each word right by 63 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight63(Vector256<ulong> value) =>
            Avx512F.VL.RotateRight(value, 63);
    }
}
