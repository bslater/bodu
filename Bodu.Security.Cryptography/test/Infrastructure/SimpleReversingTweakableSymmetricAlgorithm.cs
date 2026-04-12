using Bodu.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Infrastructure
{
    /// <summary>
    /// A test implementation of <see cref="TweakableSymmetricAlgorithm" /> that produces a simple reversing crypto transform, with optional
    /// tweak input for controlled transformation variation.
    /// </summary>
    /// <remarks>This class is used for validating tweakable crypto flows in test scenarios. It is not cryptographically secure.</remarks>
    public sealed class SimpleReversingTweakableSymmetricAlgorithm
        : Security.Cryptography.TweakableSymmetricAlgorithm
    {
        public const int DefaultBlockSize = 0x100;
        public const int DefaultTweakSize = 0x100;

        public static readonly KeySizes[] BlockSizesValue = new[]
        {
            new KeySizes(64, 64, 0),        // Supports: 64
            new KeySizes(192, 256, 64),        // Supports: 192, 256
            new KeySizes(448, 576, 128),    // Supports: 448, 576
            new KeySizes(1024, 2048, 512)    // Supports: 1024, 1536, 2048
        };

        public SimpleReversingTweakableSymmetricAlgorithm()
        {
            BlockSizeValue = DefaultBlockSize;
            KeySizeValue = DefaultBlockSize;
            TweakSizeValue = DefaultTweakSize;
            FeedbackSizeValue = DefaultBlockSize;

            LegalBlockSizesValue = BlockSizesValue;
            LegalKeySizesValue = BlockSizesValue;
            LegalTweakSizesValue = BlockSizesValue;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
            => NewEncryptor(ModeValue, rgbKey, rgbIV, tweak, FeedbackSizeValue, TransformMode.Decrypt);

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
            => NewEncryptor(ModeValue, rgbKey, rgbIV, tweak, FeedbackSizeValue, TransformMode.Encrypt);

        public override void GenerateIV()
                                            => IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSize / 8);

        public override void GenerateKey()
            => KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySize / 8);

        public override void GenerateTweak()
            => TweakValue = CryptoHelpers.GetRandomNonZeroBytes(TweakSize / 8);

        /// <summary>
        /// Explicitly sets the tweak size in bits.
        /// </summary>
        public void SetTweakSize(int size)
            => TweakSizeValue = size;

        /// <summary>
        /// Creates a transform using byte[] fallbacks.
        /// </summary>
        private ICryptoTransform NewEncryptor(CipherMode cipherMode, byte[]? rgbKey, byte[]? rgbIV, byte[]? tweak, int feedbackSize, TransformMode encryptMode)
        {
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(rgbKey, KeySizeValue / 8);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(rgbIV, BlockSizeValue / 8);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(tweak, TweakSizeValue / 8);
            ThrowHelper.ThrowIfLessThanOrEqual(feedbackSize, 0);

            return new SimpleReversingCryptoTransform(
                BlockSizeValue,
                feedbackSize,
                rgbKey,
                rgbIV,
                tweak,
                cipherMode,
                PaddingValue,
                encryptMode
            );
        }
    }
}