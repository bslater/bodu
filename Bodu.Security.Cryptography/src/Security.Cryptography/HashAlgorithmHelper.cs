// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmHelper.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides high-performance utility methods for one-shot hashing using factory-created <see cref="HashAlgorithm"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// These methods simplify hashing workflows by accepting an <see cref="IHashAlgorithmFactory{T}"/> implementation, allowing
/// consumers to construct and configure hash algorithms (including keyed or parameterized variants) without managing lifecycle manually.
/// </para>
/// <para>
/// This is ideal for use cases that require stateless or ephemeral hashing operations without incremental updates or state reuse.
/// Each call constructs a fresh algorithm instance, runs the hash, and disposes deterministically — callers do not need to
/// track <see cref="System.IDisposable"/> lifetimes themselves.
/// </para>
/// <para>
/// <strong>When to choose this over the BCL.</strong> Pick <see cref="HashAlgorithmHelper"/> when the algorithm
/// requires per-call configuration (a key, round counts, a variant flag) — the factory consistently applies
/// it to every fresh instance. For stateless one-shot hashing of unconfigured algorithms (SHA-256, SHA-512)
/// the BCL's static <c>HashData</c> on each algorithm class is simpler and faster. For tree-hashing workloads
/// pass the same factory into <see cref="MerkleTreeHash"/> or <see cref="ParallelMerkleTreeHash"/>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Configured SipHash-2-4 factory; the configuration applies to every call to HashData.
/// var factory = HashAlgorithmFactory.From(() => new SipHash64
/// {
///     Key = key,
///     CompressionRounds = 2,
///     FinalizationRounds = 4,
/// });
/// byte[] hash = HashAlgorithmHelper.HashData(factory, input);
///]]>
/// </code>
/// </example>
/// <seealso cref="IHashAlgorithmFactory{T}"/>
/// <seealso cref="HashAlgorithmFactory"/>
/// <seealso cref="DelegateHashAlgorithmFactory{T}"/>
public static class HashAlgorithmHelper
{
    /// <summary>
    /// Computes the hash for the given input using a factory-created algorithm.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="HashAlgorithm"/>.</typeparam>
    /// <param name="factory">The factory used to create the hash algorithm.</param>
    /// <param name="input">The input data to hash.</param>
    /// <returns>The computed hash as a byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    public static byte[] HashData<T>(IHashAlgorithmFactory<T> factory, ReadOnlySpan<byte> input)
        where T : System.Security.Cryptography.HashAlgorithm
    {
        ThrowHelper.ThrowIfNull(factory);

        using T algorithm = factory.Create();
        var hashSizeBytes = algorithm.HashSize >> 3;
        var result = new byte[hashSizeBytes];
        if (!algorithm.TryComputeHash(input, result, out var bytesWritten))
            throw new CryptographicException("The hash algorithm's destination buffer was too small.");
        if (bytesWritten == result.Length)
            return result;

        var trimmed = new byte[bytesWritten];
        Buffer.BlockCopy(result, 0, trimmed, 0, bytesWritten);
        return trimmed;
    }

    /// <summary>
    /// Computes the hash of a stream using a factory-created algorithm.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="HashAlgorithm"/>.</typeparam>
    /// <param name="factory">The factory used to create the hash algorithm.</param>
    /// <param name="stream">The stream to hash.</param>
    /// <returns>The computed hash as a byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> or <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    public static byte[] HashData<T>(IHashAlgorithmFactory<T> factory, Stream stream)
        where T : System.Security.Cryptography.HashAlgorithm
    {
        ThrowHelper.ThrowIfNull(factory);
        ThrowHelper.ThrowIfNull(stream);

        using T algorithm = factory.Create();
        AppendDataFromStreamInternalAsync(algorithm, stream, isAsync: false, default).GetAwaiter().GetResult();

        return algorithm.Hash ?? throw new CryptographicException("Hash algorithm did not produce a value.");
    }

    /// <summary>
    /// Asynchronously computes the hash of a stream using a factory-created algorithm.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="HashAlgorithm"/>.</typeparam>
    /// <param name="factory">The factory used to create the hash algorithm.</param>
    /// <param name="stream">The stream to hash.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task representing the asynchronous hash computation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> or <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    public static async ValueTask<byte[]> HashDataAsync<T>(
        IHashAlgorithmFactory<T> factory,
        Stream stream,
        CancellationToken cancellationToken = default)
        where T : System.Security.Cryptography.HashAlgorithm
    {
        ThrowHelper.ThrowIfNull(factory);
        ThrowHelper.ThrowIfNull(stream);

        using T algorithm = factory.Create();
        await AppendDataFromStreamInternalAsync(algorithm, stream, isAsync: true, cancellationToken).ConfigureAwait(false);

        return algorithm.Hash ?? throw new CryptographicException("Hash algorithm did not produce a value.");
    }

    /// <summary>
    /// Attempts to compute the hash and write it to the specified destination buffer.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="HashAlgorithm"/>.</typeparam>
    /// <param name="factory">The factory used to create the hash algorithm.</param>
    /// <param name="input">The input data to hash.</param>
    /// <param name="destination">The buffer to receive the hash value.</param>
    /// <param name="bytesWritten">Receives the number of bytes written to <paramref name="destination"/>.</param>
    /// <returns><see langword="true"/> if the hash fits in the destination buffer; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is <see langword="null"/>.</exception>
    public static bool TryHashData<T>(
        IHashAlgorithmFactory<T> factory,
        ReadOnlySpan<byte> input,
        Span<byte> destination,
        out int bytesWritten)
        where T : System.Security.Cryptography.HashAlgorithm
    {
        ThrowHelper.ThrowIfNull(factory);

        using T algorithm = factory.Create();
        return algorithm.TryComputeHash(input, destination, out bytesWritten);
    }

    /// <summary>
    /// Reads all bytes from a stream and feeds them into the hash algorithm incrementally.
    /// </summary>
    /// <param name="algorithm">The hash algorithm receiving the data.</param>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="isAsync">Indicates whether the stream should be read asynchronously.</param>
    /// <param name="cancellationToken">The optional cancellation token for async operations.</param>
    /// <returns>A task representing the completion of the data append operation.</returns>
    /// <remarks>
    /// This method is used by both <see cref="HashData{T}(IHashAlgorithmFactory{T}, Stream)"/> and
    /// <see cref="HashDataAsync{T}(IHashAlgorithmFactory{T}, Stream, CancellationToken)"/> to centralise the stream-to-hash logic.
    /// </remarks>
    private static async ValueTask AppendDataFromStreamInternalAsync(HashAlgorithm algorithm, Stream stream, bool isAsync, CancellationToken cancellationToken)
    {
        // Rent a reusable buffer from the shared pool
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            int bytesRead;

            // Use async path if requested
            if (isAsync)
            {
                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    // Feed buffer segment into the hash algorithm
                    algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }
            else
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }

            // Finalize the hash to flush internal state
            algorithm.TransformFinalBlock([], 0, 0);
        }
        finally
        {
            // Always return buffer to the pool, clearing any plaintext to prevent leaking
            // caller secrets to the next pool consumer.
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
