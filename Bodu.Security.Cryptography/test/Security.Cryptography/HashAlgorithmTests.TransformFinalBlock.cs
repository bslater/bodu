using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that hashing data via <see cref="HashAlgorithm.TransformBlock" /> and
        /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> produces the same result as using
        /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> with the full input.
        /// </summary>
        [TestMethod]
        public void TransformBlockAndFinalBlock_WhenUsedWithSimpleInput_ShouldMatchComputeHash()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            byte[] computeHashResult;
            using (var computeAlg = CreateAlgorithm())
            {
                computeHashResult = computeAlg.ComputeHash(input);
            }

            if (CreateAlgorithm().CanReuseTransform)
            {
                using var algorithm = CreateAlgorithm();

                algorithm.TransformBlock(input, 0, input.Length - 1, null, 0);
                algorithm.TransformFinalBlock(input, input.Length - 1, 1);

                byte[] transformResult = algorithm.Hash!;
                CollectionAssert.AreEqual(computeHashResult, transformResult, "Hash results should be identical between block-based and full ComputeHash execution.");
            }
            else
            {
                using var transformAlg = CreateAlgorithm();

                transformAlg.TransformBlock(input, 0, input.Length - 1, null, 0);
                transformAlg.TransformFinalBlock(input, input.Length - 1, 1);

                byte[] transformResult = transformAlg.Hash!;
                CollectionAssert.AreEqual(computeHashResult, transformResult, "Hash results should be identical between block-based and full ComputeHash execution.");
            }
        }

        /// <summary>
        /// Verifies that hashing an empty input via <see cref="HashAlgorithm.TransformBlock" /> and/or
        /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> produces the correct known hash result for an empty input.
        /// </summary>
        [TestMethod]
        public void TransformBlockAndFinalBlock_WhenInputIsEmpty_ShouldProduceExpectedHash()
        {
            byte[] expected = ExpectedEmptyInputHash;

            if (CreateAlgorithm().CanReuseTransform)
            {
                using var algorithm = CreateAlgorithm();

                // Case 1: TransformBlock followed by TransformFinalBlock
                algorithm.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0);
                algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                CollectionAssert.AreEqual(expected, algorithm.Hash, "TransformBlock followed by TransformFinalBlock on empty input should match expected hash.");

                // Case 2: TransformFinalBlock alone on same instance
                algorithm.Initialize();
                algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                CollectionAssert.AreEqual(expected, algorithm.Hash, "TransformFinalBlock alone on empty input should match expected hash.");
            }
            else
            {
                // One-shot case: use separate instances
                using var algorithm1 = CreateAlgorithm();
                algorithm1.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0);
                algorithm1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                CollectionAssert.AreEqual(expected, algorithm1.Hash, "TransformBlock followed by TransformFinalBlock on empty input should match expected hash.");

                using var algorithm2 = CreateAlgorithm();
                algorithm2.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                CollectionAssert.AreEqual(expected, algorithm2.Hash, "TransformFinalBlock alone on empty input should match expected hash.");
            }
        }

        /// <summary>
        /// Verifies that splitting the input between <see cref="HashAlgorithm.TransformBlock" /> and
        /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> still produces the correct and stable hash result.
        /// </summary>
        [TestMethod]
        public void TransformBlockAndFinalBlock_WhenInputIsSplitAcrossBlocks_ShouldProduceExpectedHash()
        {
            int blockSize;
            byte[] input;

            // Use a fresh instance to generate expected hash and determine block size
            using (var initial = CreateAlgorithm())
            {
                blockSize = initial.InputBlockSize;
                input = new byte[Math.Max(blockSize * 2, 8)];
                CryptoHelpers.FillWithRandomNonZeroBytes(input);
            }

            byte[] expected;

            // Compute the expected result via single-call ComputeHash
            using (var reference = CreateAlgorithm())
                expected = reference.ComputeHash(input);

            // Split and hash using TransformBlock/TransformFinalBlock
            if (CreateAlgorithm().CanReuseTransform)
            {
                using var algorithm = CreateAlgorithm();
                algorithm.Initialize();

                if (HandlePartialBlocks)
                {
                    byte[] first = input.Take(blockSize).ToArray();
                    byte[] second = input.Skip(blockSize).ToArray();

                    algorithm.TransformBlock(first, 0, first.Length, null, 0);
                    algorithm.TransformFinalBlock(second, 0, second.Length);
                }
                else
                {
                    algorithm.TransformFinalBlock(input, 0, input.Length);
                }

                CollectionAssert.AreEqual(expected, algorithm.Hash, "Split input should produce expected hash result.");

                // Validate consistent access to final hash
                CollectionAssert.AreEqual(expected, algorithm.Hash!, "Repeated access to Hash should be consistent.");
            }
            else
            {
                // One-shot MAC: Use a fresh instance for split input
                using var splitAlg = CreateAlgorithm();

                if (HandlePartialBlocks)
                {
                    byte[] first = input.Take(blockSize).ToArray();
                    byte[] second = input.Skip(blockSize).ToArray();

                    splitAlg.TransformBlock(first, 0, first.Length, null, 0);
                    splitAlg.TransformFinalBlock(second, 0, second.Length);
                }
                else
                {
                    splitAlg.TransformFinalBlock(input, 0, input.Length);
                }

                CollectionAssert.AreEqual(expected, splitAlg.Hash, "Split input should produce expected hash result.");

                // Validate consistent access to final hash
                CollectionAssert.AreEqual(expected, splitAlg.Hash!, "Repeated access to Hash should be consistent.");
            }
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.TransformBlock(byte[])" /> with partial input that is not a full block completes
        /// successfully, and produces the correct result independent of previous TransformBlock state.
        /// </summary>
        /// <remarks>
        /// This test ensures that the algorithm can correctly handle partial inputs (which are less than the block size) during streaming
        /// and finalize the hash with the correct result when TransformFinalBlock is called.
        /// </remarks>
        [TestMethod]
        public void TransformBlockAndFinalBlock_WithPartialBlockInputs_ShouldReturnExpectedHash()
        {
            var input = CryptoTestUtilities.ByteSequence256;
            byte[] expected;

            using (var reference = CreateAlgorithm())
            {
                expected = reference.ComputeHash(input);
            }

            // One-shot vs. reusable path
            if (CreateAlgorithm().CanReuseTransform)
            {
                using var algorithm = CreateAlgorithm();
                algorithm.Initialize();

                FeedWithPartialBlocks(algorithm, input);

                CollectionAssert.AreEqual(expected, algorithm.Hash, "Streamed hash with partial blocks should match expected result.");
            }
            else
            {
                using var algorithm = CreateAlgorithm();
                FeedWithPartialBlocks(algorithm, input);

                CollectionAssert.AreEqual(expected, algorithm.Hash, "Streamed hash with partial blocks should match expected result.");
            }
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> twice reuses the algorithm by resetting
        /// its state, and does not throw.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenCalledTwice_ShouldResetAndProduceValidHashes()
        {
            using var algorithm = CreateAlgorithm();
            var buffer = CryptoTestUtilities.SimpleTextAsciiBytes;

            // First pass
            algorithm.TransformBlock(buffer, 0, buffer.Length - 1, null, 0);
            algorithm.TransformFinalBlock(buffer, buffer.Length - 1, 1);
            var firstHash = algorithm.Hash;

            // Second pass with different data
            var newBuffer = CryptoTestUtilities.ByteSequence256;
            algorithm.TransformBlock(newBuffer, 0, newBuffer.Length - 1, null, 0);
            algorithm.TransformFinalBlock(newBuffer, newBuffer.Length - 1, 1);
            var secondHash = algorithm.Hash;

            Assert.IsNotNull(firstHash);
            Assert.IsNotNull(secondHash);
            CollectionAssert.AreNotEqual(firstHash, secondHash, "Expected two different hashes after resetting via TransformFinalBlock.");
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> twice without calling
        /// <see cref="HashAlgorithm.Initialize" /> behaves according to the .NET version in use. For older versions, it throws
        /// <see cref="CryptographicUnexpectedOperationException" />, whereas in later versions, it does not throw.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenCalledTwice_ExpectedBehaviorBasedOnDotNetVersion()
        {
            using var algorithm = CreateAlgorithm();
            var buffer = CryptoTestUtilities.SimpleTextAsciiBytes;

            algorithm.TransformBlock(buffer, 0, buffer.Length - 1, null, 0);
            algorithm.TransformFinalBlock(buffer, buffer.Length - 1, 1);

            // Expected behavior differs based on .NET version
#if NETFRAMEWORK || NETCOREAPP3_1

    // For .NET Framework and earlier .NET Core versions (up to 3.1), the second call should throw.
    Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(
        () => algorithm.TransformFinalBlock(buffer, 0, 0)
    );
#else

            // For .NET 5 and later, subsequent calls to TransformFinalBlock are allowed. In this case, we do not expect an exception.
            algorithm.TransformFinalBlock(buffer, 0, 0);
#endif
        }

        private void FeedWithPartialBlocks(HashAlgorithm algorithm, byte[] input)
        {
            if (HandlePartialBlocks)
            {
                int pos = 0;
                int size = Math.Max(algorithm.InputBlockSize - 1, 1);
                int len = input.Length - size;

                Random rnd = new Random();

                while (pos < len)
                {
                    int bytes = rnd.Next(1, size);
                    algorithm.TransformBlock(input, pos, bytes, null, 0);
                    pos += bytes;
                }

                algorithm.TransformFinalBlock(input, pos, input.Length - pos);
            }
            else
            {
                algorithm.TransformFinalBlock(input, 0, input.Length);
            }
        }
    }
}