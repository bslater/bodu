using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure
{
    /// <summary>
    /// A symmetric algorithm that produces a simple reversing transform for testing or diagnostic purposes.
    /// </summary>
    /// <remarks>
    /// This implementation supports configurable block/key sizes, key/IV generation, and integration with
    /// <see cref="SimpleReversingCryptoTransform" />. It is not intended for production security use.
    /// </remarks>
    public sealed class SimpleReversingSymmetricAlgorithm
        : System.Security.Cryptography.SymmetricAlgorithm
    {
        /// <summary>
        /// The default block size in bits (256 bits).
        /// </summary>
        public const int DefaultBlockSize = 0x100;

        /// <summary>
        /// The default tweak size in bits (128 bits). Not actively used by the algorithm.
        /// </summary>
        public const int DefaultTweakSize = 0x80;

        /// <summary>
        /// Legal block/key sizes supported by this algorithm.
        /// </summary>
        public static readonly KeySizes[] BlockSizesValue = new[]
        {
            new KeySizes(128, 256, 64),        // Supports: 128, 192, 256
            new KeySizes(448, 576, 128),    // Supports: 448, 576
            new KeySizes(1024, 2048, 512)    // Supports: 1024, 1536, 2048
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleReversingSymmetricAlgorithm" /> class with default block/key sizes and legal configuration.
        /// </summary>
        public SimpleReversingSymmetricAlgorithm()
        {
            BlockSizeValue = DefaultBlockSize;
            KeySizeValue = DefaultBlockSize;
            FeedbackSizeValue = DefaultBlockSize;

            LegalBlockSizesValue = BlockSizesValue;
            LegalKeySizesValue = BlockSizesValue;

            IsCryptoTransformDisposed = true;
        }

        /// <summary>
        /// Gets a hashValue indicating whether the most recently created <see cref="ICryptoTransform" /> has been disposed.
        /// </summary>
        public bool IsCryptoTransformDisposed { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="SimpleReversingSymmetricAlgorithm" /> class with the default configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="SimpleReversingSymmetricAlgorithm" />.</returns>
        /// <remarks>
        /// The newly created algorithm instance will have its key, initialization vector (IV), and tweak generated automatically as needed
        /// upon first use.
        /// </remarks>
        public new static SimpleReversingSymmetricAlgorithm Create()
        {
            return new SimpleReversingSymmetricAlgorithm();
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            => CreateTransform(rgbKey, rgbIV, TransformMode.Decrypt);

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            => CreateTransform(rgbKey, rgbIV, TransformMode.Encrypt);

        /// <inheritdoc />
        public override void GenerateIV()
            => IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSize / 8);

        /// <inheritdoc />
        public override void GenerateKey()
            => KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySize / 8);

        /// <summary>
        /// Sets the key size in bits for the algorithm.
        /// </summary>
        /// <param name="size">The key size in bits.</param>
        public void SetKeySize(int size)
            => KeySizeValue = size;

        /// <summary>
        /// Fills the provided <paramref name="destination" /> span with a randomly generated IV.
        /// </summary>
        /// <param name="destination">The span to fill with IV bytes.</param>
        /// <returns>True if the span was large enough to hold the IV; otherwise, false.</returns>
        /// <remarks>The required length is <c>BlockSize / 8</c> bytes. If the span is too small, no data is written.</remarks>
        public bool TryGenerateIV(Span<byte> destination)
        {
            int required = BlockSize / 8;
            if (destination.Length < required)
                return false;

            CryptoHelpers.FillWithRandomNonZeroBytes(destination.Slice(0, required));
            return true;
        }

        /// <summary>
        /// Fills the provided <paramref name="destination" /> span with a randomly generated key.
        /// </summary>
        /// <param name="destination">The span to fill with key bytes.</param>
        /// <returns>True if the span was large enough to hold the key; otherwise, false.</returns>
        /// <remarks>The required length is <c>KeySize / 8</c> bytes. If the span is too small, no data is written.</remarks>
        public bool TryGenerateKey(Span<byte> destination)
        {
            int required = KeySize / 8;
            if (destination.Length < required)
                return false;

            CryptoHelpers.FillWithRandomNonZeroBytes(destination.Slice(0, required));
            return true;
        }

        /// <summary>
        /// Creates a new <see cref="SimpleReversingCryptoTransform" /> with the specified parameters.
        /// </summary>
        /// <param name="key">The encryption/decryption key. If null, one will be generated.</param>
        /// <param name="iv">The initialization vector. If null, one will be generated.</param>
        /// <param name="mode">The transform mode (Encrypt or Decrypt).</param>
        /// <returns>An initialized <see cref="ICryptoTransform" /> instance.</returns>
        private ICryptoTransform CreateTransform(byte[]? key, byte[]? iv, TransformMode mode)
        {
            IsCryptoTransformDisposed = false;

            ThrowHelper.ThrowIfArrayLengthIsInsufficient(key, KeySizeValue / 8);

            // Validate IV length with a message that carries the offending bit-length so
            // SymmetricAlgorithmTests.CreateEncryptor_WithInvalidIvLength_ShouldThrowArgumentException
            // can confirm the validator reports the right quantity.
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));

            int expectedIvBytes = BlockSizeValue / 8;
            if (iv.Length != expectedIvBytes)
                throw new ArgumentException(
                    $"The IV must be {expectedIvBytes * 8} bits ({expectedIvBytes} bytes) long; the supplied IV is {iv.Length * 8} bits ({iv.Length} bytes).",
                    nameof(iv));

            var transform = new SimpleReversingCryptoTransform(
                BlockSizeValue,
                FeedbackSizeValue,
                key,
                iv,
                tweak: null,
                cipherMode: ModeValue,
                paddingMode: PaddingValue,
                transformMode: mode
            );

            transform.Disposed += (_, _) =>
            {
                Debug.Assert(!IsCryptoTransformDisposed, "Transform should not be disposed more than once.");
                IsCryptoTransformDisposed = true;
            };

            return transform;
        }
    }
}