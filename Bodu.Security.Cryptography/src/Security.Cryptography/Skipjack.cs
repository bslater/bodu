// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skipjack.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Provides a managed implementation of the Skipjack symmetric block cipher, exposing the <see cref="SkipjackBlockCipher" /> engine
    /// through the standard <see cref="SymmetricAlgorithm" /> framework. Skipjack uses a fixed 80-bit key and 64-bit block and is
    /// supported here for legacy and research scenarios only. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// Because of its 80-bit key and 64-bit block, Skipjack offers no modern security margin and must not be used to protect sensitive
    /// data in new applications. Prefer a modern cipher such as AES.
    /// </remarks>
    public sealed class Skipjack
        : System.Security.Cryptography.SymmetricAlgorithm
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="Skipjack" /> class with the fixed 80-bit key size and 64-bit block size, CBC
        /// cipher mode, and PKCS7 padding.
        /// </summary>
        public Skipjack()
        {
            // Set up legal sizes according to the original spec (80‑bit key, 64‑bit block).
            this.LegalKeySizesValue = new[] { new KeySizes(80, 80, 0) };
            this.LegalBlockSizesValue = new[] { new KeySizes(64, 64, 0) };

            this.KeySizeValue = 80;      // bits
            this.BlockSizeValue = 64;      // bits

            this.Padding = PaddingMode.PKCS7;
            this.Mode = CipherMode.CBC;   // base property (not used directly - see BlockMode)
        }

        /// <summary>
        /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
        /// </summary>
        /// <value>One of the <see cref="CipherBlockMode" /> values. The default is <see cref="CipherBlockMode.CBC" />.</value>
        /// <remarks>
        /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property when used with
        /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports.
        /// </remarks>
        public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

        private int BlockSizeBytes => this.BlockSizeValue / 8;

        private int KeySizeBytes => this.KeySizeValue / 8;

        /// <inheritdoc />
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            this.Validate(rgbKey, rgbIV);
            IBlockCipher engine = CreateCipher(rgbKey);
            return new SkipjackTransform(engine, this.BlockMode, this.Padding, rgbIV, false);
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            this.Validate(rgbKey, rgbIV);
            IBlockCipher engine = CreateCipher(rgbKey);
            return new SkipjackTransform(engine, this.BlockMode, this.Padding, rgbIV, true);
        }

        /// <inheritdoc />
        public override void GenerateIV() =>
            this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeBytes);

        /// <inheritdoc />
        public override void GenerateKey() =>
            this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);

        private static IBlockCipher CreateCipher(byte[] key) => new SkipjackBlockCipher(key);

        /// <summary>
        /// Throws a <see cref="CryptographicException" /> if <paramref name="key" /> or <paramref name="iv" /> does not match the fixed
        /// Skipjack key and block lengths.
        /// </summary>
        /// <param name="key">The key to validate.</param>
        /// <param name="iv">The initialisation vector to validate.</param>
        private void Validate(byte[] key, byte[] iv)
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(iv);

            if (key.Length != this.KeySizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidKeySize,
                                  key.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)));

            if (iv.Length != this.BlockSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidIVSize,
                                  iv.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalBlockSizesValue)));
        }
    }
}