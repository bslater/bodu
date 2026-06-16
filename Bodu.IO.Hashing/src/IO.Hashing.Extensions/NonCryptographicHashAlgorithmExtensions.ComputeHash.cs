// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.ComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Computes the hash value for the specified byte array and resets the algorithm to its initial state.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash value.
    /// </param>
    /// <param name="buffer">The byte array whose contents are appended to the algorithm.</param>
    /// <returns>A newly allocated byte array containing the computed hash value.</returns>
    /// <remarks>
    /// This method is a one-shot computation: any pending state on <paramref name="algorithm" /> is discarded before
    /// processing. The full contents of <paramref name="buffer" /> are appended, the resulting hash is retrieved, and
    /// the algorithm is reset to its initial state on exit via
    /// <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" />.
    /// </remarks>
    /// <remarks>
    /// Use <see cref="AppendData(NonCryptographicHashAlgorithm, ReadOnlySpan{byte})" /> to incorporate data into the
    /// running state without resetting.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="buffer" /> is <see langword="null" />.
    /// </exception>
    public static byte[] ComputeHash(this NonCryptographicHashAlgorithm algorithm, byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(buffer);

        algorithm.Reset();
        algorithm.Append(buffer);
        return algorithm.GetHashAndReset();
    }

    /// <summary>
    /// Computes the hash value for the specified span of bytes and resets the algorithm to its initial state.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash value.
    /// </param>
    /// <param name="data">The contiguous region of memory whose bytes are appended to the algorithm.</param>
    /// <returns>A newly allocated byte array containing the computed hash value.</returns>
    /// <remarks>
    /// This method is a one-shot computation: any pending state on <paramref name="algorithm" /> is discarded before
    /// processing. The contents of <paramref name="data" /> are appended, the resulting hash is retrieved, and the
    /// algorithm is reset to its initial state on exit via
    /// <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" />.
    /// </remarks>
    /// <remarks>
    /// Use <see cref="AppendData(NonCryptographicHashAlgorithm, ReadOnlySpan{byte})" /> to incorporate data into the
    /// running state without resetting.
    /// </remarks>
    /// <remarks>
    /// This overload accepts stack-allocated, array-backed, unmanaged, and sliced memory without requiring the caller
    /// to allocate an intermediate byte array. The returned hash value is still allocated as a new array.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="algorithm" /> is <see langword="null" />.</exception>
    public static byte[] ComputeHash(
        this NonCryptographicHashAlgorithm algorithm,
        ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        algorithm.Reset();
        algorithm.Append(data);
        return algorithm.GetHashAndReset();
    }

    /// <summary>
    /// Computes the hash value for the remaining bytes of the specified stream and resets the algorithm to its initial
    /// state.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash value.
    /// </param>
    /// <param name="source">The stream whose bytes are read and appended to the algorithm.</param>
    /// <param name="bufferSize">
    /// The size, in bytes, of the temporary buffer used when reading from <paramref name="source" />. The default value
    /// is 4096.
    /// </param>
    /// <returns>A newly allocated byte array containing the computed hash value.</returns>
    /// <remarks>
    /// This method is a one-shot computation: any pending state on <paramref name="algorithm" /> is discarded before
    /// reading begins. <paramref name="source" /> is read from its current position until no more bytes are available,
    /// each block is appended to <paramref name="algorithm" />, the resulting hash is retrieved, and the algorithm is
    /// reset to its initial state on exit via <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" />.
    /// </remarks>
    /// <remarks>
    /// The stream is not rewound before hashing and is not closed or disposed when hashing completes. For seekable
    /// streams, the caller is responsible for positioning <paramref name="source" /> before calling this method.
    /// </remarks>
    /// <remarks>
    /// Use <see cref="AppendData(NonCryptographicHashAlgorithm, Stream, int)" /> to incorporate data into the running
    /// state without resetting.
    /// </remarks>
    /// <remarks>
    /// The temporary read buffer is rented from <see cref="ArrayPool{T}.Shared" /> and returned when the operation
    /// completes. Increasing <paramref name="bufferSize" /> may improve throughput for some streams, while smaller
    /// values may reduce temporary memory pressure.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="IOException">An I/O error occurs while reading from <paramref name="source" />.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="source" /> has been disposed.</exception>
    /// <exception cref="NotSupportedException"><paramref name="source" /> does not support reading.</exception>
    public static byte[] ComputeHash(
        this NonCryptographicHashAlgorithm algorithm,
        Stream source,
        int bufferSize = 4096)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        ArgumentNullException.ThrowIfNull(source);
        ThrowHelper.ThrowIfZeroOrNegative(bufferSize);

        algorithm.Reset();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
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
    /// Computes the hash value for the specified region of a byte array and resets the algorithm to its initial state.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance used to compute the hash value.
    /// </param>
    /// <param name="buffer">The byte array containing the region to hash.</param>
    /// <param name="offset">The zero-based offset in <paramref name="buffer" /> at which hashing begins.</param>
    /// <param name="count">The number of bytes to append from <paramref name="buffer" />.</param>
    /// <returns>A newly allocated byte array containing the computed hash value.</returns>
    /// <remarks>
    /// This method is a one-shot computation: any pending state on <paramref name="algorithm" /> is discarded before
    /// processing. Exactly <paramref name="count" /> bytes from <paramref name="buffer" />, starting at
    /// <paramref name="offset" />, are appended; the resulting hash is retrieved and the algorithm is reset to its
    /// initial state on exit via <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" />.
    /// </remarks>
    /// <remarks>
    /// Use this overload when the data to hash occupies only a portion of an existing array and an intermediate copy
    /// should be avoided.
    /// </remarks>
    /// <remarks>
    /// Non-cryptographic hash algorithms are designed for scenarios such as checksums, hash tables, sharding,
    /// bucketing, fingerprinting, and accidental-corruption detection. They must not be used for password hashing,
    /// digital signatures, message authentication, tamper detection, or other security-sensitive purposes.
    /// </remarks>
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

        algorithm.Reset();
        algorithm.Append(buffer.AsSpan(offset, count));
        return algorithm.GetHashAndReset();
    }
}
