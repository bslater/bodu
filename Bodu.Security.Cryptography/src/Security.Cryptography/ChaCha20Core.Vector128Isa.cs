// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Vector128Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Supplies the per-instruction-set steps of the 128-bit keystream kernels, which ChaCha20 and Salsa20 share: a
    /// rotation of every 32-bit lane and a 4×4 transpose.
    /// </summary>
    /// <remarks>
    /// Each implementation is a struct type argument, so the kernel is compiled once per instruction set with the steps
    /// inlined, and the tests can run every shim the processor supports on the same machine.
    /// </remarks>
    internal interface IVector128Isa
    {
        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, a constant from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count);

        /// <summary>
        /// Transposes four rows of four 32-bit words in place, so that row <c>i</c> becomes column <c>i</c>.
        /// </summary>
        /// <param name="row0">The first row, replaced by the first column.</param>
        /// <param name="row1">The second row, replaced by the second column.</param>
        /// <param name="row2">The third row, replaced by the third column.</param>
        /// <param name="row3">The fourth row, replaced by the fourth column.</param>
        static abstract void Transpose(ref Vector128<uint> row0, ref Vector128<uint> row1, ref Vector128<uint> row2, ref Vector128<uint> row3);
    }
}
