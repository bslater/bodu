// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.TryVerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class HashAlgorithmExtensions
{
    /// <summary>
    /// Attempts to asynchronously compute and verify the hash of a stream against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
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
    public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);

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
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The readable stream to hash asynchronously. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />;
    /// otherwise, <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not
    /// a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, Stream stream, string expectedHex, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (stream == null || expectedHex == null)
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
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
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
    public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, Stream stream, ReadOnlyMemory<byte> expectedHash, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);

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
    /// Attempts to verify the hash of a byte array against the expected hash value, exposed as a
    /// <see cref="Task{TResult}" /> for API symmetry with the stream-based asynchronous overloads.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the task to resolve to <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value as a byte array. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="cancellationToken">
    /// Pre-checked before delegation. If already canceled, the task resolves to <see langword="false" />; the
    /// synchronous delegate cannot otherwise observe the token.
    /// </param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This overload performs no I/O. It delegates to <see cref="TryVerifyHash(HashAlgorithm, byte[], byte[])" /> and
    /// wraps the result in a completed <see cref="Task{TResult}" />, so the operation completes synchronously.
    /// </remarks>
    public static Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        return cancellationToken.IsCancellationRequested
            ? Task.FromResult(false)
            : Task.FromResult(algorithm.TryVerifyHash(input, expectedHash));
    }

    /// <summary>
    /// Attempts to verify the hash of a byte array against the expected hexadecimal hash string, exposed as a
    /// <see cref="Task{TResult}" /> for API symmetry with the stream-based asynchronous overloads.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the task to resolve to <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="cancellationToken">
    /// Pre-checked before delegation. If already canceled, the task resolves to <see langword="false" />; the
    /// synchronous delegate cannot otherwise observe the token.
    /// </param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />;
    /// otherwise, <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not
    /// a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This overload performs no I/O. It delegates to <see cref="TryVerifyHash(HashAlgorithm, byte[], string)" /> and
    /// wraps the result in a completed <see cref="Task{TResult}" />, so the operation completes synchronously.
    /// </remarks>
    public static Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, byte[] input, string expectedHex, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        return cancellationToken.IsCancellationRequested
            ? Task.FromResult(false)
            : Task.FromResult(algorithm.TryVerifyHash(input, expectedHex));
    }

    /// <summary>
    /// Attempts to verify the hash of an encoded string against the expected hash value, exposed as a
    /// <see cref="Task{TResult}" /> for API symmetry with the stream-based asynchronous overloads.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input string to encode and hash. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="encoding">
    /// The character encoding used to convert <paramref name="input" /> to bytes. A <see langword="null" /> value
    /// causes the task to resolve to <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value as a byte array. A <see langword="null" /> value causes the task to resolve to
    /// <see langword="false" />.
    /// </param>
    /// <param name="cancellationToken">
    /// Pre-checked before delegation. If already canceled, the task resolves to <see langword="false" />; the
    /// synchronous delegate cannot otherwise observe the token.
    /// </param>
    /// <returns>
    /// A task that evaluates to <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This overload performs no I/O. It delegates to
    /// <see cref="TryVerifyHash(HashAlgorithm, string, Encoding, byte[])" /> and wraps the result in a completed
    /// <see cref="Task{TResult}" />, so the operation completes synchronously.
    /// </remarks>
    public static Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, string input, Encoding encoding, byte[] expectedHash, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        return cancellationToken.IsCancellationRequested
            ? Task.FromResult(false)
            : Task.FromResult(algorithm.TryVerifyHash(input, encoding, expectedHash));
    }
}
