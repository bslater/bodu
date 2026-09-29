// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Matrix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Owns the memory matrix of one Argon2 derivation — its blocks of 128 64-bit words — in native memory, and clears it
/// when released.
/// </summary>
/// <remarks>
/// <para>
/// The matrix lives outside the collected heap, in a buffer taken from <see cref="NativeBufferPool" />, so a derivation
/// neither allocates a large managed array nor leaves one for a gen2 collection. On disposal every block the derivation
/// could have written is cleared before the buffer goes back to the pool, which is what keeps every pooled buffer all
/// zero.
/// </para>
/// <para>
/// Every block of a derivation is written before anything reads it: RFC 9106 fills the first two columns from <c>H'</c>
/// and every later block before it can be referenced. A matrix therefore never needs to start zeroed, and a freshly
/// allocated, uninitialized buffer is as good as a pooled one.
/// </para>
/// </remarks>
internal sealed unsafe class Argon2Matrix
    : IDisposable
{
    /// <summary>The number of 64-bit words in a 1024-byte memory block.</summary>
    internal const int WordsPerBlock = 128;

    /// <summary>The number of bytes in a memory block.</summary>
    private const int BlockBytes = WordsPerBlock * sizeof(ulong);

    /// <summary>The pool the buffer came from and returns to.</summary>
    private readonly NativeBufferPool _pool;

    /// <summary>The size of the buffer, in bytes, which may exceed the blocks in use.</summary>
    private readonly nuint _capacity;

    /// <summary>The matrix words, or <see langword="null" /> once the matrix has been released.</summary>
    private ulong* _words;

    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2Matrix" /> class over a buffer taken from a pool.
    /// </summary>
    /// <param name="pool">The pool the buffer came from.</param>
    /// <param name="words">The buffer.</param>
    /// <param name="capacity">The buffer's size, in bytes.</param>
    /// <param name="blockCount">The number of blocks in use.</param>
    private Argon2Matrix(NativeBufferPool pool, ulong* words, nuint capacity, int blockCount)
    {
        _pool = pool;
        _words = words;
        _capacity = capacity;
        BlockCount = blockCount;
    }

    /// <summary>
    /// Gets the number of blocks in the matrix.
    /// </summary>
    internal int BlockCount { get; }

    /// <summary>
    /// Obtains a matrix of the specified number of blocks from the shared pool.
    /// </summary>
    /// <param name="blockCount">The number of 1024-byte blocks, <c>m'</c>.</param>
    /// <returns>A matrix the caller owns and must dispose.</returns>
    /// <exception cref="OutOfMemoryException">The matrix cannot be allocated.</exception>
    internal static Argon2Matrix Rent(int blockCount) =>
        Rent(blockCount, NativeBufferPool.Shared);

    /// <summary>
    /// Obtains a matrix of the specified number of blocks from the specified pool.
    /// </summary>
    /// <param name="blockCount">The number of 1024-byte blocks, <c>m'</c>.</param>
    /// <param name="pool">The pool to take the buffer from and return it to.</param>
    /// <returns>A matrix the caller owns and must dispose.</returns>
    /// <exception cref="OutOfMemoryException">The matrix cannot be allocated.</exception>
    internal static Argon2Matrix Rent(int blockCount, NativeBufferPool pool)
    {
        byte* buffer = pool.Rent((nuint)blockCount * BlockBytes, out nuint capacity);
        return new Argon2Matrix(pool, (ulong*)buffer, capacity, blockCount);
    }

    /// <summary>
    /// Returns a reference to the first word of the block at the specified absolute index.
    /// </summary>
    /// <param name="index">The zero-based absolute block index (<c>lane * laneLength + column</c>).</param>
    /// <returns>A reference to the block's first word; the block's 128 words follow it contiguously.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the matrix.</exception>
    /// <remarks>
    /// The index is checked even though the fill only computes valid ones: the matrix is native memory, so a wrong
    /// index must fail rather than read or write outside it.
    /// </remarks>
    internal ref ulong Block(int index)
    {
        ThrowHelper.ThrowIfGreaterThanOrEqual((uint)index, (uint)BlockCount, nameof(index));

        return ref _words[(nuint)(uint)index * WordsPerBlock];
    }

    /// <summary>
    /// Returns the 128 words of the block at the specified absolute index.
    /// </summary>
    /// <param name="index">The zero-based absolute block index.</param>
    /// <returns>The block's words.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the matrix.</exception>
    internal Span<ulong> BlockSpan(int index) =>
        MemoryMarshal.CreateSpan(ref Block(index), WordsPerBlock);

    /// <summary>
    /// Clears every block in use and returns the buffer to its pool.
    /// </summary>
    public void Dispose()
    {
        ulong* words = _words;
        if (words is null)
            return;

        _words = null;

        // Only the blocks in use can hold anything; the rest of a larger pooled buffer is still zero from its last use.
        NativeBufferPool.Clear((byte*)words, (nuint)BlockCount * BlockBytes);
        _pool.Return((byte*)words, _capacity);
    }
}
