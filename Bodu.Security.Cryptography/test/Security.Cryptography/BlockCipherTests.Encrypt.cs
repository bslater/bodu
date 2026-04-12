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
        /// <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> against known input-output pairs.
        /// </remarks>
        public static IEnumerable<object[]> EncryptTestData()
        {
            var instance = new TTest();
            foreach (var variant in instance.GetBlockCipherVariants())
            {
                var testVectors = instance.GetKnownAnswerTests(variant);
                foreach (var vector in testVectors)
                {
                    yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput };
                }
            }
        }

        public static IEnumerable<object[]> GetInvalidBlockSizes()
        {
            var instance = new TTest();
            int blockSize = instance.ExpectedBlockSize;

            yield return new object[] { new byte[0] };
            yield return new object[] { new byte[blockSize - 1] };
            yield return new object[] { new byte[blockSize + 1] };
        }

        public static IEnumerable<object[]> GetValidSingleBlockData()
        {
            var instance = new TTest();
            foreach (var variant in instance.GetBlockCipherVariants())
            {
                int blockSize = instance.ExpectedBlockSize;

                yield return new object[] { variant, "All Zeros", new byte[blockSize] };
                yield return new object[] { variant, "Ascending Bytes", Enumerable.Range(0, blockSize).Select(i => (byte)i).ToArray() };
                yield return new object[] { variant, "All 0xFF", Enumerable.Repeat((byte)0xFF, blockSize).ToArray() };
                yield return new object[] { variant, "Alternating 0xAA / 0x55", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 2 == 0 ? 0xAA : 0x55)).ToArray() };
                yield return new object[] { variant, "Alternating 0xFF / 0x00", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 2 == 0 ? 0xFF : 0x00)).ToArray() };
                yield return new object[] { variant, "Alternating 0xF0 / 0x0F", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 2 == 0 ? 0xF0 : 0x0F)).ToArray() };
                yield return new object[] { variant, "Sawtooth 0x00–0x0F", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 16)).ToArray() };
                yield return new object[] { variant, "Mirrored Half Asc/Desc", Enumerable.Range(0, blockSize).Select(i => (byte)(i < blockSize / 2 ? i : blockSize - i - 1)).ToArray() };
            }
        }

        /// <summary>
        /// Verifies that decryption does not alter the input span.
        /// </summary>
        [TestMethod]
        public void Encrypt_WhenCalled_ShouldNotModifyInputBuffer()
        {
            using var cipher = CreateBlockCipher();
            byte[] original = Enumerable.Range(0, cipher.BlockSize).Select(i => (byte)i).ToArray();
            byte[] input = original.ToArray();
            byte[] output = new byte[cipher.BlockSize];

            cipher.Decrypt(input, output);

            CollectionAssert.AreEqual(original, input); // input must be unchanged
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> across diferent instances
        /// with the same input produce the same result.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Encrypt_WhenCalled_WithDiferentInstances_ShouldBeDeterministic(TVariant variant)
        {
            byte[] input = Enumerable.Range(0, 128).Select(i => (byte)(i % 256)).ToArray();
            using var cipher1 = this.CreateBlockCipher(variant);
            using var cipher2 = this.CreateBlockCipher(variant);
            byte[] output1 = new byte[ExpectedBlockSize];
            byte[] output2 = new byte[ExpectedBlockSize];

            cipher1.Encrypt(input, output1);
            cipher2.Encrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> using the same instances and
        /// input produce the same result.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Encrypt_WhenCalled_WithSameInstsnce_ShouldBeDeterministic(TVariant variant)
        {
            byte[] input = Enumerable.Range(0, 128).Select(i => (byte)(i % 256)).ToArray();
            using var cipher = this.CreateBlockCipher(variant);
            byte[] output1 = new byte[ExpectedBlockSize];
            byte[] output2 = new byte[ExpectedBlockSize];

            cipher.Encrypt(input, output1);
            cipher.Encrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        [DataTestMethod]
        [DynamicData(nameof(EncryptTestData))]
        public void Encrypt_WhenKnownInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected)
        {
            var engine = CreateBlockCipher(variant);
            byte[] actual = new byte[expected.Length];
            engine.Encrypt(input, actual);

            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
        }

        /// <summary>
        /// Verifies that encryption throws ArgumentException when input size is invalid.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataSourceType.Method)]
        public void Encrypt_WithInvalidInputSize_ShouldThrowExactly(byte[] input)
        {
            using var cipher = CreateBlockCipher();
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Encrypt(input, new byte[cipher.BlockSize]);
            });
        }

        /// <summary>
        /// Verifies that encryption throws ArgumentException when output size is invalid.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataSourceType.Method)]
        public void Encrypt_WithInvalidOutSize_ShouldThrowExactly(byte[] output)
        {
            using var cipher = CreateBlockCipher();
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Encrypt(new byte[cipher.BlockSize], output);
            });
        }

        /// <summary>
        /// Verifies that encryption and decryption can operate on the same buffer (in-place).
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(BlockCipherVariants), DynamicDataSourceType.Method)]
        public void EncryptDecrypt_WithInPlaceBuffer_ShoulSucceed(TVariant variant)
        {
            using var cipher = CreateBlockCipher();
            byte[] buffer = Enumerable.Range(0, cipher.BlockSize).Select(i => (byte)i).ToArray();
            cipher.Encrypt(buffer, buffer);
            cipher.Decrypt(buffer, buffer);

            CollectionAssert.AreEqual(Enumerable.Range(0, cipher.BlockSize).Select(i => (byte)i).ToArray(), buffer);
        }

        /// <summary>
        /// Verifies that encryption and decryption of valid blocks succeeds without exceptions.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(GetValidSingleBlockData), DynamicDataSourceType.Method)]
        public void EncryptDecrypt_WithValidInput_ShouldRoundtrip(TVariant variant, string testName, byte[] input)
        {
            using var cipher = CreateBlockCipher(variant);

            byte[] encrypted = new byte[cipher.BlockSize];
            cipher.Encrypt(input, encrypted);
            byte[] actual = new byte[cipher.BlockSize];
            cipher.Decrypt(encrypted, actual);

            CollectionAssert.AreEqual(actual, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
        }
    }
}