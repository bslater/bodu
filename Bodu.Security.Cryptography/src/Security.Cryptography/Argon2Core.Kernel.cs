// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Kernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Implements the Argon2 compression function <c>G</c> (RFC 9106, Section 3.5) for one instruction set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fill is generic over this interface and constrained to structs, so the JIT emits one specialized loop per
    /// kernel and inlines its static member: choosing a kernel costs one dispatch per derivation, not one per block.
    /// </para>
    /// <para>
    /// A kernel carries the previous block across a segment in <c>state</c> instead of reading it back from the matrix.
    /// It must be free of data-dependent branches and data-dependent memory access: in Argon2i, and in the first half
    /// of Argon2id's first pass, only the choice of reference block may depend on the inputs, and that choice is made
    /// from public values before the kernel runs.
    /// </para>
    /// </remarks>
    internal interface IArgon2Kernel
    {
        /// <summary>
        /// Computes <c>G(X, Y)</c> for the previous block <c>X</c> held in <paramref name="state" /> and the reference
        /// block <c>Y</c>, stores it in <paramref name="next" />, and leaves it in <paramref name="state" /> for the
        /// following block.
        /// </summary>
        /// <param name="state">128 words holding the previous block on entry and the new block on return.</param>
        /// <param name="scratch">128 words of working space; their contents on entry are ignored.</param>
        /// <param name="reference">
        /// The 128-word reference block <c>Y</c>; read before <paramref name="next" /> is written, so it may be the
        /// same block.
        /// </param>
        /// <param name="next">The 128-word destination block.</param>
        /// <param name="withXor">
        /// <see langword="true" /> to XOR the result into the destination's existing contents, as version 0x13 does on
        /// every pass after the first; otherwise the destination is overwritten.
        /// </param>
        static abstract void FillBlock(ref ulong state, ref ulong scratch, ref ulong reference, ref ulong next, bool withXor);
    }
}
