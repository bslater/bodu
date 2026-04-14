// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
        : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
        where TAlgorithm : BlockHashAlgorithm<TAlgorithm>, new()
        where TVariant : Enum
    {
        /// <summary>
        /// Verifies that feeding the same input across differently sized streaming chunks yields the
        /// same final hash as hashing the input in a single pass.
        /// </summary>
        /// <remarks>
        /// Exercises the residual-buffer logic in <see cref="BlockHashAlgorithm{T}" /> to confirm that
        /// arbitrarily sized transform segments are correctly accumulated into aligned blocks before
        /// being dispatched to <c>ProcessBlock</c>.
        /// </remarks>
        [TestMethod]
        public virtual void ComputeHash_WhenInputStreamedInChunks_ShouldMatchSinglePassResult()
        {
            byte[] input = Enumerable.Range(0, (this.ExpectedBlockSizeBytes * 3) + 5)
                                     .Select(i => (byte)i)
                                     .ToArray();

            byte[] expected;
            using (var reference = this.CreateAlgorithm())
            {
                expected = reference.ComputeHash(input);
            }

            foreach (int chunkSize in this.StreamingChunkSizes)
            {
                using var algorithm = this.CreateAlgorithm();

                int offset = 0;
                while (offset < input.Length)
                {
                    int count = Math.Min(chunkSize, input.Length - offset);
                    algorithm.TransformBlock(input, offset, count, null, 0);
                    offset += count;
                }

                algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                byte[] actual = algorithm.Hash!;

                CollectionAssert.AreEqual(
                    expected,
                    actual,
                    $"Streaming with chunk size {chunkSize} diverged from the single-pass result.");
            }
        }

        /// <summary>
        /// Verifies that inputs whose length is not a multiple of the block size are padded and
        /// processed without error, producing a hash of the expected output length.
        /// </summary>
        /// <remarks>
        /// Ensures that <see cref="BlockHashAlgorithm{T}.PadBlock" /> handles the full residual-length
        /// domain on the happy path and that <see cref="BlockHashAlgorithm{T}.ProcessFinalBlock" />
        /// produces a digest of the size advertised by <see cref="HashAlgorithm.HashSize" />.
        /// </remarks>
        [TestMethod]
        public virtual void ComputeHash_WhenInputIsUnaligned_ShouldProduceValidHash()
        {
            int expectedLength;
            using (var reference = this.CreateAlgorithm())
            {
                expectedLength = reference.HashSize / 8;
            }

            foreach (int length in this.UnalignedInputLengths)
            {
                using var algorithm = this.CreateAlgorithm();
                byte[] input = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();

                byte[] hash = algorithm.ComputeHash(input);

                Assert.IsNotNull(hash, $"Hash for unaligned input length {length} must not be null.");
                Assert.AreEqual(
                    expectedLength,
                    hash.Length,
                    $"Hash length for unaligned input length {length} did not match HashSize.");
            }
        }

        /// <summary>
        /// Verifies that hashing an input whose length is exactly a multiple of the block size produces
        /// the same result regardless of whether the input is supplied in a single pass or across
        /// successive block-aligned transform calls.
        /// </summary>
        /// <remarks>
        /// Isolates the block-aligned fast path so that residual buffering cannot mask a regression in
        /// block dispatch ordering or per-block state accumulation.
        /// </remarks>
        [TestMethod]
        public virtual void ComputeHash_WhenInputIsBlockAligned_ShouldMatchAcrossTransformSplits()
        {
            const int blockCount = 4;
            int blockSize = this.ExpectedBlockSizeBytes;
            byte[] input = Enumerable.Range(0, blockSize * blockCount)
                                     .Select(i => (byte)(i & 0xFF))
                                     .ToArray();

            byte[] expected;
            using (var reference = this.CreateAlgorithm())
            {
                expected = reference.ComputeHash(input);
            }

            using var algorithm = this.CreateAlgorithm();
            for (int i = 0; i < blockCount; i++)
                algorithm.TransformBlock(input, i * blockSize, blockSize, null, 0);

            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] actual = algorithm.Hash!;

            CollectionAssert.AreEqual(
                expected,
                actual,
                "Block-aligned transform splits diverged from the single-pass result.");
        }
    }
}
