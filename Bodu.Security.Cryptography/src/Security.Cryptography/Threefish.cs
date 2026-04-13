namespace Bodu.Security.Cryptography
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

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
            this.BlockSizeValue = this.KeySizeValue = blockSizeBits;
            this.FeedbackSizeValue = 8;

            this.BlockSizeBytes = this.KeySizeBytes = blockSizeBits / 8;
            this.DefaultTweakSizeBytes = tweakSizeBits / 8;

            this.LegalBlockSizesValue = new[] { new KeySizes(blockSizeBits, blockSizeBits, 0) };
            this.LegalKeySizesValue = new[] { new KeySizes(blockSizeBits, blockSizeBits, 0) };
            this.LegalTweakSizesValue = new[] { new KeySizes(tweakSizeBits, tweakSizeBits, 0) };

            this.ModeValue = CipherMode.CBC;
            this.Padding = PaddingMode.PKCS7;
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
            this.Validate(rgbKey, rgbIV, tweak);
            var engine = this.CreateCipher(rgbKey, tweak);
            return new ThreefishTransform(engine, this.BlockMode, this.Padding, rgbIV, false);
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
        {
            this.Validate(rgbKey, rgbIV, tweak);
            var engine = this.CreateCipher(rgbKey, tweak);
            return new ThreefishTransform(engine, this.BlockMode, this.Padding, rgbIV, true);
        }

        /// <inheritdoc />
        public override void GenerateIV() =>
            this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeBytes);

        /// <inheritdoc />
        public override void GenerateKey() =>
            this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);

        /// <inheritdoc />
        public override void GenerateTweak() =>
            this.TweakValue = CryptoHelpers.GetRandomNonZeroBytes(this.DefaultTweakSizeBytes);

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
            if (key.Length != this.KeySizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)));

            if (iv.Length != this.BlockSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidIVSize, key.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalBlockSizes)));

            if (tweak.Length != this.DefaultTweakSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidTweakSize, key.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalTweakSizes)));
        }
    }
}