// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.TryVerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Text;

namespace Bodu.IO.Hashing.Extensions;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of a stream against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The stream to read and hash. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value as a byte array. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static async Task<bool> TryVerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        byte[] expectedHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        if (stream == null || expectedHash == null)
            return false;

        try
        {
            return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of a stream against the expected hexadecimal hash string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The readable stream to hash asynchronously. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />;
    /// otherwise, <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not
    /// a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.
    /// </exception>
    public static async Task<bool> TryVerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        string expectedHex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(expectedHex);

        if (stream == null)
            return false;

        try
        {
            return await algorithm.VerifyHashAsync(stream, expectedHex, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of a stream against the expected hash value held in a
    /// memory buffer.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The stream to read and hash asynchronously. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a <see cref="ReadOnlyMemory{T}" /> of bytes.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static async Task<bool> TryVerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        ReadOnlyMemory<byte> expectedHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        if (stream == null)
            return false;

        try
        {
            return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of a byte array against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">The input data to hash. Must not be <see langword="null" />.</param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, or <paramref name="expectedHash" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <paramref name="input" /> is wrapped in a non-allocating <see cref="MemoryStream" /> and passed to the
    /// stream-based <see cref="VerifyHashAsync(NonCryptographicHashAlgorithm, Stream, byte[], CancellationToken)" />
    /// overload.
    /// </remarks>
    public static async Task<bool> TryVerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        byte[] input,
        byte[] expectedHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(expectedHash);

        try
        {
            using var stream = new MemoryStream(input, writable: false);
            return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of a byte array against the expected hexadecimal hash
    /// string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">The input data to hash. Must not be <see langword="null" />.</param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />;
    /// otherwise, <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not
    /// a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, or <paramref name="expectedHex" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <paramref name="input" /> is wrapped in a non-allocating <see cref="MemoryStream" /> and passed to the
    /// stream-based <see cref="VerifyHashAsync(NonCryptographicHashAlgorithm, Stream, string, CancellationToken)" />
    /// overload.
    /// </remarks>
    public static async Task<bool> TryVerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        byte[] input,
        string expectedHex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(expectedHex);

        try
        {
            using var stream = new MemoryStream(input, writable: false);
            return await algorithm.VerifyHashAsync(stream, expectedHex, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of an encoded string against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">The input string to encode and hash. Must not be <see langword="null" />.</param>
    /// <param name="encoding">
    /// The character encoding used to convert <paramref name="input" /> to bytes. Must not be <see langword="null" />.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, <paramref name="encoding" />, or
    /// <paramref name="expectedHash" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The encoded bytes are wrapped in a non-allocating <see cref="MemoryStream" /> and passed to the stream-based
    /// <see cref="VerifyHashAsync(NonCryptographicHashAlgorithm, Stream, byte[], CancellationToken)" /> overload.
    /// </remarks>
    public static async Task<bool> TryVerifyHashAsync(
        this NonCryptographicHashAlgorithm algorithm,
        string input,
        Encoding encoding,
        byte[] expectedHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(expectedHash);

        try
        {
            var inputBytes = encoding.GetBytes(input);
            using var stream = new MemoryStream(inputBytes, writable: false);
            return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }
}
