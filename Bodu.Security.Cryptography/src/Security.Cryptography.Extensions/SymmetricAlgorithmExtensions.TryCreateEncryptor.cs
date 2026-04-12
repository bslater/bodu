using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class SymmetricAlgorithmExtensions
    {
        /// <summary>
        /// Attempts to create an encryptor using the specified encryption key and initialization vector (IV).
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="key">The encryption key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the encryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown by the underlying algorithm if the key or IV are invalid.</exception>
        /// <remarks>
        /// This method wraps <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> in a try/catch block to simplify error
        /// handling when attempting to create transforms dynamically.
        /// </remarks>
        public static bool TryCreateEncryptor(
            this SymmetricAlgorithm algorithm,
            byte[] key,
            byte[] iv,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateEncryptor(key, iv);
                return true;
            }
            catch
            {
                transform = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create an encryptor using the current <see cref="SymmetricAlgorithm.Key" /> and <see cref="SymmetricAlgorithm.IV" /> values.
        /// </summary>
        /// <param name="algorithm">The symmetric algorithm to use for encryption.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the encryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown if the algorithm has not been properly initialized with a key and IV.</exception>
        /// <remarks>
        /// Use this overload when the algorithm has already been configured with <see cref="SymmetricAlgorithm.GenerateKey" /> and <see cref="SymmetricAlgorithm.GenerateIV" />.
        /// </remarks>
        public static bool TryCreateEncryptor(
            this SymmetricAlgorithm algorithm,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateEncryptor();
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