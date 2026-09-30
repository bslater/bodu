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
        /// The kind dispatch selects: the widest kernel the processor supports and the process allows, bar the AdvSimd
        /// kernel, which dispatch selects only on Apple's cores under .NET 8.
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
        /// The 128-bit kernel on ARM64, over AdvSimd, which dispatch selects on Apple's cores under .NET 8. Elsewhere,
        /// and under .NET 10, it runs only where a caller names it, and dispatch selects the scalar kernel, which ran
        /// faster on a Neoverse N2 under both runtimes and as fast on an Apple M1 under .NET 10.
        /// </summary>
        AdvSimd,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2.
        /// </summary>
        Avx2,
    }
}
