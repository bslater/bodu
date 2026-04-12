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
        /// Asynchronously decrypts data from a source stream and writes the result to a target stream using the default buffer size.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="sourceStream">The stream to read encrypted data from.</param>
        /// <param name="targetStream">The stream to write decrypted data to.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null" />.</exception>
        public static Task DecryptAsync(
            this SymmetricAlgorithm algorithm,
            Stream sourceStream,
            Stream targetStream,
            CancellationToken cancellationToken = default) =>
            algorithm.DecryptAsync(sourceStream, targetStream, DefaultBufferSize, cancellationToken);

        /// <summary>
        /// Asynchronously decrypts data from a source stream and writes the result to a target stream using the specified buffer size.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="sourceStream">The stream to read encrypted data from.</param>
        /// <param name="targetStream">The stream to write decrypted data to.</param>
        /// <param name="bufferSize">The buffer size to use for reading and writing. Must be greater than zero.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bufferSize" /> is less than or equal to zero.</exception>
        /// <example>
        /// <code>
        ///await aes.DecryptAsync(encryptedStream, decryptedStream, 4096, cancellationToken);
        /// </code>
        /// </example>
        public static async Task DecryptAsync(
            this SymmetricAlgorithm algorithm,
            Stream sourceStream,
            Stream targetStream,
            int bufferSize,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(algorithm);
            ThrowHelper.ThrowIfNull(sourceStream);
            ThrowHelper.ThrowIfNull(targetStream);
            ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

            using ICryptoTransform transform = algorithm.CreateDecryptor();
            await ICryptoTransformExtensions.TransformAsync(transform, sourceStream, targetStream, bufferSize, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}