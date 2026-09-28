// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>
    /// Supplies the rotations with AVX-512VL, each a single <c>VPRORD</c>.
    /// </summary>
    /// <remarks>
    /// Its kernel also gains the sixteen further vector registers the EVEX encoding reaches, which spare it most of the
    /// spills the AVX2 kernel's sixteen-word working vector forces.
    /// </remarks>
    internal readonly struct Avx512Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight16(Vector256<uint> value) =>
            Avx512F.VL.RotateRight(value, 16);

        /// <summary>
        /// Rotates each word right by 12 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight12(Vector256<uint> value) =>
            Avx512F.VL.RotateRight(value, 12);

        /// <summary>
        /// Rotates each word right by 8 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight8(Vector256<uint> value) =>
            Avx512F.VL.RotateRight(value, 8);

        /// <summary>
        /// Rotates each word right by 7 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<uint> RotateRight7(Vector256<uint> value) =>
            Avx512F.VL.RotateRight(value, 7);
    }
}
