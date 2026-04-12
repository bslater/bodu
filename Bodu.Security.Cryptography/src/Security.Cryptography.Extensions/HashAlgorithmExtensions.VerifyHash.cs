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
        /// Verifies that the computed hash of the input data matches the expected hash value.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="input">The input byte array whose hash will be computed.</param>
        /// <param name="expectedHash">The expected hash value as a byte array.</param>
        /// <returns><c>true</c> if the computed hash is equal to <paramref name="expectedHash" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>Computes a hash from the input and compares it to <paramref name="expectedHash" /> using byte-wise equality.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHash);

            byte[] actualHash = algorithm.ComputeHash(input);
            return actualHash.SequenceEqual(expectedHash);
        }

        /// <summary>
        /// Verifies that the computed hash of the input data matches the expected hash value as a hexadecimal string.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="input">The input byte array whose hash will be computed.</param>
        /// <param name="expectedHex">The expected hash value as a hexadecimal string.</param>
        /// <returns><c>true</c> if the computed hash matches <paramref name="expectedHex" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.</exception>
        /// <remarks>Converts the computed hash to a hexadecimal string and performs a case-insensitive comparison to <paramref name="expectedHex" />.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, byte[] input, string expectedHex)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHex);

            byte[] actualHash = algorithm.ComputeHash(input);
            string actualHex = Convert.ToHexString(actualHash);
            return string.Equals(expectedHex, actualHex, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the computed hash of the stream matches the expected hash value.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="stream">The input stream to read and hash. The stream must be readable.</param>
        /// <param name="expectedHash">The expected hash value as a byte array.</param>
        /// <returns><c>true</c> if the hash matches <paramref name="expectedHash" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>Computes the hash of the stream and compares it to <paramref name="expectedHash" /> byte-by-byte.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHash);

            byte[] actualHash = algorithm.ComputeHash(stream);
            return actualHash.SequenceEqual(expectedHash);
        }

        /// <summary>
        /// Verifies that the computed hash of the stream matches the expected hash value as a hexadecimal string.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used to compute the hash.</param>
        /// <param name="stream">The stream to hash. Must be readable.</param>
        /// <param name="expectedHex">The expected hash value as a hexadecimal string.</param>
        /// <returns><c>true</c> if the hash matches <paramref name="expectedHex" />; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.</exception>
        /// <remarks>Converts the computed hash of the stream to a hex string and compares it to <paramref name="expectedHex" />.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, Stream stream, string expectedHex)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHex);

            byte[] actualHash = algorithm.ComputeHash(stream);
            string actualHex = Convert.ToHexString(actualHash);
            return string.Equals(expectedHex, actualHex, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the computed hash of the input span matches the expected span.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> used to compute the hash.</param>
        /// <param name="input">The input span of bytes to hash.</param>
        /// <param name="expectedHash">The expected hash as a byte span.</param>
        /// <returns><c>true</c> if the computed hash matches the expected hash; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>Converts the span to a buffer and computes its hash using <see cref="HashAlgorithm.ComputeHash(byte[])" />.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, ReadOnlySpan<byte> input, ReadOnlySpan<byte> expectedHash)
        {
            ThrowHelper.ThrowIfNull(algorithm);

            byte[] actual = algorithm.ComputeHash(input.ToArray());
            return actual.AsSpan().SequenceEqual(expectedHash);
        }

        /// <summary>
        /// Verifies that the computed hash of the input memory block matches the expected hash.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> used to compute the hash.</param>
        /// <param name="input">The memory buffer to hash.</param>
        /// <param name="expectedHash">The expected hash value.</param>
        /// <returns><c>true</c> if the hash matches; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>This overload supports memory-friendly verification against a byte array.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, ReadOnlyMemory<byte> input, byte[] expectedHash)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHash);
            return algorithm.VerifyHash(input.Span, expectedHash);
        }

        /// <summary>
        /// Verifies that the hash of the specified string (after encoding) matches the expected value.
        /// </summary>
        /// <param name="algorithm">The hashing algorithm.</param>
        /// <param name="text">The input string to encode.</param>
        /// <param name="encoding">The encoding used to convert the string to bytes.</param>
        /// <param name="expectedHash">The expected hash as a byte array.</param>
        /// <returns><c>true</c> if the computed hash matches; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" />, <paramref name="text" />, <paramref name="encoding" />, or
        /// <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>This overload allows direct verification of encoded strings against known hash values.</remarks>
        public static bool VerifyHash(this HashAlgorithm algorithm, string text, Encoding encoding, byte[] expectedHash)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(text);
            ThrowHelper.ThrowIfNull(encoding);
            ThrowHelper.ThrowIfNull(expectedHash);

            byte[] data = encoding.GetBytes(text);
            return algorithm.VerifyHash(data, expectedHash);
        }
    }
}