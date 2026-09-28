// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ghash.KernelKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Ghash
{
    /// <summary>
    /// Identifies the kernel a <see cref="Key" /> is prepared for.
    /// </summary>
    internal enum KernelKind
    {
        /// <summary>
        /// The portable scalar multiply, built from masked integer multiplications.
        /// </summary>
        Scalar = 0,

        /// <summary>
        /// The x64 carry-less multiply (<c>PCLMULQDQ</c>), with <c>SSSE3</c> byte shuffles.
        /// </summary>
        Pclmulqdq = 1,

        /// <summary>
        /// The ARM64 polynomial multiply (<c>PMULL</c> / <c>PMULL2</c>) from the cryptography extension.
        /// </summary>
        Pmull = 2,
    }
}
