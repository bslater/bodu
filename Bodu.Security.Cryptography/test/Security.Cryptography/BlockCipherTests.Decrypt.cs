// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using Bodu.Extensions;
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
                    yield return new object[] { variant, vector.Name, vector.ExpectedOutput, vector.Input, vector.CipherFactory! };
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
            byte[] original = CryptoTestUtilities.GetRandomNonZeroBytes(cipher.BlockSize);
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
            var specification = GetSpecification(variant);
            using var cipher1 = CreateBlockCipher(variant);
            using var cipher2 = CreateBlockCipher(variant);

            byte[] input = CryptoTestUtilities.GetRandomNonZeroBytes(cipher1.BlockSize);

            byte[] output1 = new byte[specification.BlockSize];
            byte[] output2 = new byte[specification.BlockSize];

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
            var specification = GetSpecification(variant);
            using var cipher = CreateBlockCipher(variant);

            byte[] input = CryptoTestUtilities.GetRandomNonZeroBytes(cipher.BlockSize);

            byte[] output1 = new byte[specification.BlockSize];
            byte[] output2 = new byte[specification.BlockSize];

            cipher.Decrypt(input, output1);
            cipher.Decrypt(input, output2);

            CollectionAssert.AreEqual(output1, output2);
        }

        /// <summary>
        /// Verifies that Decrypt, when Known Input, matches Expected.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(DecryptTestData))]
        public void Decrypt_WhenKnownInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected, Func<IBlockCipher>? factory)
        {
            var engine = factory?.Invoke() ?? CreateBlockCipher(variant);
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
        public void Decrypt_WithInvalidInputSize_ShouldThrowExactly(TVariant variant, byte[] input)
        {
            using var cipher = CreateBlockCipher(variant);
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
        public void Decrypt_WithInvalidOutSize_ShouldThrowExactly(TVariant variant, byte[] output)
        {
            using var cipher = CreateBlockCipher(variant);
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                cipher.Decrypt(new byte[cipher.BlockSize], output);
            });
        }
    }
}