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
        /// The kind dispatch selects: the widest kernel the processor supports and the process allows, bar the AdvSimd
        /// kernel, which <see cref="SimdCapabilities.AdvSimdSingleState" /> holds back.
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
        /// The 128-bit kernel on ARM64, over AdvSimd, which runs only where a caller names it: dispatch selects the
        /// scalar kernel, which ran faster on the ARM64 processors measured.
        /// </summary>
        AdvSimd,
    }
}
