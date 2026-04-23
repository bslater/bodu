// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.AppendDataAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class HashAlgorithmExtensions
{
    /// <summary>
    /// Asynchronously reads all bytes from <paramref name="source" /> and feeds them into the
    /// hash accumulator via <see cref="HashAlgorithm.TransformBlock" />, without finalising the
    /// computation.
    /// </summary>
    /// <param name="algorithm">The hash algorithm to use. Must not be <see langword="null" />.</param>
    /// <param name="source">
    /// The stream whose bytes are appended to the current hash state. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="bufferSize">
    /// The number of bytes read per iteration. Must be greater than zero. Defaults to 4096.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the read loop. When signalled, the current
    /// <see cref="Stream.ReadAsync" /> is cancelled and <see cref="OperationCanceledException" />
    /// is propagated to the caller.
    /// </param>
    /// <returns>A <see cref="Task" /> that completes when all bytes have been fed into the accumulator.</returns>
    /// <remarks>
    /// <para>
    /// This method is the asynchronous counterpart to
    /// <see cref="AppendData(HashAlgorithm, System.ReadOnlySpan{byte})" />. It allows large or
    /// streaming sources to be incorporated into an incremental hash computation without blocking
    /// the calling thread.
    /// </para>
    /// <para>
    /// Because only <see cref="HashAlgorithm.TransformBlock" /> is called, the hash state is
    /// not finalised after this method returns. The caller is responsible for calling
    /// <see cref="HashAlgorithm.TransformFinalBlock" /> when all data has been supplied.
    /// </para>
    /// <para>
    /// Multiple <see cref="AppendDataAsync" /> calls — and calls interleaved with the synchronous
    /// <see cref="AppendData(HashAlgorithm, System.ReadOnlySpan{byte})" /> — accumulate correctly
    /// because all of them delegate to <see cref="HashAlgorithm.TransformBlock" />.
    /// </para>
    /// <para>
    /// The read buffer is rented from <see cref="ArrayPool{T}.Shared" /> and returned — with
    /// its contents zeroed — in all exit paths, including cancellation and exception propagation.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was signalled before or during the read loop.
    /// </exception>
    /// <exception cref="IOException">
    /// <paramref name="source" /> threw an <see cref="IOException" /> during a read.
    /// </exception>
    public static async Task AppendDataAsync(
        this HashAlgorithm algorithm,
        Stream source,
        int bufferSize = 4096,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(source);

        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(bufferSize), bufferSize, "Buffer size must be greater than zero.");

        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await source
                .ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)
                .ConfigureAwait(false)) > 0)
            {
                algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
        }
        finally
        {
            // Zero sensitive data before returning the buffer to the pool.
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
