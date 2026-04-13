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
    /// Managed <c>Skipjack</c> symmetric‐algorithm wrapper that plugs the <see cref="SkipjackBlockCipher" /> engine into the standard
    /// <see cref="SymmetricAlgorithm" /> façade. It exposes CBC and ECB modes with any .NET <see cref="PaddingMode" /> scheme (default PKCS#7).
    /// </summary>
    /// <remarks>
    /// Skipjack has an 80‑bit key and 64‑bit block, giving <b>no</b> modern security margin. This class is provided only for legacy or
    /// research scenarios.
    /// </remarks>
    public sealed class Skipjack
        : System.Security.Cryptography.SymmetricAlgorithm
    {
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
        /// Gets or sets the extended cipher‑mode enumeration used across Bodu algorithms (CBC default).
        /// </summary>
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
        /// Throws if <paramref name="key" /> or <paramref name="iv" /> size do not match the fixed Skipjack requirements.
        /// </summary>
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