// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Extensions;

using System;
using System.Buffers;
using System.IO;
using System.IO.Hashing;
using Bodu;

public static partial class NonCryptographicHashAlgorithmExtensions
{

    /// <summary>
    /// Computes the hash value for the specified byte array.
    /// </summary>
    /// <param name="algorithm">The hash algorithm instance.</param>
    /// <param name="buffer">The input bytes to hash.</param>
    /// <returns>The computed hash value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="buffer" /> is <see langword="null" />.
    /// </exception>
    public static byte[] ComputeHash(this NonCryptographicHashAlgorithm algorithm, byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(buffer);

        algorithm.Append(buffer);
        return algorithm.GetHashAndReset();
    }

    /// <summary>
    /// Computes the hash value for the specified span of bytes.
    /// </summary>
    /// <param name="algorithm">The hash algorithm instance.</param>
    /// <param name="data">The input bytes to hash.</param>
    /// <returns>The computed hash value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="algorithm" /> is <see langword="null" />.</exception>
    public static byte[] ComputeHash(
        this NonCryptographicHashAlgorithm algorithm,
        ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        algorithm.Append(data);
        return algorithm.GetHashAndReset();
    }

    /// <summary>
    /// Computes the hash value for the specified stream.
    /// </summary>
    /// <param name="algorithm">The hash algorithm instance.</param>
    /// <param name="source">The stream whose bytes are hashed.</param>
    /// <param name="bufferSize">The number of bytes read per iteration. Defaults to 4096.</param>
    /// <returns>The computed hash value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    public static byte[] ComputeHash(
        this NonCryptographicHashAlgorithm algorithm,
        Stream source,
        int bufferSize = 4096)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(source);
        ThrowHelper.ThrowIfZeroOrNegative(bufferSize);

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, bufferSize)) > 0)
                algorithm.Append(buffer.AsSpan(0, bytesRead));

            return algorithm.GetHashAndReset();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Computes the hash value for the specified region of the byte array.
    /// </summary>
    /// <param name="algorithm">The hash algorithm instance.</param>
    /// <param name="buffer">The input bytes to hash.</param>
    /// <param name="offset">The offset into <paramref name="buffer" /> at which hashing begins.</param>
    /// <param name="count">The number of bytes to hash.</param>
    /// <returns>The computed hash value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="buffer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is outside the bounds of <paramref name="buffer" />.
    /// </exception>
    public static byte[] ComputeHash(
        this NonCryptographicHashAlgorithm algorithm,
        byte[] buffer,
        int offset,
        int count)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(buffer, offset, count);

        algorithm.Append(buffer.AsSpan(offset, count));
        return algorithm.GetHashAndReset();
    }

}