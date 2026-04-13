// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blowfish.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a managed implementation of the Blowfish symmetric block cipher. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Blowfish is a symmetric-key block cipher designed by Bruce Schneier in 1993. It operates on 64-bit (8-byte) blocks and accepts a
    /// variable-length key of between 32 and 448 bits (4 to 56 bytes). The cipher applies a 16-round Feistel network using four 256-entry
    /// S-boxes and an 18-entry P-array, all initialised from the hexadecimal digits of pi (π). The key schedule is computationally
    /// intensive by design, making brute-force attacks significantly more expensive.
    /// </para>
    /// <para>
    /// This class integrates with the .NET <see cref="SymmetricAlgorithm" /> framework and supports standard block cipher modes via the
    /// <see cref="BlockMode" /> property. The default mode is <see cref="CipherBlockMode.CBC" /> with
    /// <see cref="PaddingMode.PKCS7" /> padding.
    /// </para>
    /// <para>
    /// For further details on the algorithm, see
    /// <a href="https://www.schneier.com/academic/blowfish/">https://www.schneier.com/academic/blowfish/</a>.
    /// </para>
    /// <note type="important">
    /// Blowfish has a 64-bit block size, which makes it vulnerable to birthday-bound attacks (SWEET32) when large volumes of data are
    /// encrypted under the same key. For new applications, a cipher with a 128-bit or larger block size (such as AES) should be preferred.
    /// </note>
    /// </remarks>
    public sealed class Blowfish
        : SymmetricAlgorithm
    {
        /// <summary>
        /// The Blowfish block size in bits.
        /// </summary>
        internal const int BlockSizeBits = 64;

        /// <summary>
        /// The Blowfish block size in bytes.
        /// </summary>
        internal const int BlockSizeBytes = 8;

        /// <summary>
        /// The minimum permitted key size in bytes (32 bits).
        /// </summary>
        internal const int MinKeySizeBytes = 4;

        /// <summary>
        /// The maximum permitted key size in bytes (448 bits).
        /// </summary>
        internal const int MaxKeySizeBytes = 56;

        private static readonly KeySizes[] BlowfishBlockSizes = { new KeySizes(BlockSizeBits, BlockSizeBits, 0) };
        private static readonly KeySizes[] BlowfishKeySizes = { new KeySizes(MinKeySizeBytes * 8, MaxKeySizeBytes * 8, 8) };

        /// <summary>
        /// Initialises a new instance of the <see cref="Blowfish" /> class with default parameters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default configuration uses a 64-bit block, a 128-bit (16-byte) key, CBC cipher mode, and PKCS7 padding. Call
        /// <see cref="SymmetricAlgorithm.GenerateKey" /> and <see cref="SymmetricAlgorithm.GenerateIV" /> to produce random key material,
        /// or assign <see cref="SymmetricAlgorithm.Key" /> and <see cref="SymmetricAlgorithm.IV" /> directly before calling
        /// <see cref="CreateEncryptor(byte[], byte[])" /> or <see cref="CreateDecryptor(byte[], byte[])" />.
        /// </para>
        /// </remarks>
        public Blowfish()
        {
            this.BlockSizeValue = BlockSizeBits;
            this.LegalBlockSizesValue = BlowfishBlockSizes;

            // Default to a 128-bit key, which is within the legal range.
            this.KeySizeValue = 128;
            this.LegalKeySizesValue = BlowfishKeySizes;

            this.FeedbackSizeValue = BlockSizeBits;
            this.ModeValue = CipherMode.CBC;
            this.PaddingValue = PaddingMode.PKCS7;
        }

        /// <summary>
        /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
        /// </summary>
        /// <value>
        /// One of the <see cref="CipherBlockMode" /> values. The default is <see cref="CipherBlockMode.CBC" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property for use with
        /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports, including
        /// <see cref="CipherBlockMode.CTR" /> and <see cref="CipherBlockMode.OFB" />, which are not available via the standard
        /// <see cref="CipherMode" /> enumeration.
        /// </para>
        /// </remarks>
        public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

        /// <summary>
        /// Creates a new <see cref="Blowfish" /> instance with default parameters.
        /// </summary>
        /// <returns>A new <see cref="Blowfish" /> instance.</returns>
        public new static Blowfish Create() => new Blowfish();

        /// <summary>
        /// Creates a symmetric <see cref="Blowfish" /> decryptor using the specified key and initialisation vector.
        /// </summary>
        /// <param name="rgbKey">
        /// The secret key for the symmetric algorithm. Must be between <see cref="MinKeySizeBytes" /> and
        /// <see cref="MaxKeySizeBytes" /> bytes in length. Must not be <see langword="null" />.
        /// </param>
        /// <param name="rgbIV">
        /// The initialisation vector. Must be exactly <see cref="BlockSizeBytes" /> bytes in length. Must not be
        /// <see langword="null" /> for any cipher mode other than ECB.
        /// </param>
        /// <returns>A symmetric <see cref="Blowfish" /> decryptor object implementing <see cref="ICryptoTransform" />.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="rgbKey" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="rgbKey" /> is not within the permitted key size range, or <paramref name="rgbIV" /> has an invalid length for
        /// the configured <see cref="BlockMode" />.
        /// </exception>
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            if (rgbKey == null) throw new System.ArgumentNullException(nameof(rgbKey));

            var engine = new BlowfishBlockCipher(rgbKey);
            return new BlowfishTransform(engine, this.BlockMode, this.PaddingValue, rgbIV!, false);
        }

        /// <summary>
        /// Creates a symmetric <see cref="Blowfish" /> encryptor using the specified key and initialisation vector.
        /// </summary>
        /// <param name="rgbKey">
        /// The secret key for the symmetric algorithm. Must be between <see cref="MinKeySizeBytes" /> and
        /// <see cref="MaxKeySizeBytes" /> bytes in length. Must not be <see langword="null" />.
        /// </param>
        /// <param name="rgbIV">
        /// The initialisation vector. Must be exactly <see cref="BlockSizeBytes" /> bytes in length. Must not be
        /// <see langword="null" /> for any cipher mode other than ECB.
        /// </param>
        /// <returns>A symmetric <see cref="Blowfish" /> encryptor object implementing <see cref="ICryptoTransform" />.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="rgbKey" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="rgbKey" /> is not within the permitted key size range, or <paramref name="rgbIV" /> has an invalid length for
        /// the configured <see cref="BlockMode" />.
        /// </exception>
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            if (rgbKey == null) throw new System.ArgumentNullException(nameof(rgbKey));

            var engine = new BlowfishBlockCipher(rgbKey);
            return new BlowfishTransform(engine, this.BlockMode, this.PaddingValue, rgbIV!, true);
        }

        /// <summary>
        /// Generates a random initialisation vector (<see cref="SymmetricAlgorithm.IV" />) suitable for use with the Blowfish algorithm.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The generated IV is cryptographically random and contains no zero bytes. A new IV should be generated for each independent
        /// encryption operation when reusing a <see cref="Blowfish" /> instance with the same key.
        /// </para>
        /// </remarks>
        public override void GenerateIV()
            => this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeBytes);

        /// <summary>
        /// Generates a random key (<see cref="SymmetricAlgorithm.Key" />) of the currently configured key size for use with the Blowfish
        /// algorithm.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The generated key is cryptographically random and contains no zero bytes. The length is determined by
        /// <see cref="SymmetricAlgorithm.KeySize" />, which defaults to 128 bits (16 bytes) unless explicitly changed.
        /// </para>
        /// </remarks>
        public override void GenerateKey()
            => this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeValue / 8);
    }
}