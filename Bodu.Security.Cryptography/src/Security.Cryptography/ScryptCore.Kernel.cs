// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.Kernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Implements <c>scryptBlockMix</c> and the Salsa20/8 core (RFC 7914, Sections 3 and 4) for one instruction set,
    /// together with the order the kernel keeps each 64-byte block's words in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ROMix is generic over this interface and constrained to structs, so the JIT emits one specialized loop per
    /// kernel and inlines its static members: choosing a kernel costs one dispatch per unit, not one per block.
    /// </para>
    /// <para>
    /// A kernel may keep the sixteen words of each block in an order of its own, provided word 0 stays first. ROMix
    /// converts a unit into that order once, on entry, and back once, on exit; every block in between, <c>V</c>
    /// included, stays in it, and the index <c>Integerify</c> takes is word 0 of the last block either way.
    /// </para>
    /// <para>
    /// A kernel must be free of data-dependent branches and data-dependent memory access. scrypt's only data-dependent
    /// access is ROMix's read of <c>V[j]</c>, which is inherent to its design and made outside the kernel.
    /// </para>
    /// </remarks>
    internal interface IScryptKernel
    {
        /// <summary>
        /// Rearranges the words of each 64-byte block from RFC 7914's order into the kernel's, in place.
        /// </summary>
        /// <param name="blocks">The first word of the first block.</param>
        /// <param name="count">The number of 64-byte blocks.</param>
        static abstract void Import(ref uint blocks, int count);

        /// <summary>
        /// Rearranges the words of each 64-byte block from the kernel's order back into RFC 7914's, in place.
        /// </summary>
        /// <param name="blocks">The first word of the first block.</param>
        /// <param name="count">The number of 64-byte blocks.</param>
        static abstract void Export(ref uint blocks, int count);

        /// <summary>
        /// Applies the Salsa20/8 core in place to one 64-byte block held in the kernel's order.
        /// </summary>
        /// <param name="block">The first of the block's sixteen words.</param>
        static abstract void Salsa20_8(ref uint block);

        /// <summary>
        /// Applies <c>scryptBlockMix</c> to <paramref name="input" />, writing the shuffled result to
        /// <paramref name="output" />.
        /// </summary>
        /// <param name="input">The first word of the 2·r input blocks, in the kernel's order.</param>
        /// <param name="output">The first word of the destination; it must not overlap the input.</param>
        /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
        static abstract void BlockMix(ref uint input, ref uint output, int blockSizeR);

        /// <summary>
        /// Applies <c>scryptBlockMix</c> to <c><paramref name="x" /> xor <paramref name="v" /></c>, writing the
        /// shuffled result to <paramref name="output" />, without writing the XOR out first.
        /// </summary>
        /// <param name="x">The first word of the ROMix state <c>X</c>, in the kernel's order.</param>
        /// <param name="v">The first word of the chain unit <c>V[j]</c>, in the kernel's order.</param>
        /// <param name="output">The first word of the destination; it must overlap neither input.</param>
        /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
        static abstract void BlockMixXor(ref uint x, ref uint v, ref uint output, int blockSizeR);
    }
}
