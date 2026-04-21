// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IResumableHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System;
using System.IO.Hashing;

/// <summary>
/// Represents a non-cryptographic hash algorithm that supports resuming a previously finalised hash state and continuing
/// the hash computation with additional input data.
/// </summary>
/// <remarks>
/// This interface is intended for use with stateful non-cryptographic hash algorithms (such as CRC, FNV, or Jenkins) that
/// allow the internal state to be reconstructed from a finalised hash output. Implementations must reverse any
/// finalisation steps — such as XOR-out or reflection — before resuming the hash process.
/// </remarks>
public interface IResumableHashAlgorithm
{
    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value, processes additional input, and writes the new
    /// finalised hash to the specified destination span.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from.</param>
    /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
    /// <param name="destination">The destination buffer to write the finalised hash value to.</param>
    /// <param name="bytesWritten">Outputs the number of bytes written to the destination buffer.</param>
    /// <returns>
    /// <see langword="true" /> if the resumed and finalised hash was written successfully; otherwise,
    /// <see langword="false" /> if the destination span was too small.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </exception>
    bool TryComputeHashFrom(
        ReadOnlySpan<byte> previousHash,
        ReadOnlySpan<byte> newData,
        Span<byte> destination,
        out int bytesWritten);

    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value and processes additional input, returning the
    /// new finalised hash result as a byte array.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from.</param>
    /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
    /// <returns>A byte array containing the new finalised hash result.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </exception>
    byte[] ComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData);

    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value and processes additional input, returning the
    /// new finalised hash result as a byte array.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from. Must not be <see langword="null" />.</param>
    /// <param name="newData">The additional input data to include in the resumed hash calculation. Must not be <see langword="null" />.</param>
    /// <returns>A byte array containing the new finalised hash result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="previousHash" /> or <paramref name="newData" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </exception>
    byte[] ComputeHashFrom(byte[] previousHash, byte[] newData);

    /// <summary>
    /// Resumes a hash computation from a previously finalised hash value and processes a specified range of new data,
    /// returning the new finalised hash result as a byte array.
    /// </summary>
    /// <param name="previousHash">The previously finalised hash value to resume from. Must not be <see langword="null" />.</param>
    /// <param name="newData">The buffer containing additional input data. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based offset into <paramref name="newData" /> at which to begin reading data.</param>
    /// <param name="length">The number of bytes to read from <paramref name="newData" />.</param>
    /// <returns>A byte array containing the new finalised hash result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="previousHash" /> or <paramref name="newData" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="previousHash" /> length does not match
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />, or if the offset and length exceed the bounds of
    /// <paramref name="newData" />.
    /// </exception>
    byte[] ComputeHashFrom(byte[] previousHash, byte[] newData, int offset, int length);
}
