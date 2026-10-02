// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Avx512.cs" company="Bodu Pty. Ltd.">
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
    /// Supplies the rotations with AVX-512VL: by 24, 16 and 63 bits a single <c>VPRORQ</c>, and by 32 bits the
    /// <c>VPSHUFD</c> that <see cref="Avx2Isa" /> also uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rotations were chosen by timing the kernel with each combination on AMD's Zen 4 and Zen 5 and on three
    /// generations of Intel's Xeon. Swapping each word's halves with <c>VPSHUFD</c> made it 6% faster than
    /// <c>VPRORQ</c> did on a Zen 4, where <see cref="Avx2Isa" /> had run faster than this kernel, and left it as fast
    /// or slightly faster on the other processors.
    /// </para>
    /// <para>
    /// <c>VPRORQ</c> stays for the other rotations. Rotating by 63 bits with <see cref="Avx2Isa" />'s addition, shift
    /// and XOR made the kernel 8-9% slower on the Xeons and the Zen 5, and rotating by 24 and 16 bits with its byte
    /// shuffles made it about 1% slower on the Zen 5, and faster on the Zen 4 under .NET 10 alone.
    /// </para>
    /// </remarks>
    internal readonly struct Avx512Isa
        : IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 32 bits, swapping its halves.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ulong> RotateRight32(Vector256<ulong> value) =>
            Avx2.Shuffle(value.AsUInt32(), 0b10_11_00_01).AsUInt64();

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
