// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.Vector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// Supplies the four rotations of <c>G</c> and the three lane rotations of the 128-bit BLAKE2s kernel for one
    /// instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector128Kernel{TIsa}" /> is written once against this interface, so the AVX-512, SSSE3 and AdvSimd
    /// kernels share every step but these. Each member is a fixed permutation of its operand's bits or lanes.
    /// </remarks>
    internal interface IVector128Isa
    {
        /// <summary>
        /// Rotates each word right by 16 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<uint> RotateRight16(Vector128<uint> value);

        /// <summary>
        /// Rotates each word right by 12 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<uint> RotateRight12(Vector128<uint> value);

        /// <summary>
        /// Rotates each word right by 8 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<uint> RotateRight8(Vector128<uint> value);

        /// <summary>
        /// Rotates each word right by 7 bits.
        /// </summary>
        /// <param name="value">The words to rotate.</param>
        /// <returns>The rotated words.</returns>
        static abstract Vector128<uint> RotateRight7(Vector128<uint> value);

        /// <summary>
        /// Rotates the four lanes one place toward lane 0: lane <c>i</c> of the result is lane <c>(i + 1) mod 4</c>.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLanes1(Vector128<uint> value);

        /// <summary>
        /// Rotates the four lanes two places, swapping the vector's halves.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLanes2(Vector128<uint> value);

        /// <summary>
        /// Rotates the four lanes three places toward lane 0: lane <c>i</c> of the result is lane <c>(i + 3) mod 4</c>.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLanes3(Vector128<uint> value);
    }
}
