// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCore.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class CubeHashCore
{
    /// <summary>
    /// Identifies an implementation of the CubeHash round function. Every kind produces the same state.
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
        /// The state in eight 128-bit registers on x64, over SSSE3.
        /// </summary>
        Ssse3,

        /// <summary>
        /// The state in eight 128-bit registers on ARM64, over AdvSimd.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The state in four 256-bit registers on x64 with AVX2.
        /// </summary>
        Avx2,

        /// <summary>
        /// The state in two 512-bit registers on x64 with AVX-512F, each exchange one full-register permutation.
        /// </summary>
        Avx512,
    }
}
