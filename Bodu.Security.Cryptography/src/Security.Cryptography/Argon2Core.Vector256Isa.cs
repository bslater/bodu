// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Vector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Supplies the rotation by 63 bits of the 256-bit compression kernel for one instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector256Kernel{TIsa}" /> is written once against this interface, so the AVX2 and AVX-512 kernels
    /// share every step but this one. The rotation is a fixed transformation of its operand's bits: it may not branch
    /// on, or index memory by, their values.
    /// </remarks>
    internal interface IVector256Isa
    {
        /// <summary>
        /// Rotates each word right by 63 bits: left by one.
        /// </summary>
        /// <param name="x">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector256<ulong> RotateRight63(Vector256<ulong> x);
    }
}
