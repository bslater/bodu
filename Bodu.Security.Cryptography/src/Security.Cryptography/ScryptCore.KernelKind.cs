// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Identifies an implementation of <c>scryptBlockMix</c>. Every kind produces the same key.
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
        /// The 128-bit kernel on x64, over SSE2.
        /// </summary>
        Sse2,

        /// <summary>
        /// The 128-bit kernel on ARM64, over AdvSimd.
        /// </summary>
        AdvSimd,
    }
}
