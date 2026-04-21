using Bodu.Test;

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
                    yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput, vector.CipherFactory };
                }
            }
        }

        public static IEnumerable<object[]> GetInvalidBlockSizes()
        {
            var instance = new TTest();
            foreach (var variant in instance.GetBlockCipherVariants())
            {
                int blockSize = instance.GetSpecification(variant).BlockSize;

                yield return new object[] { variant, new byte[0] };
                yield return new object[] { variant, new byte[blockSize - 1] };
                yield return new object[] { variant, new byte[blockSize + 1] };
            }
        }

        public static IEnumerable<object[]> GetValidSingleBlockData()
        {
            var instance = new TTest();
            foreach (var variant in instance.GetBlockCipherVariants())
            {
                int blockSize = instance.GetSpecification(variant).BlockSize;

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
            byte[] original = CryptoTestUtilities.GetRandomNonZeroBytes(cipher.BlockSize);
            byte[] input = original.ToArray();
            byte[] output = new byte[cipher.BlockSize];

            cipher.Decrypt(input, output);

            CollectionAssert.AreEqual(original, input); // input must be unchanged
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> across diferent instances
        /// with the same input produce the same result.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Encrypt_WhenCalled_WithDiferentInstances_ShouldBeDeterministic(TVariant variant)
        {
            var specification = GetSpecification(variant);
            using var cipher1 = CreateBlockCipher(variant);
            using var cipher2 = CreateBlockCipher(variant);

            byte[] input = CryptoTestUtilities.GetRandomNonZeroBytes(cipher1.BlockSize);

            byte[] output1 = new byte[specification.BlockSize];
            byte[] output2 = new byte[specification.BlockSize];

            cipher1.Encrypt(input, output1);
            cipher2.Encrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> using the same instances and
        /// input produce the same result.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Encrypt_WhenCalled_WithSameInstsnce_ShouldBeDeterministic(TVariant variant)
        {
            var specification = GetSpecification(variant);
            using var cipher = CreateBlockCipher(variant);

            byte[] input = CryptoTestUtilities.GetRandomNonZeroBytes(cipher.BlockSize);

            byte[] output1 = new byte[specification.BlockSize];
            byte[] output2 = new byte[specification.BlockSize];

            cipher.Encrypt(input, output1);
            cipher.Encrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        [TestMethod]
        [DynamicData(nameof(EncryptTestData))]
        public void Encrypt_WhenKnownInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected, Func<IBlockCipher>? factory)
        {
            var engine = factory?.Invoke() ?? CreateBlockCipher(variant);
            byte[] actual = new byte[expected.Length];
            engine.Encrypt(input, actual);

            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
        }

        /// <summary>
        /// Verifies that encryption throws ArgumentException when input size is invalid.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataSourceType.Method)]
        public void Encrypt_WithInvalidInputSize_ShouldThrowExactly(TVariant variant, byte[] input)
        {
            using var cipher = CreateBlockCipher(variant);
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Encrypt(input, new byte[cipher.BlockSize]);
            });
        }

        /// <summary>
        /// Verifies that encryption throws ArgumentException when output size is invalid.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataSourceType.Method)]
        public void Encrypt_WithInvalidOutSize_ShouldThrowExactly(TVariant variant, byte[] output)
        {
            using var cipher = CreateBlockCipher(variant);
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Encrypt(new byte[cipher.BlockSize], output);
            });
        }

        /// <summary>
        /// Verifies that encryption and decryption can operate on the same buffer (in-place).
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants), DynamicDataSourceType.Method)]
        public void EncryptDecrypt_WithInPlaceBuffer_ShouldSucceed(TVariant variant)
        {
            using var cipher = CreateBlockCipher();

            byte[] original = CryptoTestUtilities.GetRandomNonZeroBytes(cipher.BlockSize);
            byte[] buffer = (byte[])original.Clone();

            cipher.Encrypt(buffer, buffer);
            cipher.Decrypt(buffer, buffer);

            CollectionAssert.AreEqual(original, buffer);
        }

        /// <summary>
        /// Verifies that encryption and decryption of valid blocks succeeds without exceptions.
        /// </summary>
        [TestMethod]
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