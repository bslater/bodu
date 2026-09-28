// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Identifies an implementation of the ChaCha20 and Salsa20 keystream. Every kind produces the same keystream.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The widest kernel the processor supports and the process allows: the kind dispatch selects.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The portable scalar block function, which runs everywhere, one block at a time.
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
        /// The kernels on x64 with AVX-512VL's rotations: eight blocks at once over 256-bit vectors, and four over
        /// 128-bit vectors.
        /// </summary>
        Avx512,

        /// <summary>
        /// The <see cref="Avx512" /> kernels, with sixteen blocks at once over 512-bit vectors for runs of sixteen or
        /// more: the kind dispatch selects where the runtime prefers 512-bit vectors.
        /// </summary>
        Avx512Wide,
    }
}
