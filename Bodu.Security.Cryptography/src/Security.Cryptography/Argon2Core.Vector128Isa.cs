// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Vector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Supplies the five operations of the 128-bit compression kernel that have no portable <see cref="Vector128" />
    /// form, for one instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector128Kernel{TIsa}" /> is written once against this interface and specialized per instruction set,
    /// so the kernel's logic runs on every x64 host and only these members differ between x64 and ARM64. Each member is
    /// a fixed transformation of its operands: none may branch on, or index memory by, their values.
    /// </remarks>
    internal interface IVector128Isa
    {
        /// <summary>
        /// Multiplies the low 32 bits of each word of <paramref name="x" /> by the low 32 bits of the matching word of
        /// <paramref name="y" />, giving two full 64-bit products.
        /// </summary>
        /// <param name="x">The first operand's two words.</param>
        /// <param name="y">The second operand's two words.</param>
        /// <returns>The two 64-bit products.</returns>
        static abstract Vector128<ulong> MultiplyLow(Vector128<ulong> x, Vector128<ulong> y);

        /// <summary>
        /// Rotates each word right by 32 bits.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<ulong> RotateRight32(Vector128<ulong> x);

        /// <summary>
        /// Rotates each word right by 24 bits.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<ulong> RotateRight24(Vector128<ulong> x);

        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<ulong> RotateRight16(Vector128<ulong> x);

        /// <summary>
        /// Returns the upper word of <paramref name="first" /> followed by the lower word of <paramref name="second" />.
        /// </summary>
        /// <param name="first">The vector whose upper word becomes the result's lower word.</param>
        /// <param name="second">The vector whose lower word becomes the result's upper word.</param>
        /// <returns><c>(first[1], second[0])</c>.</returns>
        static abstract Vector128<ulong> UpperThenLower(Vector128<ulong> first, Vector128<ulong> second);
    }
}
