// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Vector256Isa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Supplies the per-instruction-set step of the 256-bit keystream kernels, which ChaCha20 and Salsa20 share: a
    /// rotation of every 32-bit lane.
    /// </summary>
    /// <remarks>
    /// Both 256-bit instruction sets have AVX2, whose in-lane unpacks the kernels use directly for the transpose, so
    /// only the rotation differs between them.
    /// </remarks>
    internal interface IVector256Isa
    {
        /// <summary>
        /// Rotates every 32-bit lane left by a constant number of bits.
        /// </summary>
        /// <param name="value">The lanes to rotate.</param>
        /// <param name="count">The number of bits, a constant from 1 to 31.</param>
        /// <returns>The rotated lanes.</returns>
        static abstract Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected(Min = 1, Max = 31)] byte count);
    }
}
