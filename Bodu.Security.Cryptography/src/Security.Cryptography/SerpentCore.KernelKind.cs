// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class SerpentCore
{
    /// <summary>
    /// Identifies an implementation of Serpent-128 over runs of blocks. Every kind produces the same output.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The widest kernel the processor supports and the process allows: the kind dispatch selects.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The portable scalar rounds, which run everywhere, one block at a time.
        /// </summary>
        Scalar,

        /// <summary>
        /// Four blocks at once over 128-bit vectors on x64, over SSSE3.
        /// </summary>
        Ssse3,

        /// <summary>
        /// Four blocks at once over 128-bit vectors on ARM64, over AdvSimd.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The kernels on x64 with AVX2: eight blocks at once over 256-bit vectors, and the SSSE3 kernel for four.
        /// </summary>
        Avx2,

        /// <summary>
        /// The kernels on x64 with AVX-512VL's rotate instruction: eight blocks at once over 256-bit vectors, and four
        /// over 128-bit vectors.
        /// </summary>
        Avx512,
    }
}
