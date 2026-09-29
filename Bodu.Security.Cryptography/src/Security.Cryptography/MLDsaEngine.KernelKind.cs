// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class MLDsaEngine
{
    /// <summary>
    /// Identifies an implementation of the number-theoretic transforms and the coefficient-wise products. Every kind
    /// produces the same coefficients.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The widest kernel the processor supports and the process allows: the kind dispatch selects.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The portable scalar code, one coefficient at a time, which runs everywhere.
        /// </summary>
        Scalar,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2: eight coefficients per vector.
        /// </summary>
        Avx2,
    }
}
