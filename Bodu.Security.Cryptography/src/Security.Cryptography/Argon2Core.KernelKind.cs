// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Identifies an implementation of the compression function <c>G</c>. Every kind produces the same blocks.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The kind dispatch selects: the widest x64 kernel the processor supports and the process allows, the hybrid
        /// kernel on ARM64, and the scalar kernel elsewhere.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The portable scalar kernel, which runs everywhere.
        /// </summary>
        Scalar,

        /// <summary>
        /// The 128-bit kernel on x64, over SSSE3.
        /// </summary>
        Ssse3,

        /// <summary>
        /// The 128-bit kernel on ARM64, over AdvSimd, one row or column in eight vector registers. It runs only where a
        /// caller names it: dispatch selects <see cref="AdvSimdHybrid" />, and the scalar kernel ran faster than this
        /// one on a Neoverse N2.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2.
        /// </summary>
        Avx2,

        /// <summary>
        /// The kernel dispatch selects on ARM64: pairs of rows and pairs of columns, one of each pair over AdvSimd and
        /// the other in general registers, interleaved so that the vector and integer pipes work at once.
        /// </summary>
        AdvSimdHybrid,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2 with AVX-512VL's single-instruction rotation by 63 bits.
        /// </summary>
        Avx512,
    }
}
