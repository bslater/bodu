// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.TryVerifyHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class HashAlgorithmExtensions
{
    /// <summary>
    /// Attempts to compute and verify the hash of a byte array against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the method to return <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value to compare against. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (input == null || expectedHash == null)
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
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The input data to hash. A <see langword="null" /> value causes the method to return <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash as a hexadecimal string. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHex" />; otherwise,
    /// <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid
    /// hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(this HashAlgorithm algorithm, byte[] input, string expectedHex)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (input == null || expectedHex == null)
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
    /// Attempts to compute and verify the hash of an encoded string against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">
    /// The plain-text string to encode and hash. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <param name="encoding">
    /// The encoding used to convert <paramref name="input" /> to bytes. A <see langword="null" /> value causes the
    /// method to return <see langword="false" />.
    /// </param>
    /// <param name="expectedHash">
    /// The expected hash value as a byte array. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(this HashAlgorithm algorithm, string input, Encoding encoding, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (input == null || encoding == null || expectedHash == null)
            return false;

        try
        {
            return algorithm.VerifyHash(input, encoding, expectedHash);
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
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
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
    public static bool TryVerifyHash(this HashAlgorithm algorithm, ReadOnlySpan<byte> input, ReadOnlySpan<byte> expectedHash)
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
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="input">The memory buffer containing the input data to hash.</param>
    /// <param name="expectedHash">
    /// The expected hash value as a byte array. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the computed hash matches <paramref name="expectedHash" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(this HashAlgorithm algorithm, ReadOnlyMemory<byte> input, byte[] expectedHash)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (expectedHash == null)
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
    /// Attempts to compute and verify the hash of a stream against the expected hash value.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
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
    public static bool TryVerifyHash(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash)
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
    /// The <see cref="HashAlgorithm" /> instance used to compute the hash. Must not be <see langword="null" />.
    /// </param>
    /// <param name="stream">
    /// The input stream to read and hash. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <param name="expectedHex">
    /// The expected hash value as a hexadecimal string. A <see langword="null" /> value causes the method to return
    /// <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the stream hash matches <paramref name="expectedHex" />; otherwise,
    /// <see langword="false" />. Returns <see langword="false" /> if <paramref name="expectedHex" /> is not a valid
    /// hexadecimal string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public static bool TryVerifyHash(this HashAlgorithm algorithm, Stream stream, string expectedHex)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (stream == null || expectedHex == null)
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
}
