using System;
using System.Linq;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Serves as the abstract base class for managed implementations of the Threefish symmetric block cipher family.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This base class provides common functionality for Threefish variants, including support for tweakable keys, custom cipher block
    /// modes (e.g., CBC, CFB, OFB, CTR), and integration with the .NET <see cref="SymmetricAlgorithm" /> cryptographic framework.
    /// </para>
    /// <para>
    /// Derived classes must implement the <see cref="CreateCipher(byte[], byte[])" /> method to instantiate the appropriate
    /// <c>Threefish-256</c>, <c>Threefish-512</c>, or <c>Threefish-1024</c> block cipher engine.
    /// </para>
    /// <para>
    /// The <see cref="BlockMode" /> property replaces the standard <see cref="SymmetricAlgorithm.Mode" /> property, enabling use of
    /// additional or non-standard block cipher modes.
    /// </para>
    /// <note type="important">This class is not intended to be instantiated directly. Use a derived class such as
    /// <c>Threefish256Algorithm</c> or <c>Threefish512Algorithm</c>.</note>
    /// </remarks>
    public abstract class Threefish
        : TweakableSymmetricAlgorithm
    {
        /// <summary>
        /// The block size in bytes.
        /// </summary>
        protected readonly int BlockSizeBytes;

        /// <summary>
        /// The key size in bytes.
        /// </summary>
        protected readonly int KeySizeBytes;

        private readonly int DefaultTweakSizeBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Threefish" /> class with the specified block, key, and tweak sizes.
        /// </summary>
        /// <param name="blockSizeBits">The block size in bits. Must match the Threefish variant block size.</param>
        /// <param name="tweakSizeBits">The tweak size in bits. Typically 128 bits for all Threefish variants.</param>
        protected Threefish(int blockSizeBits, int tweakSizeBits)
        {
            BlockSizeValue = KeySizeValue = blockSizeBits;
            FeedbackSizeValue = 8;

            BlockSizeBytes = KeySizeBytes = blockSizeBits / 8;
            DefaultTweakSizeBytes = tweakSizeBits / 8;

            LegalBlockSizesValue = new[] { new KeySizes(blockSizeBits, blockSizeBits, 0) };
            LegalKeySizesValue = new[] { new KeySizes(blockSizeBits, blockSizeBits, 0) };
            LegalTweakSizesValue = new[] { new KeySizes(tweakSizeBits, tweakSizeBits, 0) };

            ModeValue = CipherMode.CBC;
            Padding = PaddingMode.PKCS7;
        }

        /// <summary>
        /// Gets or sets the block cipher mode to be used by the Threefish transform.
        /// </summary>
        /// <remarks>
        /// This property replaces the base class <see cref="SymmetricAlgorithm.Mode" /> and supports additional non-standard modes.
        /// </remarks>
        public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

        /// <inheritdoc />
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
        {
            Validate(rgbKey, rgbIV, tweak);
            var engine = CreateCipher(rgbKey, tweak);
            return new ThreefishTransform(engine, BlockMode, Padding, rgbIV, false);
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
        {
            Validate(rgbKey, rgbIV, tweak);
            var engine = CreateCipher(rgbKey, tweak);
            return new ThreefishTransform(engine, BlockMode, Padding, rgbIV, true);
        }

        /// <inheritdoc />
        public override void GenerateIV() =>
            IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeBytes);

        /// <inheritdoc />
        public override void GenerateKey() =>
            KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySizeBytes);

        /// <inheritdoc />
        public override void GenerateTweak() =>
            TweakValue = CryptoHelpers.GetRandomNonZeroBytes(DefaultTweakSizeBytes);

        /// <summary>
        /// Instantiates the concrete Threefish block cipher with the specified key and tweak.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="tweak">The tweak value.</param>
        /// <returns>A configured <see cref="IBlockCipher" /> instance for encryption or decryption.</returns>
        protected abstract IBlockCipher CreateCipher(byte[] key, byte[] tweak);

        /// <summary>
        /// Validates the provided key, IV, and tweak against expected lengths and legal sizes.
        /// </summary>
        /// <param name="key">The encryption key.</param>
        /// <param name="iv">The initialization vector.</param>
        /// <param name="tweak">The tweak value.</param>
        /// <exception cref="CryptographicException">Thrown when any input does not match the required length.</exception>
        protected void Validate(byte[] key, byte[] iv, byte[] tweak)
        {
            if (key.Length != KeySizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, CryptoHelpers.FormatLegalSizes(LegalKeySizesValue)));

            if (iv.Length != BlockSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidIVSize, key.Length * 8, CryptoHelpers.FormatLegalSizes(LegalBlockSizes)));

            if (tweak.Length != DefaultTweakSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidTweakSize, key.Length * 8, CryptoHelpers.FormatLegalSizes(LegalTweakSizes)));
        }
    }
}