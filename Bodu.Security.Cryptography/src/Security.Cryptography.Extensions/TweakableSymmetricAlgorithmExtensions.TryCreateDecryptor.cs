using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class TweakableSymmetricAlgorithmExtensions
    {
        /// <summary>
        /// Attempts to create a decryptor using the current <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />,
        /// and <see cref="TweakableSymmetricAlgorithm.Tweak" /> values.
        /// </summary>
        /// <param name="algorithm">The tweakable symmetric algorithm to use for decryption.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the decryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown if the key, IV, or tweak values are invalid or uninitialized.</exception>
        /// <remarks>
        /// This method uses the configured <see cref="TweakableSymmetricAlgorithm.Key" />, <see cref="TweakableSymmetricAlgorithm.IV" />,
        /// and <see cref="TweakableSymmetricAlgorithm.Tweak" />.
        /// </remarks>
        public static bool TryCreateDecryptor(
            this TweakableSymmetricAlgorithm algorithm,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
                return true;
            }
            catch
            {
                transform = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create a decryptor using the specified key, initialization vector, and tweak.
        /// </summary>
        /// <param name="algorithm">The tweakable symmetric algorithm to use for decryption.</param>
        /// <param name="key">The decryption key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="tweak">The tweak value to use for decryption.</param>
        /// <param name="transform">
        /// When this method returns, contains the created <see cref="ICryptoTransform" /> if successful; otherwise, <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if the decryptor was successfully created; otherwise, <see langword="false" />.</returns>
        /// <exception cref="CryptographicException">Thrown if the key, IV, or tweak do not meet algorithm constraints.</exception>
        /// <remarks>Use this overload when you need to explicitly supply all decryption parameters including a custom tweak.</remarks>
        public static bool TryCreateDecryptor(
            this TweakableSymmetricAlgorithm algorithm,
            byte[] key,
            byte[] iv,
            byte[] tweak,
            out ICryptoTransform? transform)
        {
            try
            {
                transform = algorithm.CreateDecryptor(key, iv, tweak);
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