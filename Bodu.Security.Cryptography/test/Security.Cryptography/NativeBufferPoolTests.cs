// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NativeBufferPoolTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="NativeBufferPool" />, the bounded, idle-trimmed pool of native buffers, grouped into
/// member-named partial files. The tests rent through <see cref="Argon2Matrix" />, the pool's first client; each uses
/// its own pool, so none observes another's buffers.
/// </summary>
[TestClass]
public sealed partial class NativeBufferPoolTests
{
    /// <summary>The number of bytes in a memory block.</summary>
    private const int BlockBytes = Argon2Matrix.WordsPerBlock * sizeof(ulong);

    /// <summary>An idle timeout long enough that the pool's own timer never fires during a test.</summary>
    private static readonly TimeSpan LongIdleTimeout = TimeSpan.FromHours(1);

    /// <summary>
    /// Creates a pool that retains up to four buffers of up to 1 MiB, released after an hour idle.
    /// </summary>
    /// <param name="timeProvider">The pool's clock; the system clock when <see langword="null" />.</param>
    /// <returns>The pool.</returns>
    private static NativeBufferPool CreatePool(TimeProvider? timeProvider = null) =>
        new(4, 1024 * 1024, LongIdleTimeout, timeProvider ?? TimeProvider.System);

    /// <summary>
    /// Fills every block of a matrix with a non-zero pattern.
    /// </summary>
    /// <param name="matrix">The matrix to fill.</param>
    private static void FillWithPattern(Argon2Matrix matrix)
    {
        for (int block = 0; block < matrix.BlockCount; block++)
            matrix.BlockSpan(block).Fill(0xA5A5_5A5A_F00D_BEEFUL ^ (ulong)block);
    }

    /// <summary>
    /// Determines whether every word of every block of a matrix is zero.
    /// </summary>
    /// <param name="matrix">The matrix to inspect.</param>
    /// <returns><see langword="true" /> if the matrix is all zero; otherwise, <see langword="false" />.</returns>
    private static bool IsAllZero(Argon2Matrix matrix)
    {
        for (int block = 0; block < matrix.BlockCount; block++)
        {
            if (matrix.BlockSpan(block).ContainsAnyExcept(0UL))
                return false;
        }

        return true;
    }
}
