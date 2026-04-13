namespace Bodu.Security.Cryptography
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a hash algorithm that supports resuming a previously finalized hash state and continuing the hash computation with
    /// additional input data.
    /// </summary>
    /// <remarks>
    /// This interface is intended for use with non-cryptographic or stateful hash algorithms (such as CRC, FNV, or Jenkins) that allow the
    /// internal state to be reconstructed from a finalized hash output. Implementations must reverse any finalization steps-such as XOR-out
    /// or reflection-before resuming the hash process.
    /// </remarks>
    public interface IResumableHashAlgorithm
    {
        /// <summary>
        /// Resumes a hash computation from a previously finalized hash value, processes additional input, and writes the new finalized hash
        /// to the specified destination span.
        /// </summary>
        /// <param name="previousHash">The previously finalized hash value to resume from.</param>
        /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
        /// <param name="destination">The destination buffer to write the finalized hash value to.</param>
        /// <param name="bytesWritten">Outputs the number of bytes written to the destination buffer.</param>
        /// <returns>
        /// <see langword="true" /> if the resumed and finalized hash was written successfully; otherwise, <see langword="false" /> if the
        /// destination span was too small.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="previousHash" /> length does not match <see cref="HashAlgorithm.HashSize" />.</exception>
        bool TryComputeHashFrom(
            ReadOnlySpan<byte> previousHash,
            ReadOnlySpan<byte> newData,
            Span<byte> destination,
            out int bytesWritten);

        /// <summary>
        /// Resumes a hash computation from a previously finalized hash value and processes additional input, returning the new finalized
        /// hash result as a byte array.
        /// </summary>
        /// <param name="previousHash">The previously finalized hash value to resume from.</param>
        /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
        /// <returns>A byte array containing the new finalized hash result.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="previousHash" /> length does not match <see cref="HashAlgorithm.HashSize" />.</exception>
        byte[] ComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData);

        /// <summary>
        /// Resumes a hash computation from a previously finalized hash value and processes additional input, returning the new finalized
        /// hash result as a byte array.
        /// </summary>
        /// <param name="previousHash">The previously finalized hash value to resume from.</param>
        /// <param name="newData">The additional input data to include in the resumed hash calculation.</param>
        /// <returns>A byte array containing the new finalized hash result.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="previousHash" /> length does not match <see cref="HashAlgorithm.HashSize" />.</exception>
        byte[] ComputeHashFrom(byte[] previousHash, byte[] newData);

        /// <summary>
        /// Resumes a hash computation from a previously finalized hash value and processes a specified range of new data, returning the new
        /// finalized hash result as a byte array.
        /// </summary>
        /// <param name="previousHash">The previously finalized hash value to resume from.</param>
        /// <param name="newData">The buffer containing additional input data.</param>
        /// <param name="offset">The zero-based offset into <paramref name="newData" /> at which to begin reading data.</param>
        /// <param name="length">The number of bytes to read from <paramref name="newData" />.</param>
        /// <returns>A byte array containing the new finalized hash result.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="previousHash" /> length does not match <see cref="HashAlgorithm.HashSize" />, or if the offset and
        /// length exceed the bounds of <paramref name="newData" />.
        /// </exception>
        byte[] ComputeHashFrom(byte[] previousHash, byte[] newData, int offset, int length);
    }
}