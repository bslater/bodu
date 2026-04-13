namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Creates <see cref="IBlockCipherModeTransform" /> instances that wrap an <see cref="IBlockCipher" /> with a standard chaining mode.
    /// </summary>
    /// <example>
    /// The following example composes a block cipher, a CBC mode transform, and PKCS#7 padding to encrypt a message:
    /// <code>
    /// using IBlockCipher cipher = /* construct an IBlockCipher, e.g. an AES wrapper */;
    /// IBlockCipherModeTransform mode = BlockCipherModeFactory.Create(CipherBlockMode.CBC, cipher, iv);
    /// IPaddingStrategy padding = PaddingFactory.Create(PaddingMode.PKCS7);
    ///
    /// byte[] padded = padding.Pad(plaintext, cipher.BlockSize);
    /// byte[] ciphertext = new byte[padded.Length];
    /// mode.Transform(padded, ciphertext, encrypt: true);
    /// </code>
    /// </example>
    public static class BlockCipherModeFactory
    {
        /// <summary>
        /// Creates a new <see cref="IBlockCipherModeTransform" /> instance for the specified block cipher mode.
        /// </summary>
        /// <param name="mode">The cipher mode to apply (for example <see cref="CipherBlockMode.CBC" />, <see cref="CipherBlockMode.CFB" />,
        /// <see cref="CipherBlockMode.OFB" />, <see cref="CipherBlockMode.ECB" />, or <see cref="CipherBlockMode.CTR" />).</param>
        /// <param name="cipher">The underlying block cipher to wrap.</param>
        /// <param name="iv">The initialisation vector or initial counter. Required by all modes except <see cref="CipherBlockMode.ECB" />
        /// and must have the same length as <see cref="IBlockCipher.BlockSize" />.</param>
        /// <returns>An <see cref="IBlockCipherModeTransform" /> that applies <paramref name="mode" /> over <paramref name="cipher" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv" /> is required but <see langword="null" /> or of the wrong length.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="mode" /> is not a supported <see cref="CipherBlockMode" /> value.</exception>
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