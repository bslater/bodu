// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Ssse3.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Supplies the 128-bit kernel's instruction-set operations on x64, from SSE2 and SSSE3.
    /// </summary>
    internal readonly struct Ssse3Isa
        : IVector128Isa
    {
        /// <summary>
        /// Multiplies the low 32 bits of each pair of words into a 64-bit product, with <c>PMULUDQ</c>.
        /// </summary>
        /// <param name="x">The first operand's two words.</param>
        /// <param name="y">The second operand's two words.</param>
        /// <returns>The two 64-bit products.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyLow(Vector128<ulong> x, Vector128<ulong> y) =>
            Sse2.Multiply(x.AsUInt32(), y.AsUInt32());

        /// <summary>
        /// Rotates each word right by 32 bits by swapping its halves, with <c>PSHUFD</c>.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> RotateRight32(Vector128<ulong> x) =>
            Sse2.Shuffle(x.AsUInt32(), 0xB1).AsUInt64();

        /// <summary>
        /// Rotates each word right by 24 bits, with <c>PSHUFB</c> over constant indices.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> RotateRight24(Vector128<ulong> x) =>
            Ssse3.Shuffle(x.AsByte(), Vector128.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64();

        /// <summary>
        /// Rotates each word right by 16 bits, with <c>PSHUFB</c> over constant indices.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> RotateRight16(Vector128<ulong> x) =>
            Ssse3.Shuffle(x.AsByte(), Vector128.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64();

        /// <summary>
        /// Returns the upper word of <paramref name="first" /> followed by the lower word of <paramref name="second" />,
        /// with <c>PALIGNR</c>.
        /// </summary>
        /// <param name="first">The vector whose upper word becomes the result's lower word.</param>
        /// <param name="second">The vector whose lower word becomes the result's upper word.</param>
        /// <returns><c>(first[1], second[0])</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> UpperThenLower(Vector128<ulong> first, Vector128<ulong> second) =>
            Ssse3.AlignRight(second.AsByte(), first.AsByte(), 8).AsUInt64();
    }
}
