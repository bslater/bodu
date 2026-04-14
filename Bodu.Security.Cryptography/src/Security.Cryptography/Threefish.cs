namespace Bodu.Security.Cryptography
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    /// Serves as the abstract base class for managed implementations of the Threefish tweakable symmetric block cipher family
    /// (Threefish-256, Threefish-512, and Threefish-1024).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Threefish is the tweakable block cipher used as the core primitive of the Skein hash function. Each variant operates on a block
    /// whose size in bits matches its key size (256, 512, or 1024 bits) together with a 128-bit tweak. Derived classes must implement
    /// <see cref="CreateCipher(byte[], byte[])" /> to instantiate the appropriate concrete engine.
    /// </para>
    /// <para>
    /// The <see cref="BlockMode" /> property replaces the standard <see cref="SymmetricAlgorithm.Mode" /> property, enabling the use of
    /// additional or non-standard block cipher modes such as <see cref="CipherBlockMode.CTR" /> and <see cref="CipherBlockMode.OFB" />.
    /// </para>
    /// <note type="important">This class is not intended to be instantiated directly. Use <see cref="Threefish256" />,
    /// <see cref="Threefish512" />, or <see cref="Threefish1024" /> instead.</note>
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
        /// Initialises a new instance of the <see cref="Threefish" /> class with the specified block and tweak sizes.
        /// </summary>
        /// <param name="blockSizeBits">The block size in bits. Must match the Threefish variant block size (256, 512, or 1024).</param>
        /// <param name="tweakSizeBits">The tweak size in bits. 128 bits for all Threefish variants.</param>
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
        /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
        /// </summary>
        /// <value>One of the <see cref="CipherBlockMode" /> values. The default is <see cref="CipherBlockMode.CBC" />.</value>
        /// <remarks>
        /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property when used with
        /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports, including <see cref="CipherBlockMode.CTR" />
        /// and <see cref="CipherBlockMode.OFB" />.
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
                    string.Format(ResourceStrings.CryptographicException_InvalidIVSize, iv.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalBlockSizes)));

            if (tweak.Length != this.DefaultTweakSizeBytes)
                throw new CryptographicException(
                    string.Format(ResourceStrings.CryptographicException_InvalidTweakSize, tweak.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalTweakSizes)));
        }
    }
}