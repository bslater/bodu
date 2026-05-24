// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.VerifyHashAsync.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class HashAlgorithmExtensions
{
    /// <summary>
    /// Asynchronously verifies that the computed hash of a stream matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
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
    /// <remarks>
    /// <para>
    /// Comparison is performed using <see cref="CryptographicOperations.FixedTimeEquals" /> to mitigate timing
    /// side-channel attacks.
    /// </para>
    /// <para>
    /// If <paramref name="cancellationToken" /> is already canceled on entry, an
    /// <see cref="OperationCanceledException" /> is thrown immediately before any I/O begins.
    /// </para>
    /// </remarks>
    public static async Task<bool> VerifyHashAsync(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(expectedHash);

        // Throw OperationCanceledException directly for an already-canceled token.
        // ComputeHashAsync would otherwise throw the derived TaskCanceledException.
        cancellationToken.ThrowIfCancellationRequested();

        var actualHash = await algorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// Asynchronously verifies that the computed hash of a stream matches the expected hexadecimal hash string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="stream">The readable stream to hash asynchronously. Must not be <see langword="null" />.</param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. Case-insensitive. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />;
    /// otherwise, <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not
    /// a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="stream" />, or <paramref name="expectedHex" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="expectedHex" /> is decoded to bytes before comparison. The comparison is then performed using
    /// <see cref="CryptographicOperations.FixedTimeEquals" /> to mitigate timing side-channel attacks.
    /// </para>
    /// <para>
    /// A malformed <paramref name="expectedHex" /> string is treated as a non-match and returns
    /// <see langword="false" />.
    /// </para>
    /// <para>
    /// If <paramref name="cancellationToken" /> is already canceled on entry, an
    /// <see cref="OperationCanceledException" /> is thrown immediately before any I/O begins.
    /// </para>
    /// </remarks>
    public static async Task<bool> VerifyHashAsync(this HashAlgorithm algorithm, Stream stream, string expectedHex, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(expectedHex);

        // Throw OperationCanceledException directly for an already-canceled token.
        // ComputeHashAsync would otherwise throw the derived TaskCanceledException.
        cancellationToken.ThrowIfCancellationRequested();

        // Decode the expected hex to bytes for a constant-time binary comparison.
        // A malformed hex string cannot represent a valid hash, so treat it as a non-match.
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = await algorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedBytes);
    }

    /// <summary>
    /// Asynchronously verifies that the computed hash of a stream matches the expected hash value held in a memory
    /// buffer.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
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
    /// <remarks>
    /// <para>
    /// This overload supports allocation-reduced verification when the expected hash is already held in a
    /// <see cref="ReadOnlyMemory{T}" /> buffer. Comparison is performed using
    /// <see cref="CryptographicOperations.FixedTimeEquals" /> to mitigate timing side-channel attacks.
    /// </para>
    /// <para>
    /// If <paramref name="cancellationToken" /> is already canceled on entry, an
    /// <see cref="OperationCanceledException" /> is thrown immediately before any I/O begins.
    /// </para>
    /// </remarks>
    public static async Task<bool> VerifyHashAsync(this HashAlgorithm algorithm, Stream stream, ReadOnlyMemory<byte> expectedHash, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(stream);

        // Throw OperationCanceledException directly for an already-canceled token.
        // ComputeHashAsync would otherwise throw the derived TaskCanceledException.
        cancellationToken.ThrowIfCancellationRequested();

        var actualHash = await algorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash.Span);
    }
}
