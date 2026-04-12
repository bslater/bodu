using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bodu;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class SymmetricAlgorithmExtensions
    {
        /// <summary>
        /// Defines the default buffer size, in bytes, to be used when reading from or writing to streams during encryption.
        /// </summary>
        public const int DefaultBufferSize = 81920;

        /// <summary>
        /// Encrypts the entire input byte array using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="array">The input byte array to encrypt.</param>
        /// <returns>A new byte array containing the encrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.</exception>
        /// <remarks>This is equivalent to calling <c>Encrypt(array, 0, array.Length)</c>.</remarks>
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, byte[] array)
            => algorithm.Encrypt(array, 0, array?.Length ?? 0);

        /// <summary>
        /// Encrypts a portion of a byte array from the specified offset to the end using the given symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="array">The input byte array to encrypt.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="array" /> to begin reading from.</param>
        /// <returns>A new byte array containing the encrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="offset" /> is negative or exceeds the bounds of <paramref name="array" />.
        /// </exception>
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, byte[] array, int offset)
            => algorithm.Encrypt(array, offset, array?.Length - offset ?? 0);

        /// <summary>
        /// Encrypts a portion of a byte array using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="array">The input byte array to encrypt.</param>
        /// <param name="offset">The starting offset within the byte array.</param>
        /// <param name="count">The number of bytes to encrypt.</param>
        /// <returns>A new byte array containing the encrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm"/> or <paramref name="array"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> is out of bounds.</exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// byte[] cipherText = aes.Encrypt(data, 0, data.Length);
        ///]]>
        /// </code>
        /// </example>
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, byte[] array, int offset, int count)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);

            return algorithm.Encrypt(array.AsSpan(offset, count));
        }

        /// <summary>
        /// Encrypts a span of bytes using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="input">The span of input bytes to encrypt.</param>
        /// <returns>A new byte array containing the encrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>This method is optimized for performance by avoiding temporary buffer allocations.</remarks>
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, ReadOnlySpan<byte> input)
        {
            ThrowHelper.ThrowIfNull(algorithm);

            using ICryptoTransform transform = algorithm.CreateEncryptor();
            return ICryptoTransformExtensions.Transform(transform, input);
        }

        /// <summary>
        /// Encrypts a memory region using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="input">The input memory region to encrypt.</param>
        /// <returns>A new byte array containing the encrypted output.</returns>
        /// <remarks>This overload simply delegates to <see cref="Encrypt(SymmetricAlgorithm, ReadOnlySpan{byte})" />.</remarks>
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, ReadOnlyMemory<byte> input)
            => algorithm.Encrypt(input.Span);

        /// <summary>
        /// Encrypts data from a source stream and writes the encrypted result to a target stream using a default buffer size.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="sourceStream">The stream to read plaintext from.</param>
        /// <param name="targetStream">The stream to write the encrypted output to.</param>
        /// <returns>The total number of bytes read and encrypted.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is <see langword="null" />.
        /// </exception>
        public static int Encrypt(this SymmetricAlgorithm algorithm, Stream sourceStream, Stream targetStream)
            => algorithm.Encrypt(sourceStream, targetStream, DefaultBufferSize);

        /// <summary>
        /// Encrypts data from a source stream and writes the encrypted result to a target stream using the specified buffer size.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="sourceStream">The stream to read plaintext from.</param>
        /// <param name="targetStream">The stream to write the encrypted output to.</param>
        /// <param name="bufferSize">The size of the buffer used for reading and writing. Must be greater than zero.</param>
        /// <returns>The total number of bytes read and encrypted.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm"/>, <paramref name="sourceStream"/>, or <paramref name="targetStream"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is less than or equal to zero.</exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// using var input = File.OpenRead("input.txt");
        /// using var output = File.Create("output.enc");
        /// aes.Encrypt(input, output, 4096);
        ///]]>
        /// </code>
        /// </example>
        public static int Encrypt(this SymmetricAlgorithm algorithm, Stream sourceStream, Stream targetStream, int bufferSize)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(sourceStream);
            ThrowHelper.ThrowIfNull(targetStream);
            ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

            using ICryptoTransform transform = algorithm.CreateEncryptor();
            return ICryptoTransformExtensions.Transform(transform, sourceStream, targetStream, bufferSize);
        }
    }
}