using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Returns test data that combines algorithm variants, named inputs, and expected hash results.
        /// </summary>
        /// <returns>A sequence of test case arguments: variant, input name, input bytes, and expected hash output.</returns>
        /// <remarks>
        /// This method is used to parameterize tests that verify the correctness of <see cref="HashAlgorithm.ComputeHash(byte[])" />
        /// against known input-output pairs.
        /// </remarks>
        public static IEnumerable<object[]> ComputeHashNamedInputTestData()
        {
            var instance = new TTest();
            foreach (var variant in instance.GetHashAlgorithmVariants())
            {
                var testVectors = instance.GetTestVectors(variant);
                foreach (var vector in testVectors)
                {
                    yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput };
                }
            }
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[])" /> after streaming begins completes successfully, and does
        /// not depend on previous TransformBlock state.
        /// </summary>
        [TestMethod]
        public void ComputeHash_AfterTransformBlock_ShouldIgnorePriorStreaming()
        {
            using var algorithm = this.CreateAlgorithm();

            // Begin a partial stream operation
            algorithm.TransformBlock(CryptoTestUtilities.ByteSequence0To255, 0, 128, null, 0);

            // Call ComputeHash independently
            var hash = algorithm.ComputeHash(CryptoTestUtilities.ByteSequence0To255);

            Assert.IsNotNull(hash);
            Assert.AreEqual(algorithm.HashSize / 8, hash.Length);
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="HashAlgorithm.ComputeHash(byte[])" /> with the same input produce the same result.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void ComputeHash_ShouldBeDeterministic(TVariant variant)
        {
            byte[] input = Enumerable.Range(0, 128).Select(i => (byte)(i % 256)).ToArray();
            using var algorithm1 = this.CreateAlgorithm(variant);
            using var algorithm2 = this.CreateAlgorithm(variant);
            byte[] hash1 = algorithm1.ComputeHash(input);
            byte[] hash2 = algorithm2.ComputeHash(input);

            CollectionAssert.AreEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that hashing a zero-length region in a buffer matches the empty input hash using <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" />.
        /// </summary>
        [DataTestMethod]
        [DataRow(128, 128, 0)]
        [DataRow(128, 0, 0)]
        public void ComputeHash_ShouldReturnEmptyHash_WhenOffsetAndCountAreZero(int size, int offset, int count)
        {
            byte[] buffer = new byte[size];
            byte[] expected = ExpectedEmptyInputHash;
            using var algorithm1 = this.CreateAlgorithm();
            using var algorithm2 = this.CreateAlgorithm();

            byte[] actual = algorithm1.ComputeHash(buffer, offset, count); CollectionAssert.AreEqual(expected, actual);

            buffer[0] = 0xFF;
            buffer[^1] = 0xFF;
            actual = algorithm2.ComputeHash(buffer, offset, count);
            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> throws <see cref="ArgumentException" /> when the cound
        /// or segment is invalid.
        /// </summary>
        [DataTestMethod]
        [DataRow(0, 0, -1)]
        [DataRow(0, 1, 0)]
        [DataRow(3, 0, 4)]
        [DataRow(3, 1, 3)]
        [DataRow(3, 3, 1)]
        public void ComputeHash_WhenOffsetAndCountCombinationIsInvalid_ShouldThrowExactly(int size, int offset, int count)
        {
            byte[] buffer = new byte[size];
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                algorithm.ComputeHash(buffer, offset, count);
            });
        }

        /// <summary>
        /// Verifies that hashing a segment with <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> produces the same result as the
        /// full input.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenOffsetAndCountReferToSameData_ShouldProduceIdenticalHash()
        {
            byte[] bufferA = new byte[256];
            byte[] bufferB = new byte[257];
            CryptoHelpers.FillWithRandomNonZeroBytes(bufferA);
            Array.Copy(bufferA, 0, bufferB, 1, bufferA.Length);

            using var algorithm1 = this.CreateAlgorithm();
            using var algorithm2 = this.CreateAlgorithm();
            byte[] expected = algorithm1.ComputeHash(bufferA);
            byte[] actual = algorithm2.ComputeHash(bufferB, 1, bufferA.Length);
            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> throws <see cref="ArgumentOutOfRangeException" /> when
        /// offset is negative.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenOffsetIsNegative_ShouldThrowExactly()
        {
            byte[] buffer = Array.Empty<byte>();
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                algorithm.ComputeHash(buffer, -1, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> produces consistent output for repeated identical input.
        /// </summary>
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(7)]
        [DataRow(64)]
        [DataRow(91)]
        [DataRow(1023)]
        [DataRow(1024)]
        public void ComputeHash_WhenReusedWithSameInput_ShouldProduceIdenticalHashResults(int size)
        {
            using var algorithm1 = this.CreateAlgorithm();
            using var algorithm2 = this.CreateAlgorithm();
            byte[] input = new byte[size];
            if (size > 0)
                CryptoHelpers.FillWithRandomNonZeroBytes(input);

            byte[] hashA = algorithm1.ComputeHash(input);
            byte[] hashB = algorithm2.ComputeHash(input);

            CollectionAssert.AreEqual(hashA, hashB);
        }

        /// <summary>
        /// Verifies that repeated calls to <see cref="HashAlgorithm.ComputeHash(byte[])" /> with the same input produce the same result.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void ComputeHash_WhenUsingIncrementalInput_ShouldMatchExpected(TVariant variant)
        {
            var algorithm = CreateAlgorithm(variant);
            var expectedHashes = GetExpectedHashesForIncrementalInput(variant).ToArray();

            if (expectedHashes.Length == 0)
                Assert.Inconclusive($"No expected hashes defined for variant {variant}.");

            byte[] input = new byte[expectedHashes.Length];

            for (int i = 0; i < expectedHashes.Length; i++)
            {
                byte[] expected = Convert.FromHexString(expectedHashes[i]);
                input[i] = (byte)i;
                var actual = algorithm.ComputeHash(input, 0, i);

                TestHelpers.TraceWriteIfNotEqual(expected, actual);

                CollectionAssert.AreEqual(expected, actual, $"Hash mismatch for variant '{variant}' at incremental length {i + 1}.");
                if (!algorithm.CanReuseTransform)
                    algorithm = CreateAlgorithm(variant);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ComputeHashNamedInputTestData))]
        public void ComputeHash_WhenUsingNamedInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected)
        {
            var algorithm = CreateAlgorithm(variant);
            byte[] actual = algorithm.ComputeHash(input);

            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual, $"Hash mismatch for {testName} using variant '{variant}'.");
        }

        [TestMethod]
        public void ComputeHash_WithLargeInput_ShouldNotThrow()
        {
            using var algorithm = this.CreateAlgorithm();

            // Create data that will certainly exceed uint.MaxValue during computation
            byte[] data = Enumerable.Repeat((byte)255, 20_480_000).ToArray();

            // This will throw naturally if there's an overflow or other error
            var result = algorithm.ComputeHash(data);

            Assert.AreEqual(algorithm.HashSize / 8, result.Length);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> throws <see cref="ArgumentNullException" /> when passed a null buffer.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WithNullBuffer_ShouldThrowExactly()
        {
            byte[] buffer = null!;
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.ComputeHash(buffer);
            });
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> throws <see cref="ArgumentNullException" /> when buffer
        /// is null.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WithNullBufferAndRange_ShouldThrowExactly()
        {
            byte[] buffer = null!;
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.ComputeHash(buffer, 0, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(Stream)" /> throws <see cref="ArgumentNullException" /> when passed a null stream.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WithNullStream_ShouldThrowExactly()
        {
            using var algorithm = this.CreateAlgorithm();

            // Expected behavior differs based on .NET version
#if NETFRAMEWORK || NETCOREAPP3_1

            // For .NET Framework and earlier .NET Core versions (up to 3.1), it throws ArgumentNullException.
            Assert.ThrowsExactly<ArgumentNullException>(                () => {
            algorithm.ComputeHash((Stream)null!);
            });
#else

            // For .NET 5 and later, it throws NullReferenceException instead.
            Assert.ThrowsExactly<NullReferenceException>(() =>
            {
                algorithm.ComputeHash((Stream)null!);
            });
#endif
        }
    }
}