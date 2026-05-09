// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.AppendData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Extensions;

using Bodu;
using System;
using System.Buffers;
using System.IO;
using System.IO.Hashing;

public static partial class NonCryptographicHashAlgorithmExtensions
{
    /// <summary>
    /// Feeds a span of bytes into the ongoing hash computation of the specified <see cref="NonCryptographicHashAlgorithm" />
    /// without finalising it.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance receiving the data. Must not be <see langword="null" />.
    /// </param>
    /// <param name="data">The span of bytes to feed into the hash computation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is a null-guarded wrapper around <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />,
    /// intended for use in incremental hashing scenarios where data is supplied in multiple segments. Retrieve the current
    /// digest at any point by calling <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> or
    /// <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" />.
    /// </para>
    /// <para>
    /// If <paramref name="data" /> is empty, this method returns without performing any work.
    /// </para>
    /// </remarks>
    public static void AppendData(this NonCryptographicHashAlgorithm algorithm, ReadOnlySpan<byte> data)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        if (data.IsEmpty)
            return;

        algorithm.Append(data);
    }

    /// <summary>
    /// Reads all bytes from <paramref name="source" /> and feeds them into the hash accumulator via
    /// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />, without finalising the computation.
    /// </summary>
    /// <param name="algorithm">
    /// The <see cref="NonCryptographicHashAlgorithm" /> instance receiving the data. Must not be <see langword="null" />.
    /// </param>
    /// <param name="source">
    /// The stream whose bytes are appended to the current hash state. Must not be <see langword="null" />.
    /// </param>
    /// <param name="bufferSize">
    /// The number of bytes read per iteration. Must be greater than zero. Defaults to 4096.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="algorithm" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bufferSize" /> is less than or equal to zero.
    /// </exception>
    /// <exception cref="IOException">
    /// <paramref name="source" /> threw an <see cref="IOException" /> during a read.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method allows large or streaming sources to be incorporated into an incremental hash computation. Because only
    /// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> is called, the hash state is not finalised after
    /// this method returns.
    /// </para>
    /// <para>
    /// The read buffer is rented from <see cref="ArrayPool{T}.Shared" /> and returned in all exit paths, including exception
    /// propagation.
    /// </para>
    /// </remarks>
    public static void AppendData(this NonCryptographicHashAlgorithm algorithm, Stream source, int bufferSize = 4096)
    {
        ThrowHelper.ThrowIfNull(algorithm);
        ThrowHelper.ThrowIfNull(source);

        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(bufferSize),
                bufferSize,
                "Buffer size must be greater than zero.");

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, bufferSize)) > 0)
                algorithm.Append(buffer.AsSpan(0, bytesRead));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
