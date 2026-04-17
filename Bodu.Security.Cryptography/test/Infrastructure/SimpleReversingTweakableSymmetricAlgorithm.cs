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
        /// Creates a new <see cref="SimpleReversingCryptoTransform" /> by assembling a
        /// <see cref="SimpleReversingBlockCipher" /> primitive (seeded with the supplied tweak), a
        /// <see cref="BlockCipherModeFactory" />-built chaining mode, and a <see cref="PaddingFactory" />-built padding
        /// strategy, in that order.
        /// </summary>
        private ICryptoTransform NewEncryptor(CipherMode cipherMode, byte[]? rgbKey, byte[]? rgbIV, byte[]? tweak, int feedbackSize, TransformMode encryptMode)
        {
            // Validate key and tweak lengths at the algorithm boundary. The reversing primitive ignores the key
            // and tolerates any tweak length, but honouring the declared sizes keeps the harness aligned with
            // the TweakableSymmetricAlgorithm contract that tests depend on.
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(rgbKey, KeySizeValue / 8);

            // Pre-check null IV so the conventional ArgumentNullException type is raised. The length check is
            // delegated to BlockCipherModeFactory, whose message carries both the required and offending IV
            // bit-lengths for the regression guard in SymmetricAlgorithmTests.
            if (rgbIV is null)
                throw new ArgumentNullException(nameof(rgbIV));

            ThrowHelper.ThrowIfArrayLengthIsInsufficient(tweak, TweakSizeValue / 8);
            ThrowHelper.ThrowIfLessThanOrEqual(feedbackSize, 0);

            // Build the pipeline via the production factories so the tweakable harness exercises the same
            // composition path as the real algorithms.
            var cipher = new SimpleReversingBlockCipher(BlockSizeValue / 8, tweak);
            IBlockCipherModeTransform modeTransform = BlockCipherModeFactory.Create(
                ToCipherBlockMode(cipherMode), cipher, rgbIV);
            IPaddingStrategy paddingStrategy = PaddingFactory.Create(PaddingValue);

            return new SimpleReversingCryptoTransform(cipher, modeTransform, paddingStrategy, encryptMode);
        }

        /// <summary>
        /// Maps the framework <see cref="CipherMode" /> exposed by <see cref="SymmetricAlgorithm" /> onto the library's
        /// <see cref="CipherBlockMode" /> enum so the algorithm can plug straight into <see cref="BlockCipherModeFactory" />.
        /// </summary>
        private static CipherBlockMode ToCipherBlockMode(CipherMode mode) => mode switch
        {
            CipherMode.ECB => CipherBlockMode.ECB,
            CipherMode.CBC => CipherBlockMode.CBC,
            CipherMode.CFB => CipherBlockMode.CFB,
            CipherMode.OFB => CipherBlockMode.OFB,
            _ => throw new NotSupportedException($"Cipher mode '{mode}' is not supported by {nameof(SimpleReversingTweakableSymmetricAlgorithm)}."),
        };
    }
}