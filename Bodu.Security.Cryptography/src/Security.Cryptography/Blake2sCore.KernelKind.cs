// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCore.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Blake2sCore
{
    /// <summary>
    /// Identifies an implementation of the BLAKE2s compression function. Every kind produces the same state.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The widest kernel the processor supports and the process allows: the kind dispatch selects.
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
        /// The 128-bit kernel on ARM64, over AdvSimd.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The 128-bit kernel on x64, with AVX-512VL's rotations.
        /// </summary>
        Avx512,
    }
}
