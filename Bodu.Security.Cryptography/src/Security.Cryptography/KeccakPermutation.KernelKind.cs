// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutation.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class KeccakPermutation
{
    /// <summary>
    /// Identifies an implementation of the four-way permutation. Every kind produces the same four states.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The widest kernel the processor supports and the process allows: the kind dispatch selects.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Four calls of the scalar permutation, one state at a time, which runs everywhere.
        /// </summary>
        Scalar,

        /// <summary>
        /// The 256-bit kernel on x64, over AVX2: each rotation a pair of shifts, or one byte shuffle.
        /// </summary>
        Avx2,

        /// <summary>
        /// The 256-bit kernel on x64, with AVX-512VL's rotate instruction and three-input logic.
        /// </summary>
        Avx512,
    }
}
