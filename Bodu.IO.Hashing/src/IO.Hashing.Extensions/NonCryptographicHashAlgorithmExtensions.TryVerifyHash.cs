// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.TryVerifyHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Text;

namespace Bodu.IO.Hashing.Extensions;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Attempts to compute and verify the hash of a byte array against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the method to return <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value to compare against. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        byte[] input,
        byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(expectedHash);

        if (input == null)
            return false;

        try
        {
            return algorithm.VerifyHash(input, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of a byte array against an expected hexadecimal hash string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the method to return <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />; otherwise,
    /// <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid
    /// hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        byte[] input,
        string expectedHex)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(expectedHex);

        if (input == null)
            return false;

        try
        {
            return algorithm.VerifyHash(input, expectedHex);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of a span of bytes against the expected hash span.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">The span of input bytes to hash.</param>
    /// <param name="expectedHash">The expected hash as a read-only byte span.</param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        ReadOnlySpan<byte> input,
        ReadOnlySpan<byte> expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        try
        {
            return algorithm.VerifyHash(input, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of a memory buffer against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">The memory buffer containing the input data to hash.</param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        ReadOnlyMemory<byte> input,
        byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(expectedHash);

        try
        {
            return algorithm.VerifyHash(input, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of a stream against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The input stream to read and hash. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value as a byte array. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the stream produces a matching hash; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (stream == null || expectedHash == null)
            return false;

        try
        {
            return algorithm.VerifyHash(stream, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of a stream against the expected hexadecimal hash string.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The input stream to read and hash. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash value as a hexadecimal string. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the stream hash matches <paramref name="expectedHex" />; otherwise,
    /// <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid
    /// hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        Stream stream,
        string expectedHex)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(expectedHex);

        if (stream == null)
            return false;

        try
        {
            return algorithm.VerifyHash(stream, expectedHex);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of a byte array, reporting both whether the operation succeeded and
    /// whether the hash matched.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the method to return <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value to compare against. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, contains <see langword="true" /> if the computed hash matched
    /// <paramref name="expectedHash" />; otherwise, <see langword="false" />. Always <see langword="false" /> when the
    /// method itself returns <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the hash computation and comparison completed without error; <see langword="false" />
    /// if <paramref name="input" /> or <paramref name="expectedHash" /> is <see langword="null" />, or an internal
    /// exception occurred.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Unlike <see cref="VerifyHash(NonCryptographicHashAlgorithm, byte[], byte[])" />, this overload distinguishes
    /// between a failed operation (return value <see langword="false" />) and a successful but non-matching comparison
    /// (<paramref name="result" /> = <see langword="false" />). Both <see langword="null" /> inputs are treated as an
    /// operation failure, making this overload suitable for defensive validation where inputs may be absent.
    /// </remarks>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        byte[] input,
        byte[] expectedHash,
        out bool result)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        result = false;

        if (input == null || expectedHash == null)
            return false;

        try
        {
            result = algorithm.VerifyHash(input, expectedHash);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to compute and verify the hash of an encoded string against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="input">The plain-text string to encode and hash. Must not be <see langword="null" />.</param>
    /// <param name="encoding">
    /// The encoding used to convert <paramref name="input" /> to bytes. Must not be <see langword="null" />.
    /// </param>
    /// <param name="expectedHash">The expected hash value as a byte array. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, <paramref name="encoding" />, or
    /// <paramref name="expectedHash" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(
        this NonCryptographicHashAlgorithm algorithm,
        string input,
        Encoding encoding,
        byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(input);
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(expectedHash);

        try
        {
            return algorithm.VerifyHash(input, encoding, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
