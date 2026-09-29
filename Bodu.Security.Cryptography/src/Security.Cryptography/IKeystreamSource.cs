// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IKeystreamSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a keystream of 64-byte blocks drawn one block at a time or as a run of whole blocks combined with input: the
/// access the Poly1305 AEAD framings make of the ChaCha20 and Salsa20 keystreams.
/// </summary>
/// <remarks>
/// <see cref="Poly1305AeadCore" /> is generic over its implementations, which are value types, so that a message drawn
/// from <see cref="ChaCha20Core.Keystream" /> or <see cref="Salsa20Core.Keystream" /> on the stack allocates nothing,
/// while an <see cref="IStreamCipher" /> engine still serves through an adapter. Both members advance the keystream
/// past the blocks they use, and both draw from the same sequence.
/// </remarks>
internal interface IKeystreamSource
{
    /// <summary>
    /// Gets the kernel that <see cref="XorBlocks" /> runs, for which the framings plan how they draw a message's
    /// keystream.
    /// </summary>
    /// <value>
    /// The kernel dispatch selects for the keystream; never <see cref="ChaCha20Core.KernelKind.Auto" />.
    /// <see cref="ChaCha20Core.KernelKind.Scalar" /> where runs take the block function one block at a time, and for a
    /// keystream whose kernels are not known.
    /// </value>
    ChaCha20Core.KernelKind Kernel { get; }

    /// <summary>
    /// Writes the next keystream block and advances past it.
    /// </summary>
    /// <param name="destination">Receives the 64-byte block in its first 64 bytes.</param>
    void NextBlock(Span<byte> destination);

    /// <summary>
    /// Combines whole blocks of input with the next keystream blocks by XOR and advances past them.
    /// </summary>
    /// <param name="input">The input, a whole number of 64-byte blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />. It may be the same memory as
    /// <paramref name="input" />, but must not partially overlap it.
    /// </param>
    void XorBlocks(ReadOnlySpan<byte> input, Span<byte> output);
}
