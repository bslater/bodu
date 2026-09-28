// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Vector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>
    /// Supplies the four rotations of <c>G</c> for the 256-bit BLAKE3 kernel on one instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector256Kernel{TIsa}" /> is written once against this interface, so the AVX-512 and AVX2 kernels
    /// share every step but these. Each member is a fixed transformation of its operand's bits.
    /// </remarks>
    internal interface IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<uint> RotateRight16(Vector256<uint> value);

        /// <summary>
        /// Rotates each word right by 12 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<uint> RotateRight12(Vector256<uint> value);

        /// <summary>
        /// Rotates each word right by 8 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<uint> RotateRight8(Vector256<uint> value);

        /// <summary>
        /// Rotates each word right by 7 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<uint> RotateRight7(Vector256<uint> value);
    }
}
