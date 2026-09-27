// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.PclmulqdqIsa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Supplies the carry-less GHASH kernel's operations on x64, from <c>PCLMULQDQ</c>, <c>SSSE3</c>, and <c>SSE2</c>.
    /// </summary>
    internal readonly struct PclmulqdqIsa
        : IClmulIsa
    {
        /// <summary>
        /// Gets the <c>PSHUFB</c> indices that reverse a vector's sixteen bytes.
        /// </summary>
        private static Vector128<byte> ReverseMask =>
            Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

        /// <summary>
        /// Multiplies the low halves without carries, with <c>PCLMULQDQ</c> selector <c>0x00</c>.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyLower(Vector128<ulong> left, Vector128<ulong> right) =>
            Pclmulqdq.CarrylessMultiply(left, right, 0x00);

        /// <summary>
        /// Multiplies the high halves without carries, with <c>PCLMULQDQ</c> selector <c>0x11</c>.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyUpper(Vector128<ulong> left, Vector128<ulong> right) =>
            Pclmulqdq.CarrylessMultiply(left, right, 0x11);

        /// <summary>
        /// Multiplies the low half of <paramref name="left" /> by the high half of <paramref name="right" />, with
        /// <c>PCLMULQDQ</c> selector <c>0x10</c>.
        /// </summary>
        /// <param name="left">The operand whose low half is taken.</param>
        /// <param name="right">The operand whose high half is taken.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyLowerUpper(Vector128<ulong> left, Vector128<ulong> right) =>
            Pclmulqdq.CarrylessMultiply(left, right, 0x10);

        /// <summary>
        /// Multiplies the high half of <paramref name="left" /> by the low half of <paramref name="right" />, with
        /// <c>PCLMULQDQ</c> selector <c>0x01</c>.
        /// </summary>
        /// <param name="left">The operand whose high half is taken.</param>
        /// <param name="right">The operand whose low half is taken.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyUpperLower(Vector128<ulong> left, Vector128<ulong> right) =>
            Pclmulqdq.CarrylessMultiply(left, right, 0x01);

        /// <summary>
        /// Reverses the sixteen bytes, with <c>PSHUFB</c> over constant indices.
        /// </summary>
        /// <param name="value">The vector to reverse.</param>
        /// <returns>The reversed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ReverseBytes(Vector128<byte> value) =>
            Ssse3.Shuffle(value, ReverseMask);

        /// <summary>
        /// Shifts four bytes toward the high end, with <c>PSLLDQ</c>.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesLeft4(Vector128<byte> value) =>
            Sse2.ShiftLeftLogical128BitLane(value, 4);

        /// <summary>
        /// Shifts eight bytes toward the high end, with <c>PSLLDQ</c>.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesLeft8(Vector128<byte> value) =>
            Sse2.ShiftLeftLogical128BitLane(value, 8);

        /// <summary>
        /// Shifts twelve bytes toward the high end, with <c>PSLLDQ</c>.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesLeft12(Vector128<byte> value) =>
            Sse2.ShiftLeftLogical128BitLane(value, 12);

        /// <summary>
        /// Shifts four bytes toward the low end, with <c>PSRLDQ</c>.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesRight4(Vector128<byte> value) =>
            Sse2.ShiftRightLogical128BitLane(value, 4);

        /// <summary>
        /// Shifts eight bytes toward the low end, with <c>PSRLDQ</c>.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesRight8(Vector128<byte> value) =>
            Sse2.ShiftRightLogical128BitLane(value, 8);

        /// <summary>
        /// Shifts twelve bytes toward the low end, with <c>PSRLDQ</c>.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesRight12(Vector128<byte> value) =>
            Sse2.ShiftRightLogical128BitLane(value, 12);
    }
}
