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
    /// Supplies the four rotations of <c>G</c>, the three lane rotations and the 4×4 transpose of the 128-bit BLAKE2s
    /// and BLAKE3 kernels for one instruction set.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector128Kernel{TIsa}" /> and BLAKE3's 128-bit kernels are written once against this interface, so
    /// the AVX-512, SSSE3 and AdvSimd kernels share every step but these; BLAKE3's <c>G</c> is BLAKE2s's, rotations
    /// included. Each member is a fixed permutation of its operands' bits or lanes.
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

        /// <summary>
        /// Transposes four rows of four words: afterwards row <c>i</c> holds what was column <c>i</c>.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        /// <remarks>
        /// BLAKE3's four-way kernel uses it to turn four inputs' message words into one vector per word, and the four
        /// chaining values back.
        /// </remarks>
        static abstract void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3);
    }
}
