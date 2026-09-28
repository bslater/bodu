// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.ClmulIsa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Supplies the instruction-set operations the carry-less GHASH kernel needs, so that one kernel serves both the
    /// x64 carry-less multiply and the ARM64 polynomial multiply.
    /// </summary>
    /// <remarks>
    /// Each operation is a single instruction, or two, on its architecture. The byte shifts move whole bytes toward the
    /// high or the low end of the vector, filling with zeros, as <c>PSLLDQ</c> and <c>PSRLDQ</c> do; the counts are
    /// separate methods so each call site passes the instruction a constant.
    /// </remarks>
    internal interface IClmulIsa
    {
        /// <summary>
        /// Multiplies the low 64-bit halves of two vectors without carries.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        static abstract Vector128<ulong> MultiplyLower(Vector128<ulong> left, Vector128<ulong> right);

        /// <summary>
        /// Multiplies the high 64-bit halves of two vectors without carries.
        /// </summary>
        /// <param name="left">The first operand.</param>
        /// <param name="right">The second operand.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        static abstract Vector128<ulong> MultiplyUpper(Vector128<ulong> left, Vector128<ulong> right);

        /// <summary>
        /// Multiplies the low half of <paramref name="left" /> by the high half of <paramref name="right" /> without
        /// carries.
        /// </summary>
        /// <param name="left">The operand whose low half is taken.</param>
        /// <param name="right">The operand whose high half is taken.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        static abstract Vector128<ulong> MultiplyLowerUpper(Vector128<ulong> left, Vector128<ulong> right);

        /// <summary>
        /// Multiplies the high half of <paramref name="left" /> by the low half of <paramref name="right" /> without
        /// carries.
        /// </summary>
        /// <param name="left">The operand whose high half is taken.</param>
        /// <param name="right">The operand whose low half is taken.</param>
        /// <returns>The 128-bit carry-less product.</returns>
        static abstract Vector128<ulong> MultiplyUpperLower(Vector128<ulong> left, Vector128<ulong> right);

        /// <summary>
        /// Reverses the order of the sixteen bytes of a vector.
        /// </summary>
        /// <param name="value">The vector to reverse.</param>
        /// <returns>The reversed vector.</returns>
        static abstract Vector128<byte> ReverseBytes(Vector128<byte> value);

        /// <summary>
        /// Shifts a vector four bytes toward its high end, filling with zeros.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        static abstract Vector128<byte> ShiftBytesLeft4(Vector128<byte> value);

        /// <summary>
        /// Shifts a vector eight bytes toward its high end, filling with zeros.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        static abstract Vector128<byte> ShiftBytesLeft8(Vector128<byte> value);

        /// <summary>
        /// Shifts a vector twelve bytes toward its high end, filling with zeros.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        static abstract Vector128<byte> ShiftBytesLeft12(Vector128<byte> value);

        /// <summary>
        /// Shifts a vector four bytes toward its low end, filling with zeros.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        static abstract Vector128<byte> ShiftBytesRight4(Vector128<byte> value);

        /// <summary>
        /// Shifts a vector eight bytes toward its low end, filling with zeros.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        static abstract Vector128<byte> ShiftBytesRight8(Vector128<byte> value);

        /// <summary>
        /// Shifts a vector twelve bytes toward its low end, filling with zeros.
        /// </summary>
        /// <param name="value">The vector to shift.</param>
        /// <returns>The shifted vector.</returns>
        static abstract Vector128<byte> ShiftBytesRight12(Vector128<byte> value);
    }
}
