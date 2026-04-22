// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Known Answer Tests for <see cref="SimpleReversingBlockCipher" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="SimpleReversingBlockCipher" /> reverses the byte order of each block. Encryption and
    /// decryption are identical operations, so all KATs apply in both directions: encrypting the input
    /// produces the expected output, and encrypting the expected output reproduces the original input.
    /// </para>
    /// <para>
    /// The key has no effect on the cipher output and is provided only to satisfy the
    /// <see cref="IBlockCipher" /> contract. All vectors use a deterministic 16-byte all-zero key.
    /// </para>
    /// <para>
    /// Three block sizes are exercised, corresponding to the supported ranges declared by
    /// <see cref="SimpleReversingSymmetricAlgorithm.BlockSizesValue" />:
    /// <list type="bullet">
    /// <item><description>128 bits (16 bytes)</description></item>
    /// <item><description>192 bits (24 bytes)</description></item>
    /// <item><description>256 bits (32 bytes)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [TestClass]
    internal sealed partial class SimpleReversingBlockCipherTests
        : BlockCipherTests<SimpleReversingBlockCipherTests, SimpleReversingBlockCipher, SimpleReversingBlockSizeVariant>
    {
        // All KATs use a deterministic all-zero key — the key does not affect cipher output.
        private static readonly byte[] DefaultKey = new byte[16];

        /// <inheritdoc />
        protected override BlockCipherSpecification GetSpecification(SimpleReversingBlockSizeVariant variant) =>
            variant switch
            {
                SimpleReversingBlockSizeVariant.Block128 => new() { BlockSize = 16, KeySize = 16 },
                SimpleReversingBlockSizeVariant.Block192 => new() { BlockSize = 24, KeySize = 16 },
                SimpleReversingBlockSizeVariant.Block256 => new() { BlockSize = 32, KeySize = 16 },
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };

        /// <inheritdoc />
        public override IEnumerable<SimpleReversingBlockSizeVariant> GetBlockCipherVariants() =>
            Enum.GetValues<SimpleReversingBlockSizeVariant>();

        /// <inheritdoc />
        protected override SimpleReversingBlockCipher CreateBlockCipher(SimpleReversingBlockSizeVariant variant)
        {
            int blockSizeBytes = (int)variant / 8;
            return new SimpleReversingBlockCipher(DefaultKey, blockSizeBytes);
        }

        /// <inheritdoc />
        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SimpleReversingBlockSizeVariant variant) =>
            variant switch
            {
                SimpleReversingBlockSizeVariant.Block128 => GetKats128(),
                SimpleReversingBlockSizeVariant.Block192 => GetKats192(),
                SimpleReversingBlockSizeVariant.Block256 => GetKats256(),
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
            };

        /// <summary>
        /// Returns Known Answer Tests for the 128-bit (16-byte) block size.
        /// </summary>
        /// <remarks>
        /// Expected outputs are the byte-reversed inputs. Vectors cover: sequential ascending bytes,
        /// all-zero input (fixed point), all-0xFF input (fixed point), alternating 0xAA/0x55 pattern,
        /// repeated 8-byte patterns, and a mixed real-world-style pattern.
        /// </remarks>
        private static IEnumerable<KnownAnswerTest> GetKats128()
        {
            // Sequential ascending bytes — most useful for verifying byte order.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 128-bit / sequential ascending",
                Input = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
                ExpectedOutput = Convert.FromHexString("0F0E0D0C0B0A09080706050403020100"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 16),
            };

            // All-zero input — fixed point: reversing zeros yields zeros.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 128-bit / all zeros (fixed point)",
                Input = Convert.FromHexString("00000000000000000000000000000000"),
                ExpectedOutput = Convert.FromHexString("00000000000000000000000000000000"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 16),
            };

            // All-0xFF input — fixed point: reversing 0xFF bytes yields 0xFF bytes.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 128-bit / all 0xFF (fixed point)",
                Input = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
                ExpectedOutput = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 16),
            };

            // Alternating 0xAA/0x55 — verifies that adjacent byte positions are correctly swapped.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 128-bit / alternating 0xAA 0x55",
                Input = Convert.FromHexString("AA55AA55AA55AA55AA55AA55AA55AA55"),
                ExpectedOutput = Convert.FromHexString("55AA55AA55AA55AA55AA55AA55AA55AA"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 16),
            };

            // Repeated 8-byte pattern — confirms that the full 16-byte block is reversed,
            // not two independent 8-byte halves.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 128-bit / repeated 8-byte pattern",
                Input = Convert.FromHexString("0123456789ABCDEF0123456789ABCDEF"),
                ExpectedOutput = Convert.FromHexString("EFCDAB8967452301EFCDAB8967452301"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 16),
            };

            // Mixed pattern — simulates a typical block-cipher input with varied byte values.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 128-bit / mixed pattern",
                Input = Convert.FromHexString("DEADBEEFCAFEBABE0102030405060708"),
                ExpectedOutput = Convert.FromHexString("0807060504030201BEBAFECAEFBEADDE"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 16),
            };
        }

        /// <summary>
        /// Returns Known Answer Tests for the 192-bit (24-byte) block size.
        /// </summary>
        /// <remarks>
        /// Exercises the second supported block size range. The sequential-ascending vector is most
        /// useful for confirming correct reversal length; the fixed-point vectors guard against
        /// accidental truncation.
        /// </remarks>
        private static IEnumerable<KnownAnswerTest> GetKats192()
        {
            // Sequential ascending bytes across the full 24-byte block.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 192-bit / sequential ascending",
                Input = Convert.FromHexString("000102030405060708090A0B0C0D0E0F1011121314151617"),
                ExpectedOutput = Convert.FromHexString("17161514131211100F0E0D0C0B0A09080706050403020100"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 24),
            };

            // All-zero fixed point.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 192-bit / all zeros (fixed point)",
                Input = Convert.FromHexString("000000000000000000000000000000000000000000000000"),
                ExpectedOutput = Convert.FromHexString("000000000000000000000000000000000000000000000000"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 24),
            };

            // All-0xFF fixed point.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 192-bit / all 0xFF (fixed point)",
                Input = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
                ExpectedOutput = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 24),
            };
        }

        /// <summary>
        /// Returns Known Answer Tests for the 256-bit (32-byte) block size.
        /// </summary>
        /// <remarks>
        /// Exercises the largest of the three common block sizes in the first supported range.
        /// The sequential-ascending vector spans the full 32 bytes, making any partial-reversal
        /// defect immediately visible.
        /// </remarks>
        private static IEnumerable<KnownAnswerTest> GetKats256()
        {
            // Sequential ascending bytes across the full 32-byte block.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 256-bit / sequential ascending",
                Input = Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F"),
                ExpectedOutput = Convert.FromHexString("1F1E1D1C1B1A191817161514131211100F0E0D0C0B0A09080706050403020100"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 32),
            };

            // All-zero fixed point.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 256-bit / all zeros (fixed point)",
                Input = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000"),
                ExpectedOutput = Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 32),
            };

            // Alternating 0x00/0xFF — confirms block-wide reversal at extremes.
            yield return new KnownAnswerTest
            {
                Name = "SimpleReversing 256-bit / alternating 0x00 0xFF",
                Input = Convert.FromHexString("00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF"),
                ExpectedOutput = Convert.FromHexString("FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00FF00"),
                CipherFactory = () => new SimpleReversingBlockCipher(DefaultKey, 32),
            };
        }
    }
}