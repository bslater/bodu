using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class SymmetricAlgorithmExtensions
    {
        /// <summary>
        /// Attempts to create a decryptor using the specified decryption key and initialization vector (IV).
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="key">The decryption key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the decryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown by the algorithm if the key or IV is invalid.</exception>
        /// <remarks>
        /// This method wraps <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> in a try/catch block for safe execution in
        /// dynamic or runtime scenarios.
        /// </remarks>
        public static bool TryCreateDecryptor(
            this SymmetricAlgorithm algorithm,
            byte[] key,
            byte[] iv,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateDecryptor(key, iv);
                return true;
            }
            catch
            {
                transform = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create a decryptor using the current <see cref="SymmetricAlgorithm.Key" /> and <see cref="SymmetricAlgorithm.IV" /> values.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for decryption.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the decryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown if the algorithm has not been properly initialized with a key and IV.</exception>
        /// <remarks>Use this overload when the algorithm instance has already been configured with a key and IV.</remarks>
        public static bool TryCreateDecryptor(
            this SymmetricAlgorithm algorithm,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateDecryptor();
                return true;
            }
            catch
            {
                transform = null;
                return false;
            }
        }
    }
}