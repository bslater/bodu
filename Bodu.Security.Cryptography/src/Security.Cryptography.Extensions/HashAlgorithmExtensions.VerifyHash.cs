// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.VerifyHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class HashAlgorithmExtensions
{
    /// <summary>
    /// Verifies that the computed hash of the input data matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> instance used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="input">The input byte array whose hash will be computed. Must not be <see langword="null"/>.</param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the computed hash equals <paramref name="expectedHash"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm"/>, <paramref name="input"/>, or <paramref name="expectedHash"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Comparison is performed using <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(input);
        ThrowHelper.ThrowIfNull(expectedHash);

        var actualHash = algorithm.ComputeHash(input);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the input data matches the expected hash value expressed as a hexadecimal string.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> instance used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="input">The input byte array whose hash will be computed. Must not be <see langword="null"/>.</param>
    /// <param name="expectedHex">
    /// The expected hash value as a hexadecimal string. Case-insensitive. Must not be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the computed hash matches <paramref name="expectedHex"/>; otherwise, <see langword="false"/>.
    /// Returns <see langword="false"/> if <paramref name="expectedHex"/> is not a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm"/>, <paramref name="input"/>, or <paramref name="expectedHex"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="expectedHex"/> is decoded to bytes before comparison. The comparison is then performed using
    /// <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </para>
    /// <para>
    /// A malformed <paramref name="expectedHex"/> string (one that cannot be decoded) is treated as a non-match and
    /// returns <see langword="false"/>.
    /// </para>
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, byte[] input, string expectedHex)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(input);
        ThrowHelper.ThrowIfNull(expectedHex);

        // Decode the expected hex to bytes BEFORE computing the hash so a malformed hex string fails fast
        // without wasting work hashing the input. A malformed hex string cannot represent a valid hash,
        // so treat it as a non-match.
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = algorithm.ComputeHash(input);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedBytes);
    }

    /// <summary>
    /// Verifies that the computed hash of the stream matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> instance used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="stream">
    /// The input stream to read and hash. Must not be <see langword="null"/> and must be readable.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the hash of the stream matches <paramref name="expectedHash"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm"/>, <paramref name="stream"/>, or <paramref name="expectedHash"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Comparison is performed using <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(expectedHash);

        var actualHash = algorithm.ComputeHash(stream);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the stream matches the expected hash value expressed as a hexadecimal string.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> instance used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="stream">
    /// The input stream to read and hash. Must not be <see langword="null"/> and must be readable.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash value as a hexadecimal string. Case-insensitive. Must not be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the hash of the stream matches <paramref name="expectedHex"/>; otherwise, <see langword="false"/>.
    /// Returns <see langword="false"/> if <paramref name="expectedHex"/> is not a valid hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm"/>, <paramref name="stream"/>, or <paramref name="expectedHex"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <paramref name="expectedHex"/> is decoded to bytes before comparison. The comparison is then performed using
    /// <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </para>
    /// <para>
    /// A malformed <paramref name="expectedHex"/> string is treated as a non-match and returns <see langword="false"/>.
    /// </para>
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, Stream stream, string expectedHex)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(expectedHex);

        // Decode the expected hex to bytes BEFORE reading and hashing the stream so a malformed hex string
        // fails fast without consuming stream data or wasting the hash computation. A malformed hex string
        // cannot represent a valid hash, so treat it as a non-match. This matches the behavior of the
        // asynchronous stream overload.
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHex);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = algorithm.ComputeHash(stream);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedBytes);
    }

    /// <summary>
    /// Verifies that the computed hash of the input span matches the expected hash span.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="input">The input span of bytes to hash.</param>
    /// <param name="expectedHash">The expected hash as a read-only span of bytes.</param>
    /// <returns>
    /// <see langword="true"/> if the computed hash equals <paramref name="expectedHash"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This overload uses <see cref="HashAlgorithm.TryComputeHash"/> with an <see cref="ArrayPool{T}"/>-backed output buffer
    /// to avoid heap allocation for the computed hash. The buffer is cleared before being returned to the pool.
    /// </para>
    /// <para>
    /// Comparison is performed using <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </para>
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, ReadOnlySpan<byte> input, ReadOnlySpan<byte> expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        var hashLength = algorithm.HashSize >> 3;
        var actualBuffer = ArrayPool<byte>.Shared.Rent(hashLength);
        try
        {
            if (!algorithm.TryComputeHash(input, actualBuffer.AsSpan(0, hashLength), out var bytesWritten))
                return false;

            return CryptographicOperations.FixedTimeEquals(actualBuffer.AsSpan(0, bytesWritten), expectedHash);
        }
        finally
        {
            // Clear hash bytes from pooled memory before returning to prevent leaking sensitive data.
            ArrayPool<byte>.Shared.Return(actualBuffer, clearArray: true);
        }
    }

    /// <summary>
    /// Verifies that the computed hash of the input memory block matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="input">The memory buffer containing the input data to hash.</param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the hash of <paramref name="input"/> equals <paramref name="expectedHash"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm"/> or <paramref name="expectedHash"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Delegates to the <see cref="VerifyHash(HashAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/> overload. Comparison is
    /// performed using <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, ReadOnlyMemory<byte> input, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(expectedHash);

        return algorithm.VerifyHash(input.Span, expectedHash);
    }

    /// <summary>
    /// Verifies that the computed hash of the encoded string matches the expected hash value.
    /// </summary>
    /// <param name="algorithm">The <see cref="HashAlgorithm"/> used to compute the hash. Must not be <see langword="null"/>.</param>
    /// <param name="text">The input string to encode and hash. Must not be <see langword="null"/>.</param>
    /// <param name="encoding">
    /// The encoding used to convert <paramref name="text"/> to bytes. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="expectedHash">The expected hash as a byte array. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the hash of the encoded string equals <paramref name="expectedHash"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm"/>, <paramref name="text"/>, <paramref name="encoding"/>, or
    /// <paramref name="expectedHash"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Comparison is performed using <see cref="CryptographicOperations.FixedTimeEquals"/> to mitigate timing side-channel attacks.
    /// </remarks>
    public static bool VerifyHash(this HashAlgorithm algorithm, string text, Encoding encoding, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(expectedHash);

        var data = encoding.GetBytes(text);

        return algorithm.VerifyHash(data, expectedHash);
    }
}
