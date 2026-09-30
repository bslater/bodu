// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCore.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Blake2bCore
{
    /// <summary>
    /// Identifies an implementation of the BLAKE2b compression function. Every kind produces the same state.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The kind dispatch selects: the widest kernel the processor supports and the process allows, bar the AdvSimd
        /// kernel, which <see cref="SimdCapabilities.AdvSimdSingleState" /> holds back.
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
        /// The 128-bit kernel on ARM64, over AdvSimd, which runs only where a caller names it: dispatch selects the
        /// scalar kernel, which ran faster on the ARM64 processors measured.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2.
        /// </summary>
        Avx2,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2 with AVX-512VL's rotations.
        /// </summary>
        Avx512,
    }
}
