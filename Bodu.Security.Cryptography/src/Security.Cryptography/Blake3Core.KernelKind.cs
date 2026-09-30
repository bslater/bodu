// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>
    /// Identifies an implementation of the BLAKE3 compression function. Every kind produces the same chaining values.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The kind dispatch selects: the widest kernel the processor supports and the process allows, bar the 128-bit
        /// AdvSimd kernel for a single block, which <see cref="SimdCapabilities.AdvSimdSingleState" /> holds back.
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
        /// The kernels on ARM64: up to four inputs at once over AdvSimd, and for a lone input the scalar kernel, which
        /// compressed a block faster than the 128-bit kernel on the ARM64 processors measured. A single compression
        /// runs the 128-bit kernel only where a caller names this kind.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The kernels on x64 with AVX2: eight inputs at once over 256-bit vectors, and the SSSE3 kernel for fewer.
        /// </summary>
        Avx2,

        /// <summary>
        /// The kernels on x64 with AVX-512VL's rotations: eight inputs at once over 256-bit vectors, and the 128-bit
        /// kernel for fewer.
        /// </summary>
        Avx512,

        /// <summary>
        /// The <see cref="Avx512" /> kernels, with sixteen inputs at once over 512-bit vectors for runs of more than
        /// eight: the kind dispatch selects where the runtime prefers 512-bit vectors.
        /// </summary>
        Avx512Wide,
    }
}
