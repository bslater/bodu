// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// Supplies the rotations with AVX-512VL, each a single <c>VPRORD</c>, and the lane rotations with <c>PSHUFD</c>.
    /// </summary>
    internal readonly struct Avx512Isa
        : IVector128Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight16(Vector128<uint> value) =>
            Avx512F.VL.RotateRight(value, 16);

        /// <summary>
        /// Rotates each word right by 12 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight12(Vector128<uint> value) =>
            Avx512F.VL.RotateRight(value, 12);

        /// <summary>
        /// Rotates each word right by 8 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight8(Vector128<uint> value) =>
            Avx512F.VL.RotateRight(value, 8);

        /// <summary>
        /// Rotates each word right by 7 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateRight7(Vector128<uint> value) =>
            Avx512F.VL.RotateRight(value, 7);

        /// <summary>
        /// Rotates the four lanes one place toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes1(Vector128<uint> value) =>
            Sse2.Shuffle(value, 0b00_11_10_01);

        /// <summary>
        /// Rotates the four lanes two places.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes2(Vector128<uint> value) =>
            Sse2.Shuffle(value, 0b01_00_11_10);

        /// <summary>
        /// Rotates the four lanes three places toward lane 0.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<uint> RotateLanes3(Vector128<uint> value) =>
            Sse2.Shuffle(value, 0b10_01_00_11);
    }
}
