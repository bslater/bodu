// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Matrix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Owns the memory matrix of one Argon2 derivation — its blocks of 128 64-bit words — and clears it when released.
/// </summary>
/// <remarks>
/// Every block of a derivation is written before anything reads it (RFC 9106 fills the first two columns from <c>H'</c>
/// and every later block before it can be referenced), so a matrix is never required to start zeroed. It is always
/// cleared on release, because every word in it is derived from the password.
/// </remarks>
internal sealed class Argon2Matrix
    : IDisposable
{
    /// <summary>The number of 64-bit words in a 1024-byte memory block.</summary>
    internal const int WordsPerBlock = 128;

    /// <summary>The matrix words, or <see langword="null" /> once the matrix has been released.</summary>
    private ulong[]? _words;

    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2Matrix" /> class with the specified number of blocks.
    /// </summary>
    /// <param name="blockCount">The number of 1024-byte blocks.</param>
    private Argon2Matrix(int blockCount)
    {
        _words = new ulong[blockCount * WordsPerBlock];
        BlockCount = blockCount;
    }

    /// <summary>
    /// Gets the number of blocks in the matrix.
    /// </summary>
    internal int BlockCount { get; }

    /// <summary>
    /// Obtains a matrix of the specified number of blocks for one derivation.
    /// </summary>
    /// <param name="blockCount">The number of 1024-byte blocks, <c>m'</c>.</param>
    /// <returns>A matrix the caller owns and must dispose.</returns>
    internal static Argon2Matrix Rent(int blockCount) =>
        new(blockCount);

    /// <summary>
    /// Returns a reference to the first word of the block at the specified absolute index.
    /// </summary>
    /// <param name="index">The zero-based absolute block index (<c>lane * laneLength + column</c>).</param>
    /// <returns>A reference to the block's first word; the block's 128 words follow it contiguously.</returns>
    internal ref ulong Block(int index) =>
        ref _words![index * WordsPerBlock];

    /// <summary>
    /// Returns the 128 words of the block at the specified absolute index.
    /// </summary>
    /// <param name="index">The zero-based absolute block index.</param>
    /// <returns>The block's words.</returns>
    internal Span<ulong> BlockSpan(int index) =>
        _words.AsSpan(index * WordsPerBlock, WordsPerBlock);

    /// <summary>
    /// Clears every word of the matrix and releases it.
    /// </summary>
    public void Dispose()
    {
        if (_words is null)
            return;

        CryptographyHelper.Clear(_words);
        _words = null;
    }
}
