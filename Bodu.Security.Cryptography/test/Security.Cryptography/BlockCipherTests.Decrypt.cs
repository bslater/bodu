using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
    {
        /// <summary>
        /// Returns test data that combines algorithm variants, named inputs, and expected hash results.
        /// </summary>
        /// <returns>A sequence of test case arguments: variant, input name, input bytes, and expected hash output.</returns>
        /// <remarks>
        /// This method is used to parameterize tests that verify the correctness of
        /// <see cref="IBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> against known input-output pairs.
        /// </remarks>
        public static IEnumerable<object[]> DecryptTestData()
        {
            var instance = new TTest();
            foreach (var variant in instance.GetBlockCipherVariants())
            {
                var testVectors = instance.GetKnownAnswerTests(variant);
                foreach (var vector in testVectors)
                {
                    yield return new object[] { variant, vector.Name, vector.ExpectedOutput, vector.Input };
                }
            }
        }

        /// <summary>
        /// Verifies that decryption does not alter the input span.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenCalled_ShouldNotModifyInputBuffer()
        {
            using var cipher = CreateBlockCipher();
            byte[] original = Enumerable.Range(0, cipher.BlockSize).Select(i => (byte)i).ToArray();
            byte[] input = original.ToArray();
            byte[] output = new byte[cipher.BlockSize];

            cipher.Decrypt(input, output);

            CollectionAssert.AreEqual(original, input); // input must be unchanged
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="IBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> across diferent instances
        /// with the same input produce the same result.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Decrypt_WhenCalled_WithDiferentInstances_ShouldBeDeterministic(TVariant variant)
        {
            byte[] input = Enumerable.Range(0, 128).Select(i => (byte)(i % 256)).ToArray();
            using var cipher1 = this.CreateBlockCipher(variant);
            using var cipher2 = this.CreateBlockCipher(variant);
            byte[] output1 = new byte[ExpectedBlockSize];
            byte[] output2 = new byte[ExpectedBlockSize];

            cipher1.Decrypt(input, output1);
            cipher2.Decrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="IBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> using the same instances and
        /// input produce the same result.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Decrypt_WhenCalled_WithSameInstsnce_ShouldBeDeterministic(TVariant variant)
        {
            byte[] input = Enumerable.Range(0, 128).Select(i => (byte)(i % 256)).ToArray();
            using var cipher = this.CreateBlockCipher(variant);
            byte[] output1 = new byte[ExpectedBlockSize];
            byte[] output2 = new byte[ExpectedBlockSize];

            cipher.Decrypt(input, output1);
            cipher.Decrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        [TestMethod]
        [DynamicData(nameof(DecryptTestData))]
        public void Decrypt_WhenKnownInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected)
        {
            var engine = CreateBlockCipher(variant);
            byte[] actual = new byte[expected.Length];
            engine.Decrypt(input, actual);

            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
        }

        /// <summary>
        /// Verifies that cedryption throws ArgumentException when input size is invalid.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataSourceType.Method)]
        public void Decrypt_WithInvalidInputSize_ShouldThrowExactly(byte[] input)
        {
            using var cipher = CreateBlockCipher();
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Decrypt(input, new byte[cipher.BlockSize]);
            });
        }

        /// <summary>
        /// Verifies that cedryption throws ArgumentException when output size is invalid.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataSourceType.Method)]
        public void Decrypt_WithInvalidOutSize_ShouldThrowExactly(byte[] output)
        {
            using var cipher = CreateBlockCipher();
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Decrypt(new byte[cipher.BlockSize], output);
            });
        }
    }
}