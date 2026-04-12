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
        /// Attempts to asynchronously verify that the computed hash of a stream matches the expected hash value.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="stream">The stream to read and hash. Must be readable.</param>
        /// <param name="expectedHash">The expected hash value as a byte array.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns><c>true</c> if the computed hash matches; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>This method safely validates a stream against a known hash, handling any internal errors gracefully.</remarks>
        public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            if (expectedHash == null || stream == null)
                return false;

            try
            {
                return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to asynchronously verify that the computed hash of a stream matches the expected hexadecimal string.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> used to compute the hash.</param>
        /// <param name="stream">The readable stream to hash.</param>
        /// <param name="expectedHex">The expected hash as a hexadecimal string.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns><c>true</c> if the computed hash matches the expected hex; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.</exception>
        /// <remarks>Used for verifying hashes from test vectors or external sources represented as hex strings.</remarks>
        public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, Stream stream, string expectedHex, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHex);

            try
            {
                return await algorithm.VerifyHashAsync(stream, expectedHex, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to asynchronously verify that the computed hash of a stream matches the expected memory buffer.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> used to compute the hash.</param>
        /// <param name="stream">The stream to read and hash asynchronously.</param>
        /// <param name="expectedHash">The expected hash value as a memory buffer.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> if the computed hash matches; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>This overload supports memory-friendly comparison of hashes from stream input.</remarks>
        public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, Stream stream, ReadOnlyMemory<byte> expectedHash, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);

            try
            {
                return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to asynchronously verify that the computed hash of a byte array matches the expected hash.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> used to compute the hash.</param>
        /// <param name="input">The input data as a byte array.</param>
        /// <param name="expectedHash">The expected hash to compare against.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns><c>true</c> if the computed hash matches the expected value; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any argument is <see langword="null" />.</exception>
        /// <remarks>Converts the input to a stream internally for compatibility with async hash APIs.</remarks>
        public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(input);
            ThrowHelper.ThrowIfNull(expectedHash);

            try
            {
                var stream = new MemoryStream(input, writable: false);
                return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to asynchronously verify that the computed hash of a byte array matches the expected hexadecimal hash.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> used for hashing.</param>
        /// <param name="input">The input data to hash.</param>
        /// <param name="expectedHex">The expected hash in hexadecimal format.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns><c>true</c> if the hash matches the hex string; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any argument is <see langword="null" />.</exception>
        /// <remarks>This method enables secure comparison against external sources or stored hashes in hex format.</remarks>
        public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, byte[] input, string expectedHex, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(input);
            ThrowHelper.ThrowIfNull(expectedHex);

            try
            {
                var stream = new MemoryStream(input, writable: false);
                return await algorithm.VerifyHashAsync(stream, expectedHex, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to asynchronously verify that the computed hash of a string (after encoding) matches the expected value.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance.</param>
        /// <param name="input">The input string to encode and hash.</param>
        /// <param name="encoding">The character encoding used to convert the string to bytes.</param>
        /// <param name="expectedHash">The expected hash byte array.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns><c>true</c> if the computed hash matches <paramref name="expectedHash" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any argument is <see langword="null" />.</exception>
        /// <remarks>Used when comparing user-entered or stored strings after encoding to binary form for hashing.</remarks>
        public static async Task<bool> TryVerifyHashAsync(this HashAlgorithm algorithm, string input, Encoding encoding, byte[] expectedHash, CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(input);
            ThrowHelper.ThrowIfNull(encoding);
            ThrowHelper.ThrowIfNull(expectedHash);

            try
            {
                byte[] inputBytes = encoding.GetBytes(input);
                var stream = new MemoryStream(inputBytes, writable: false);
                return await algorithm.VerifyHashAsync(stream, expectedHash, cancellationToken);
            }
            catch
            {
                return false;
            }
        }
    }
}