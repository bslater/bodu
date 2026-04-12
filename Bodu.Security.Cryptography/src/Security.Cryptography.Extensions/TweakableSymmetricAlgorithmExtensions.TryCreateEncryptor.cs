using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class TweakableSymmetricAlgorithmExtensions
    {
        /// <summary>
        /// Attempts to create an encryptor using the current key, IV, and tweak values of a <see cref="TweakableSymmetricAlgorithm" />.
        /// </summary>
        /// <param name="algorithm">The tweakable symmetric algorithm to use for encryption.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the encryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown if the key, IV, or tweak values are invalid or uninitialized.</exception>
        /// <remarks>
        /// This method uses the algorithm's configured <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, and <see cref="TweakableSymmetricAlgorithm.Tweak" />.
        /// </remarks>
        public static bool TryCreateEncryptor(
            this TweakableSymmetricAlgorithm algorithm,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
                return true;
            }
            catch
            {
                transform = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create an encryptor using the specified key, IV, and tweak values for a <see cref="TweakableSymmetricAlgorithm" />.
        /// </summary>
        /// <param name="algorithm">The tweakable symmetric algorithm to use for encryption.</param>
        /// <param name="key">The encryption key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="tweak">The tweak value to use for encryption.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the encryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown if the key, IV, or tweak are not valid for the algorithm's configuration.</exception>
        /// <remarks>This method provides full control over the keying material and is preferred when tweak values must be set explicitly.</remarks>
        public static bool TryCreateEncryptor(
            this TweakableSymmetricAlgorithm algorithm,
            byte[] key,
            byte[] iv,
            byte[] tweak,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateEncryptor(key, iv, tweak);
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