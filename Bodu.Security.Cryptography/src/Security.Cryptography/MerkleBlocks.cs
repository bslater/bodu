// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the block arithmetic shared by every consumer of <see cref="Rfc6962MerkleTree" />'s block mode — the
/// number of blocks a byte length divides into, and the offset and length of each one.
/// </summary>
/// <remarks>
/// <para>
/// A byte stream becomes an ordered sequence of tree entries by cutting it into fixed-size blocks. Every block but
/// the last is <c>blockSize</c> bytes; the last is short whenever the length is not a whole multiple, and is hashed
/// at its <em>actual</em> length rather than padded — padding would make a short final block indistinguishable from
/// a full block of the same bytes followed by zeros.
/// </para>
/// <para>
/// A zero-length input has <strong>zero</strong> blocks, not one empty block. Its root is therefore the empty
/// tree's — the hash of zero bytes — rather than the hash of one empty leaf.
/// </para>
/// <para>
/// These three functions are trivial and are nonetheless centralized here, because a consumer that computes the
/// final block's length incorrectly does not fail loudly — it produces a different, wrong root.
/// </para>
/// </remarks>
public static class MerkleBlocks
{
    /// <summary>
    /// Returns the number of blocks that an input of the specified length divides into.
    /// </summary>
    /// <param name="inputLength">The total length, in bytes, of the input.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>
    /// The number of blocks, which is zero when <paramref name="inputLength" /> is zero, and otherwise
    /// <c>ceil(inputLength / blockSize)</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="inputLength" /> is negative, or <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    public static long BlockCount(long inputLength, int blockSize)
    {
        ThrowHelper.ThrowIfNegative(inputLength);
        ThrowIfBlockSizeInvalid(blockSize);

        return (inputLength + blockSize - 1) / blockSize;
    }

    /// <summary>
    /// Returns the byte offset at which the specified block begins.
    /// </summary>
    /// <param name="blockIndex">The zero-based index of the block.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>The offset, in bytes, from the start of the input.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockIndex" /> is negative, or <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// The multiplication is performed in 64-bit arithmetic, so an offset beyond <see cref="int.MaxValue" /> is
    /// returned correctly rather than wrapping.
    /// </remarks>
    public static long BlockOffset(long blockIndex, int blockSize)
    {
        ThrowHelper.ThrowIfNegative(blockIndex);
        ThrowIfBlockSizeInvalid(blockSize);

        return blockIndex * blockSize;
    }

    /// <summary>
    /// Returns the length of the specified block, which is shorter than <paramref name="blockSize" /> only for the
    /// final block of an input whose length is not a whole multiple of it.
    /// </summary>
    /// <param name="inputLength">The total length, in bytes, of the input.</param>
    /// <param name="blockIndex">The zero-based index of the block.</param>
    /// <param name="blockSize">The size, in bytes, of each block.</param>
    /// <returns>
    /// The length, in bytes, of the block; zero when the block begins at or beyond
    /// <paramref name="inputLength" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="inputLength" /> or <paramref name="blockIndex" /> is negative, or
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    public static int BlockLength(long inputLength, long blockIndex, int blockSize)
    {
        ThrowHelper.ThrowIfNegative(inputLength);
        ThrowHelper.ThrowIfNegative(blockIndex);
        ThrowIfBlockSizeInvalid(blockSize);

        long offset = blockIndex * blockSize;
        if (offset >= inputLength)
            return 0;

        return (int)Math.Min(blockSize, inputLength - offset);
    }

    /// <summary>
    /// Throws when a block size is not a positive number of bytes.
    /// </summary>
    /// <param name="blockSize">The block size to validate.</param>
    /// <param name="paramName">The caller-supplied parameter name, captured automatically.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    internal static void ThrowIfBlockSizeInvalid(
        int blockSize,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(blockSize))] string? paramName = null)
    {
        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan,
                    0));
    }
}
