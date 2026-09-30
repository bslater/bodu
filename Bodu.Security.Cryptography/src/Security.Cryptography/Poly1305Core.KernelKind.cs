// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Core.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal partial struct Poly1305Core
{
    /// <summary>
    /// Identifies an implementation of the Poly1305 block loop. Every kind produces the same accumulator.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The kernel dispatch selects for the length of the run: the kind every caller outside the tests passes.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The portable scalar loop, which runs everywhere, one block at a time over limbs of 44, 44 and 42 bits.
        /// </summary>
        Scalar,

        /// <summary>
        /// Four blocks at once over 256-bit vectors on x64 with AVX2, one block in each 64-bit lane, one group of four
        /// at a time.
        /// </summary>
        Avx2,

        /// <summary>
        /// The <see cref="Avx2" /> kernel taking two groups of four at a time, for longer runs where AVX-512VL provides
        /// the 32 vector registers its loop needs. It uses only AVX2's instructions, so it runs wherever
        /// <see cref="Avx2" /> does.
        /// </summary>
        Avx2Paired,

        /// <summary>
        /// Eight blocks at once over 512-bit vectors on x64 with AVX-512F, one block in each 64-bit lane.
        /// </summary>
        Avx512,

        /// <summary>
        /// Two blocks at once over 128-bit vectors on ARM64 with AdvSimd, one block in each 64-bit lane, one group of
        /// two at a time.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The <see cref="AdvSimd" /> kernel taking two groups of two at a time, for longer runs.
        /// </summary>
        AdvSimdPaired,
    }
}
