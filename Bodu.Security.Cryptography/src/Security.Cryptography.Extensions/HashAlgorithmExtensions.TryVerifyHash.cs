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
        /// Attempts to compute the hash of the specified input and compare it against the expected hash.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used for computing the hash.</param>
        /// <param name="input">The byte array input to hash.</param>
        /// <param name="expectedHash">The expected hash value for comparison.</param>
        /// <param name="result">Outputs <c>true</c> if the computed hash matches <paramref name="expectedHash" />; otherwise, <c>false</c>.</param>
        /// <returns><c>true</c> if hashing and comparison completed without exception; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// This method is useful for defensive validation when inputs may be malformed or optional. Unlike <c>VerifyHash</c>, it avoids
        /// exceptions during failure.
        /// </remarks>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash, out bool result)
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
        /// Attempts to compute and verify the hash of a byte array against an expected hash value.
        /// </summary>
        /// <param name="algorithm">The <see cref="HashAlgorithm" /> instance used for hashing.</param>
        /// <param name="input">The input data as a byte array.</param>
        /// <param name="expectedHash">The expected hash value.</param>
        /// <returns><c>true</c> if the hash matches; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, byte[] input, byte[] expectedHash)
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
        /// Attempts to verify the computed hash of the byte array input against a hexadecimal hash string.
        /// </summary>
        /// <param name="algorithm">The hashing algorithm.</param>
        /// <param name="input">The data to hash.</param>
        /// <param name="expectedHex">The expected hexadecimal hash string.</param>
        /// <returns><c>true</c> if the computed hash matches the expected hex string; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.</exception>
        /// <remarks>This is useful for verifying known test vectors stored in hexadecimal format.</remarks>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, byte[] input, string expectedHex)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHex);

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
        /// Attempts to verify that the computed hash of a UTF-encoded string matches the expected hash.
        /// </summary>
        /// <param name="algorithm">The hash algorithm instance.</param>
        /// <param name="input">The plain text input string.</param>
        /// <param name="encoding">The encoding used to convert the string into bytes.</param>
        /// <param name="expectedHash">The expected hash byte array.</param>
        /// <returns><c>true</c> if the hash matches the expected value; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" />, <paramref name="input" />, <paramref name="encoding" />, or
        /// <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, string input, Encoding encoding, byte[] expectedHash)
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

        /// <summary>
        /// Attempts to verify that the hash computed from a span of bytes matches the expected span value.
        /// </summary>
        /// <param name="algorithm">The algorithm used for hashing.</param>
        /// <param name="input">The span of input bytes.</param>
        /// <param name="expectedHash">The expected hash as a span.</param>
        /// <returns><c>true</c> if the spans match; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
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
        /// Attempts to verify that the hash of a memory buffer matches the expected byte array value.
        /// </summary>
        /// <param name="algorithm">The hashing algorithm instance.</param>
        /// <param name="input">The memory buffer containing input data.</param>
        /// <param name="expectedHash">The expected hash result.</param>
        /// <returns><c>true</c> if the memory contents produce a matching hash; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, ReadOnlyMemory<byte> input, byte[] expectedHash)
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
        /// Attempts to verify that the hash of a stream matches the expected byte array.
        /// </summary>
        /// <param name="algorithm">The hashing algorithm used to compute the hash.</param>
        /// <param name="stream">The input stream to hash. The stream must be readable and ideally seekable.</param>
        /// <param name="expectedHash">The expected hash value.</param>
        /// <returns><c>true</c> if the stream produces a matching hash; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" /> or <paramref name="expectedHash" /> is <see langword="null" />.
        /// </exception>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, Stream stream, byte[] expectedHash)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHash);

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
        /// Attempts to verify that the hash of a stream matches the expected hexadecimal hash value.
        /// </summary>
        /// <param name="algorithm">The hashing algorithm used to compute the hash.</param>
        /// <param name="stream">The input stream to hash. The stream must be readable and ideally seekable.</param>
        /// <param name="expectedHex">The expected hash value in hexadecimal format.</param>
        /// <returns><c>true</c> if the stream hash matches the expected hex; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="expectedHex" /> is <see langword="null" />.</exception>
        public static bool TryVerifyHash(this HashAlgorithm algorithm, Stream stream, string expectedHex)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(expectedHex);

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
}