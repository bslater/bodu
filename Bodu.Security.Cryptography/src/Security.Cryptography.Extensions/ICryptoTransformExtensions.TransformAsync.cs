// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensions.TransformAsync.cs" company="PlaceholderCompany">
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

public static partial class ICryptoTransformExtensions
{
    /// <summary>
    /// Asynchronously applies a cryptographic transformation to data read from a source stream and writes the
    /// transformed output to a target stream.
    /// </summary>
    /// <param name="transform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="sourceStream">The stream to read untransformed data from. Must not be <see langword="null" />.</param>
    /// <param name="targetStream">The stream to write transformed data to. Must not be <see langword="null" />.</param>
    /// <param name="bufferSize">The buffer size, in bytes, used for streaming. Must be greater than zero.</param>
    /// <param name="cancellationToken">
    /// A token that may be used to cancel the operation before or during processing, including prior to finalisation.
    /// </param>
    /// <returns>A <see cref="Task" /> representing the asynchronous transformation operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transform" />, <paramref name="sourceStream" />, or <paramref name="targetStream" />
    /// is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cancellation is checked before the final block is flushed. If cancellation is requested after the last successful
    /// read but before finalisation, an <see cref="OperationCanceledException" /> is thrown and finalisation is skipped,
    /// leaving <paramref name="targetStream" /> in a partial state.
    /// </para>
    /// <para>
    /// Neither <paramref name="sourceStream" /> nor <paramref name="targetStream" /> is disposed by this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// using var input = File.OpenRead("input.bin");
    /// using var output = File.Create("encrypted.bin");
    /// await aes.CreateEncryptor().TransformAsync(input, output, 81920);
    ///]]>
    /// </code>
    /// </example>
    /// <exception cref="TaskCanceledException">Thrown when the operation is cancelled via the supplied cancellation token.</exception>
    public static async Task TransformAsync(
        this ICryptoTransform transform,
        Stream sourceStream,
        Stream targetStream,
        int bufferSize,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(transform);
        ThrowHelper.ThrowIfNull(sourceStream);
        ThrowHelper.ThrowIfNull(targetStream);
        ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

        // Use a pooled buffer cleared on return so plaintext read from sourceStream cannot leak
        // to a subsequent pool consumer.
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var cryptoStream = new CryptoStream(targetStream, transform, CryptoStreamMode.Write, leaveOpen: true);
            bool completed = false;

            try
            {
                int bytesRead;
                while (true)
                {
                    try
                    {
                        bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TaskCanceledException();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (bytesRead == 0)
                        break;

                    await cryptoStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken)
                        .ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                await cryptoStream.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);
                completed = true;
            }
            finally
            {
                if (completed)
                {
                    await cryptoStream.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    // Any failure before FlushFinalBlockAsync completed leaves the transform in
                    // an incomplete state; DisposeAsync will attempt to finalise the block and
                    // may raise a secondary CryptographicException that would mask the original
                    // cause (cancellation or I/O failure). Swallow that secondary exception so
                    // the primary exception propagates.
                    try { await cryptoStream.DisposeAsync().ConfigureAwait(false); }
                    catch { }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <summary>
    /// Asynchronously applies a cryptographic transformation to a memory region and writes the transformed result
    /// into a destination memory region.
    /// </summary>
    /// <param name="transform">The cryptographic transform to apply. Must not be <see langword="null" />.</param>
    /// <param name="input">The memory region containing the input data.</param>
    /// <param name="destination">The memory region to receive the transformed output.</param>
    /// <param name="cancellationToken">
    /// A token that may be used to cancel the operation before finalisation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}" /> whose result is the number of bytes written to <paramref name="destination" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transform" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the transformed output. A safe minimum size
    /// is <c>input.Length + transform.OutputBlockSize</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the internal transformed data buffer cannot be accessed after finalisation.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cancellation is checked before finalisation. If cancellation is requested, an
    /// <see cref="OperationCanceledException" /> is thrown and the content of <paramref name="destination" /> is
    /// undefined.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// using var aes = Aes.Create();
    /// using var encryptor = aes.CreateEncryptor();
    ///
    /// byte[] input = { 0x01, 0x02, 0x03 };
    /// byte[] buffer = new byte[64];
    ///
    /// int written = await encryptor.TransformAsync(input, buffer);
    ///]]>
    /// </code>
    /// </example>
    public static async Task<int> TransformAsync(
        this ICryptoTransform transform,
        ReadOnlyMemory<byte> input,
        Memory<byte> destination,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(transform);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination.Span, 0, input.Length + transform.OutputBlockSize);

        var ms = new MemoryStream(input.Length + transform.OutputBlockSize);
        CryptoStream? cryptoStream = null;
        bool completed = false;

        try
        {
            cryptoStream = new CryptoStream(ms, transform, CryptoStreamMode.Write, leaveOpen: true);

            await cryptoStream.WriteAsync(input, cancellationToken).ConfigureAwait(false);

            // Guard against cancellation between the write and finalisation.
            cancellationToken.ThrowIfCancellationRequested();

            await cryptoStream.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);

            ArraySegment<byte> segment = CryptoHelpers.GetBufferOrThrowIfInaccessible(ms);
            segment.AsSpan().CopyTo(destination.Span);
            completed = true;
            return segment.Count;
        }
        finally
        {
            // Dispose the CryptoStream (if it was successfully constructed) first, suppressing
            // any secondary CryptographicException from DisposeAsync-triggered finalisation when
            // the primary flow failed. Then always dispose the backing MemoryStream, even if
            // construction of the CryptoStream itself threw before the using block could
            // take ownership of it.
            if (cryptoStream is not null)
            {
                if (completed)
                {
                    await cryptoStream.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    try { await cryptoStream.DisposeAsync().ConfigureAwait(false); }
                    catch { }
                }
            }

            await ms.DisposeAsync().ConfigureAwait(false);
        }
    }
}
