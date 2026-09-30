// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICounterModeBlockCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extends <see cref="IBlockCipher" /> with a counter-mode entry point for 128-bit block ciphers whose kernels form the
/// counter blocks and combine their keystream with the input themselves.
/// </summary>
/// <remarks>
/// The entry point produces exactly what encrypting each counter block with <see cref="IBlockCipher.Encrypt" /> and
/// combining the result with the input would, with no run of counter blocks laid out in memory and no second pass to
/// combine the keystream. <see cref="CtrModeTransform" /> and <see cref="CounterKeystream" />, the counter EAX and SIV
/// share, use it where the cipher implements it, and a run of counter blocks through
/// <see cref="IBlockCipher.EncryptBlocks" /> otherwise.
/// </remarks>
internal interface ICounterModeBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// Combines the input by XOR with the keystream of successive counter blocks, and advances the counter past the
    /// blocks used.
    /// </summary>
    /// <param name="counterHigh">The counter's high 64 bits, the first eight bytes of the counter block.</param>
    /// <param name="counterLow">The counter's low 64 bits, the last eight bytes of the counter block.</param>
    /// <param name="input">The input; its last block may be partial.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />. It may be the same memory as
    /// <paramref name="input" />, but must not partially overlap it.
    /// </param>
    /// <exception cref="ObjectDisposedException">The cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="output" /> is shorter than <paramref name="input" />.
    /// </exception>
    /// <remarks>
    /// The counter block is the big-endian 128-bit integer <c>counterHigh · 2⁶⁴ + counterLow</c>, which increases by
    /// one for every whole or partial block, modulo <c>2¹²⁸</c>. Stopping at a wrap is the caller's to decide, by the
    /// length it passes.
    /// </remarks>
    void XorCounterKeystream(ref ulong counterHigh, ref ulong counterLow, ReadOnlySpan<byte> input, Span<byte> output);
}
