using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class HashAlgorithmExtensions
    {
        /// <summary>
        /// Asynchronously verifies that the computed hash of a stream matches the expected hash value.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="stream">The stream to read and hash asynchronously. The stream must be readable.</param>
        /// <param name="expectedHash">The expected hash value as a byte array.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that evaluates to <c>true</c> if the computed hash matches <paramref name="expectedHash" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// This method uses <see cref="HashAlgorithm.TransformBlock" /> and <see cref="HashAlgorithm.TransformFinalBlock" /> to compute the
        /// hash in buffered blocks and compare it to <paramref name="expectedHash" />.
        /// </remarks>
        public static async Task<bool> VerifyHashAsync(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHash);

            byte[] buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                algorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return algorithm.Hash!.SequenceEqual(expectedHash);
        }

        /// <summary>
        /// Asynchronously verifies that the computed hash of a stream matches the expected hexadecimal hash string.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="stream">The readable stream to hash asynchronously.</param>
        /// <param name="expectedHex">The expected hash as a hexadecimal string.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that evaluates to <c>true</c> if the computed hash matches <paramref name="expectedHex" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// This method computes the hash, converts it to a hexadecimal string, and compares it using case-insensitive ordinal comparison.
        /// </remarks>
        public static async Task<bool> VerifyHashAsync(this HashAlgorithm algorithm, Stream stream, string expectedHex, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHex);

            byte[] actualHash = await algorithm.ComputeHashAsync(stream, cancellationToken);
            string actualHex = Convert.ToHexString(actualHash);
            return string.Equals(expectedHex, actualHex, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Asynchronously verifies that the computed hash of a stream matches the expected hash value in memory.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="stream">The readable stream to hash asynchronously.</param>
        /// <param name="expectedHash">The expected hash value as a <see cref="ReadOnlyMemory{T}" />.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that evaluates to <c>true</c> if the computed hash matches <paramref name="expectedHash" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>This overload enables hash verification using memory buffers to reduce allocations in memory-sensitive scenarios.</remarks>
        public static async Task<bool> VerifyHashAsync(this HashAlgorithm algorithm, Stream stream, ReadOnlyMemory<byte> expectedHash, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);

            byte[] actualHash = await algorithm.ComputeHashAsync(stream, cancellationToken);
            return actualHash.AsSpan().SequenceEqual(expectedHash.Span);
        }
    }
}