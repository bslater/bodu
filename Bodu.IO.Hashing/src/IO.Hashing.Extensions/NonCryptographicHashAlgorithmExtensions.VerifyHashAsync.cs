// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.VerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Extensions;

using System;
using System.IO;
using System.IO.Hashing;
using System.Threading;
using System.Threading.Tasks;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Asynchronously verifies that the computed hash of a stream matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The stream to read and hash asynchronously. Must not be <see langword="null" /> and must be readable.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash equals <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="stream" />, or <paramref name="expectedHash" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was signalled before or during stream reading.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The algorithm state is reset before the stream is read. Any prior incremental state is discarded.
    /// </para>
    /// <para>
    /// If <paramref name="cancellationToken" /> is already cancelled on entry, an
    /// <see cref="OperationCanceledException" /> is thrown immediately before any I/O begins.
    /// </para>
    /// </remarks>
    public static async Task<bool> VerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        byte[] expectedHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(expectedHash);

        cancellationToken.ThrowIfCancellationRequested();

        algorithm.Reset();
        await algorithm.AppendDataAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedHash);
    }

    /// <summary>
    /// Asynchronously verifies that the computed hash of a stream matches the expected hexadecimal hash string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">The readable stream to hash asynchronously. Must not be <see langword="null" />.</param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. Case-insensitive. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />;
    /// otherwise, <see langword="false" />.
    /// Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="stream" />, or <paramref name="expectedHex" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was signalled before or during stream reading.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="expectedHex" /> is decoded to bytes before the stream is read so that a malformed hex string
    /// fails fast without consuming stream data. A malformed <paramref name="expectedHex" /> string is treated as a
    /// non-match and returns <see langword="false" />.
    /// </para>
    /// <para>
    /// If <paramref name="cancellationToken" /> is already cancelled on entry, an
    /// <see cref="OperationCanceledException" /> is thrown immediately before any I/O begins.
    /// </para>
    /// </remarks>
    public static async Task<bool> VerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        string expectedHex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(expectedHex);

        cancellationToken.ThrowIfCancellationRequested();

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        algorithm.Reset();
        await algorithm.AppendDataAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedBytes);
    }

    /// <summary>
    /// Asynchronously verifies that the computed hash of a stream matches the expected hash value held in a memory buffer.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">The readable stream to hash asynchronously. Must not be <see langword="null" />.</param>
    /// <param name="expectedHash">The expected hash value as a <see cref="ReadOnlyMemory{T}" /> of bytes.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash equals <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was signalled before or during stream reading.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload supports allocation-reduced verification when the expected hash is already held in a
    /// <see cref="ReadOnlyMemory{T}" /> buffer.
    /// </para>
    /// <para>
    /// If <paramref name="cancellationToken" /> is already cancelled on entry, an
    /// <see cref="OperationCanceledException" /> is thrown immediately before any I/O begins.
    /// </para>
    /// </remarks>
    public static async Task<bool> VerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        ReadOnlyMemory<byte> expectedHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(stream);

        cancellationToken.ThrowIfCancellationRequested();

        algorithm.Reset();
        await algorithm.AppendDataAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        byte[] actualHash = algorithm.GetHashAndReset();

        return actualHash.AsSpan().SequenceEqual(expectedHash.Span);
    }
}
