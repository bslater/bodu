// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Asynchronously computes the hash value for the specified stream.
    /// </summary>
    /// <param name="algorithm">The hash algorithm instance.</param>
    /// <param name="source">The stream whose bytes are hashed.</param>
    /// <param name="bufferSize">The number of bytes read per iteration. Defaults to 81920.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous read operation.</param>
    /// <returns>The computed hash value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async ValueTask<byte[]> ComputeHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream source,
        int bufferSize = 81920,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(source);
        ThrowHelper.ThrowIfZeroOrNegative(bufferSize);

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false)) > 0)
                algorithm.Append(buffer.AsSpan(0, bytesRead));

            return algorithm.GetHashAndReset();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
