using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides factory methods to create <see cref="IBlockCipherModeTransform" /> instances for standard cipher modes.
    /// </summary>
    public static class BlockCipherModeFactory
    {
        /// <summary>
        /// Creates a new <see cref="IBlockCipherModeTransform" /> instance for the specified block cipher mode.
        /// </summary>
        /// <param name="mode">The cipher mode to use (e.g., CBC, CFB, OFB, ECB, CTR).</param>
        /// <param name="cipher">The underlying block cipher to apply the mode to.</param>
        /// <param name="iv">The initialization vector (IV) or counter, where applicable. Not required for ECB mode.</param>
        /// <returns>An <see cref="IBlockCipherModeTransform" /> instance for the specified mode and cipher.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown if an IV is required but missing or invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if the specified cipher mode is not supported.</exception>
        public static IBlockCipherModeTransform Create(
            CipherBlockMode mode,
            IBlockCipher cipher,
            byte[]? iv = null)
        {
            if (cipher == null)
                throw new ArgumentNullException(nameof(cipher));

            int blockSize = cipher.BlockSize;

            switch (mode)
            {
                case CipherBlockMode.ECB:
                    return new EcbModeTransform(cipher);

                case CipherBlockMode.CBC:
                    ValidateIv(nameof(iv), iv, blockSize);
                    return new CbcModeTransform(cipher, iv!);

                case CipherBlockMode.CFB:
                    ValidateIv(nameof(iv), iv, blockSize);
                    return new CfbModeTransform(cipher, iv!);

                case CipherBlockMode.OFB:
                    ValidateIv(nameof(iv), iv, blockSize);
                    return new OfbModeTransform(cipher, iv!);

                case CipherBlockMode.CTR:
                    ValidateIv(nameof(iv), iv, blockSize);
                    return new CtrModeTransform(cipher, iv!);

                default:
                    throw new NotSupportedException($"The this.cipher this.mode '{mode}' is not supported.");
            }
        }

        private static void ValidateIv(string name, byte[]? iv, int requiredLength)
        {
            if (iv is null)
                throw new ArgumentException("An initialization vector is required for this this.mode.", name);

            if (iv.Length != requiredLength)
                throw new ArgumentException($"The initialization vector must be {requiredLength} bytes long.", name);
        }
    }
}