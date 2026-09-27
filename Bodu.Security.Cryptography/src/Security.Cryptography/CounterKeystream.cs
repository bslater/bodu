// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CounterKeystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the keystream layer the counter-based modes share: a run of counter blocks, built by the mode under its own
/// increment rule, encrypted through one <see cref="IBlockCipher.EncryptBlocks" /> call and combined with the input.
/// </summary>
/// <remarks>
/// <para>
/// Encrypting counters a run at a time rather than a block at a time lets a cipher amortize its per-call cost — for
/// <see cref="AesBlockCipher" />, one call into the platform's AES for the whole run instead of one per 16 bytes —
/// while a cipher that keeps the default <see cref="IBlockCipher.EncryptBlocks" /> still encrypts one block per call,
/// exactly as before.
/// </para>
/// <para>
/// The modes keep their counters, increment rules, and wrap checks; they build each run of counters and stop it at the
/// point their per-block loop would have stopped, so output and exceptions match the per-block formulation byte for
/// byte, including the output already written when a wrap check throws.
/// </para>
/// </remarks>
internal static class CounterKeystream
{
    /// <summary>The largest run, in bytes, of counter blocks encrypted in one call.</summary>
    internal const int BatchBytes = 4096;

    /// <summary>
    /// Returns the length of a run: the largest whole number of blocks that fits in <see cref="BatchBytes" />, and at
    /// least one block.
    /// </summary>
    /// <param name="blockBytes">The cipher's block size, in bytes.</param>
    /// <returns>The run length, in bytes.</returns>
    internal static int BatchLength(int blockBytes) =>
        Math.Max(blockBytes, BatchBytes / blockBytes * blockBytes);

    /// <summary>
    /// Encrypts a run of counter blocks and writes the keystream, combined with <paramref name="input" />, into
    /// <paramref name="output" />.
    /// </summary>
    /// <param name="cipher">The cipher whose encrypt primitive produces the keystream.</param>
    /// <param name="counters">The run of counter blocks, a whole number of blocks.</param>
    /// <param name="keystream">
    /// Scratch space at least as long as <paramref name="counters" />; receives the keystream.
    /// </param>
    /// <param name="input">The input to combine, no longer than <paramref name="counters" />.</param>
    /// <param name="output">The destination, at least as long as <paramref name="input" />.</param>
    internal static void Apply(IBlockCipher cipher, ReadOnlySpan<byte> counters, Span<byte> keystream, ReadOnlySpan<byte> input, Span<byte> output)
    {
        cipher.EncryptBlocks(counters, keystream[..counters.Length]);
        CryptographyHelper.Xor(input, keystream, output);
    }
}
