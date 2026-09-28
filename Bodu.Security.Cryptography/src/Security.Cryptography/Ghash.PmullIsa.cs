// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.PmullIsa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Supplies the carry-less GHASH kernel's operations on ARM64, from the cryptography extension's <c>PMULL</c> and
    /// AdvSimd.
    /// </summary>
    /// <remarks>
    /// <c>EXT</c> takes bytes from the concatenation of its two sources, the first source supplying the low bytes, so
    /// extracting from a zero vector and the value shifts whole bytes in either direction, as <c>PSLLDQ</c> and
    /// <c>PSRLDQ</c> do on x64.
    /// </remarks>
    internal readonly struct PmullIsa
        : IClmulIsa
    {
        /// <summary>
        /// Multiplies the low halves without carries, with <c>PMULL</c>.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyLower(Vector128<ulong> left, Vector128<ulong> right) =>
            Aes.PolynomialMultiplyWideningLower(left.GetLower(), right.GetLower());

        /// <summary>
        /// Multiplies the high halves without carries, with <c>PMULL2</c>.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyUpper(Vector128<ulong> left, Vector128<ulong> right) =>
            Aes.PolynomialMultiplyWideningUpper(left, right);

        /// <summary>
        /// Multiplies the low half of <paramref name="left" /> by the high half of <paramref name="right" />, with
        /// <c>PMULL</c>.
        /// </summary>
        /// <param name="left">The operand whose low half is taken.</param>
        /// <param name="right">The operand whose high half is taken.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyLowerUpper(Vector128<ulong> left, Vector128<ulong> right) =>
            Aes.PolynomialMultiplyWideningLower(left.GetLower(), right.GetUpper());

        /// <summary>
        /// Multiplies the high half of <paramref name="left" /> by the low half of <paramref name="right" />, with
        /// <c>PMULL</c>.
        /// </summary>
        /// <param name="left">The operand whose high half is taken.</param>
        /// <param name="right">The operand whose low half is taken.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ulong> MultiplyUpperLower(Vector128<ulong> left, Vector128<ulong> right) =>
            Aes.PolynomialMultiplyWideningLower(left.GetUpper(), right.GetLower());

        /// <summary>
        /// Reverses the sixteen bytes: <c>REV64</c> reverses each half, and <c>EXT</c> swaps the halves.
        /// </summary>
        /// <param name="value">The vector to reverse.</param>
        /// <returns>The reversed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ReverseBytes(Vector128<byte> value)
        {
            Vector128<byte> halves = AdvSimd.ReverseElement8(value.AsUInt64()).AsByte();
            return AdvSimd.ExtractVector128(halves, halves, 8);
        }

        /// <summary>
        /// Shifts four bytes toward the high end, with <c>EXT</c> from a zero vector.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesLeft4(Vector128<byte> value) =>
            AdvSimd.ExtractVector128(Vector128<byte>.Zero, value, 12);

        /// <summary>
        /// Shifts eight bytes toward the high end, with <c>EXT</c> from a zero vector.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesLeft8(Vector128<byte> value) =>
            AdvSimd.ExtractVector128(Vector128<byte>.Zero, value, 8);

        /// <summary>
        /// Shifts twelve bytes toward the high end, with <c>EXT</c> from a zero vector.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesLeft12(Vector128<byte> value) =>
            AdvSimd.ExtractVector128(Vector128<byte>.Zero, value, 4);

        /// <summary>
        /// Shifts four bytes toward the low end, with <c>EXT</c> into a zero vector.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesRight4(Vector128<byte> value) =>
            AdvSimd.ExtractVector128(value, Vector128<byte>.Zero, 4);

        /// <summary>
        /// Shifts eight bytes toward the low end, with <c>EXT</c> into a zero vector.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesRight8(Vector128<byte> value) =>
            AdvSimd.ExtractVector128(value, Vector128<byte>.Zero, 8);

        /// <summary>
        /// Shifts twelve bytes toward the low end, with <c>EXT</c> into a zero vector.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> ShiftBytesRight12(Vector128<byte> value) =>
            AdvSimd.ExtractVector128(value, Vector128<byte>.Zero, 12);
    }
}
