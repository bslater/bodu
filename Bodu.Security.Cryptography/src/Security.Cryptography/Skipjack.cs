// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skipjack.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
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
            LegalKeySizesValue = new[] { new KeySizes(80, 80, 0) };
            LegalBlockSizesValue = new[] { new KeySizes(64, 64, 0) };

            KeySizeValue = 80;      // bits
            BlockSizeValue = 64;      // bits

            Padding = PaddingMode.PKCS7;
            Mode = CipherMode.CBC;   // base property (not used directly - see BlockMode)
        }

        /// <summary>
        /// Gets or sets the extended cipher‑mode enumeration used across Bodu algorithms (CBC default).
        /// </summary>
        public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

        private int BlockSizeBytes => BlockSizeValue / 8;

        private int KeySizeBytes => KeySizeValue / 8;

        /// <inheritdoc />
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Validate(rgbKey, rgbIV);
            IBlockCipher engine = CreateCipher(rgbKey);
            return new SkipjackTransform(engine, BlockMode, Padding, rgbIV, false);
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            Validate(rgbKey, rgbIV);
            IBlockCipher engine = CreateCipher(rgbKey);
            return new SkipjackTransform(engine, BlockMode, Padding, rgbIV, true);
        }

        /// <inheritdoc />
        public override void GenerateIV() =>
            IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeBytes);

        /// <inheritdoc />
        public override void GenerateKey() =>
            KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySizeBytes);

        private static IBlockCipher CreateCipher(byte[] key) => new SkipjackBlockCipher(key);

        /// <summary>
        /// Throws if <paramref name="key" /> or <paramref name="iv" /> size do not match the fixed Skipjack requirements.
        /// </summary>
        private void Validate(byte[] key, byte[] iv)
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(iv);

            if (key.Length != KeySizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidKeySize,
                                  key.Length * 8, CryptoHelpers.FormatLegalSizes(LegalKeySizesValue)));

            if (iv.Length != BlockSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidIVSize,
                                  iv.Length * 8, CryptoHelpers.FormatLegalSizes(LegalBlockSizesValue)));
        }
    }
}