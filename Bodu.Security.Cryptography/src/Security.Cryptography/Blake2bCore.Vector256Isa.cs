// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.Vector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2bCore
{
    /// <summary>
    /// Supplies the four rotations of the 256-bit BLAKE2b kernel for one instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector256Kernel{TIsa}" /> is written once against this interface, so the AVX-512 and AVX2 kernels
    /// share every step but these. Each member is a fixed transformation of its operand's bits.
    /// </remarks>
    internal interface IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 32 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<ulong> RotateRight32(Vector256<ulong> value);

        /// <summary>
        /// Rotates each word right by 24 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<ulong> RotateRight24(Vector256<ulong> value);

        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<ulong> RotateRight16(Vector256<ulong> value);

        /// <summary>
        /// Rotates each word right by 63 bits: left by one.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<ulong> RotateRight63(Vector256<ulong> value);
    }
}
