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
        /// Decrypts the entire input byte array using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="array">The input byte array to decrypt.</param>
        /// <returns>A new byte array containing the decrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.</exception>
        /// <remarks>This is equivalent to calling <c>Decrypt(array, 0, array.Length)</c>.</remarks>
        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] array)
            => algorithm.Decrypt(array, 0, array?.Length ?? 0);

        /// <summary>
        /// Decrypts a portion of a byte array from the specified offset to the end using the given symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="array">The input byte array to decrypt.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="array" /> to begin reading from.</param>
        /// <returns>A new byte array containing the decrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> or <paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="offset" /> is negative or exceeds the bounds of <paramref name="array" />.
        /// </exception>
        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] array, int offset)
            => algorithm.Decrypt(array, offset, array?.Length - offset ?? 0);

        /// <summary>
        /// Decrypts a portion of a byte array using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="array">The input byte array to decrypt.</param>
        /// <param name="offset">The starting offset within the byte array.</param>
        /// <param name="count">The number of bytes to decrypt.</param>
        /// <returns>A new byte array containing the decrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm"/> or <paramref name="array"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> is out of bounds.</exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// byte[] decrypted = aes.Decrypt(cipherText, 0, cipherText.Length);
        ///]]>
        /// </code>
        /// </example>
        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] array, int offset, int count)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(array);
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);

            return algorithm.Decrypt(array.AsSpan(offset, count));
        }

        /// <summary>
        /// Decrypts a span of bytes using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="input">The span of input bytes to decrypt.</param>
        /// <returns>A new byte array containing the decrypted output.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
        /// <remarks>This method avoids intermediate allocations and is optimal for performance.</remarks>
        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, ReadOnlySpan<byte> input)
        {
            ThrowHelper.ThrowIfNull(algorithm);

            using ICryptoTransform transform = algorithm.CreateDecryptor();
            return ICryptoTransformExtensions.Transform(transform, input);
        }

        /// <summary>
        /// Decrypts a memory region using the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="input">The input memory region to decrypt.</param>
        /// <returns>A new byte array containing the decrypted output.</returns>
        /// <remarks>This overload delegates to <see cref="Decrypt(SymmetricAlgorithm, ReadOnlySpan{byte})" />.</remarks>
        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, ReadOnlyMemory<byte> input)
            => algorithm.Decrypt(input.Span);

        /// <summary>
        /// Decrypts data from a source stream and writes the result to a target stream using a default buffer size.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="sourceStream">The stream to read encrypted input from.</param>
        /// <param name="targetStream">The stream to write decrypted output to.</param>
        /// <returns>The total number of bytes read and decrypted.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is <see langword="null" />.
        /// </exception>
        public static int Decrypt(this SymmetricAlgorithm algorithm, Stream sourceStream, Stream targetStream)
            => algorithm.Decrypt(sourceStream, targetStream, SymmetricAlgorithmExtensions.DefaultBufferSize);

        /// <summary>
        /// Decrypts data from a source stream and writes the result to a target stream using the specified buffer size.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="sourceStream">The stream to read encrypted input from.</param>
        /// <param name="targetStream">The stream to write decrypted output to.</param>
        /// <param name="bufferSize">The buffer size, in bytes, to use while reading and writing. Must be greater than zero.</param>
        /// <returns>The total number of bytes read and decrypted.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="algorithm"/>, <paramref name="sourceStream"/>, or <paramref name="targetStream"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is less than or equal to zero.</exception>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// using var input = File.OpenRead("output.enc");
        /// using var output = File.Create("decrypted.txt");
        /// aes.Decrypt(input, output, 4096);
        ///]]>
        /// </code>
        /// </example>
        public static int Decrypt(this SymmetricAlgorithm algorithm, Stream sourceStream, Stream targetStream, int bufferSize)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(sourceStream);
            ThrowHelper.ThrowIfNull(targetStream);
            ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

            using ICryptoTransform transform = algorithm.CreateDecryptor();
            return ICryptoTransformExtensions.Transform(transform, sourceStream, targetStream, bufferSize);
        }
    }
}