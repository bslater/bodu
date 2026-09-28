// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.AdvSimd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Supplies the 128-bit kernel's instruction-set operations on ARM64, from AdvSimd.
    /// </summary>
    /// <remarks>
    /// These members run only on ARM64 hardware; on any other processor they throw
    /// <see cref="PlatformNotSupportedException" />, so dispatch selects them only where AdvSimd is present.
    /// </remarks>
    internal readonly struct AdvSimdIsa
        : IVector128Isa
    {
        /// <summary>
        /// Multiplies the low 32 bits of each pair of words into a 64-bit product: <c>XTN</c> keeps each word's low
        /// half, and <c>UMULL</c> widens the products.
        /// </summary>
        /// <param name="x">The first operand's two words.</param>
        /// <param name="y">The second operand's two words.</param>
        /// <returns>The two 64-bit products.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyLow(Vector128<ulong> x, Vector128<ulong> y) =>
            AdvSimd.MultiplyWideningLower(AdvSimd.ExtractNarrowingLower(x), AdvSimd.ExtractNarrowingLower(y));

        /// <summary>
        /// Rotates each word right by 32 bits by swapping its halves, with <c>REV64</c> on 32-bit elements.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> RotateRight32(Vector128<ulong> x) =>
            AdvSimd.ReverseElement32(x);

        /// <summary>
        /// Rotates each word right by 24 bits, with <c>TBL</c> over constant indices.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> RotateRight24(Vector128<ulong> x) =>
            AdvSimd.Arm64.VectorTableLookup(x.AsByte(), Vector128.Create((byte)3, 4, 5, 6, 7, 0, 1, 2, 11, 12, 13, 14, 15, 8, 9, 10)).AsUInt64();

        /// <summary>
        /// Rotates each word right by 16 bits, with <c>TBL</c> over constant indices.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> RotateRight16(Vector128<ulong> x) =>
            AdvSimd.Arm64.VectorTableLookup(x.AsByte(), Vector128.Create((byte)2, 3, 4, 5, 6, 7, 0, 1, 10, 11, 12, 13, 14, 15, 8, 9)).AsUInt64();

        /// <summary>
        /// Returns the upper word of <paramref name="first" /> followed by the lower word of <paramref name="second" />,
        /// with <c>EXT</c>.
        /// </summary>
        /// <param name="first">The vector whose upper word becomes the result's lower word.</param>
        /// <param name="second">The vector whose lower word becomes the result's upper word.</param>
        /// <returns><c>(first[1], second[0])</c>.</returns>
        /// <remarks>
        /// <c>EXT</c> takes bytes from its first operand's position eight onward, then its second operand's first
        /// eight; the API names its operands <c>upper</c> and <c>lower</c>, but the first is the one read first.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> UpperThenLower(Vector128<ulong> first, Vector128<ulong> second) =>
            AdvSimd.ExtractVector128(first.AsByte(), second.AsByte(), 8).AsUInt64();
    }
}
